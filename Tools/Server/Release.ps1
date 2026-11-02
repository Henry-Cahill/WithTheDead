<#
.SYNOPSIS
    Builds the co-op release (the Windows game and its Linux dedicated server), runs the server in Docker on hts-llm and
    publishes the game on its download page for the people you play with.

.DESCRIPTION
    Build: mirrors this project into a copy beside it (..\CLONE-Z-build, so the editor you have open isn't disturbed) and
    runs CloneZ.EditorTools.CloneZBatch.BuildRelease there in a batch editor. The result is Builds\Release\<stamp>\client
    (WithTheDead.exe, for players) and \server (the Linux Dedicated Server), both stamped 0.1.0+<stamp>; a server lets in
    only players on its own build (the editor always gets in). Both get a copy of the world store's current build
    (WorldStore\), so everyone builds the same Kentucky without the NAS. The client gets server.txt, the address its Join
    box starts at, and updates.txt, where it asks whether a new build is out. The build includes uncommitted changes.

    Deploy: copies the server to hts-llm and (re)starts it in Docker (docker\Dockerfile, docker\compose.yaml) as the
    container clonez-server on UDP -Port, beside the download page's file server (clonez-site). A raid under way on the
    old server is lost.

    Publish: zips the game and puts it on the download page (-SiteUrl, https://withthedead.howto.software): the page, the
    zip and latest.json, which the game reads to tell players a new build is out. With -Itch it also pushes the game to
    that itch.io project with butler (downloaded the first time; it asks you to log in once, in the browser).

    The one-time setup (the router's port forwards, the DNS record, the edge Caddy's site) is in Tools\Server\README.md.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Server\Release.ps1
    Builds both, redeploys the server and publishes the game on the download page.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Server\Release.ps1 -NoPublish
    Builds both and redeploys the server, without publishing the game.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Server\Release.ps1 -NoBuild -NoDeploy
    Publishes the newest release's game again.
#>
param(
    # The download page, or 'none'. Its host name is also the address the game joins unless -PublicAddress says otherwise.
    [string]$SiteUrl = $(if ($env:CLONEZ_SITE_URL) { $env:CLONEZ_SITE_URL } else { 'https://withthedead.howto.software' }),
    # The address your friends join: a DNS name or your public IP. The router forwards UDP -Port to the server.
    [string]$PublicAddress = $env:CLONEZ_PUBLIC_ADDRESS,
    # An itch.io project to push the game to as well, as user/game.
    [string]$Itch = $env:CLONEZ_ITCH,
    # The machine that runs the server and the download page, and the folder in its home that holds them.
    [string]$ServerHost = $(if ($env:CLONEZ_SERVER_HOST) { $env:CLONEZ_SERVER_HOST } else { 'htsadmin@192.168.1.202' }),
    [string]$ServerDir = 'clonez-server',
    [ValidateRange(1, 65535)][int]$Port = 7777,
    # Skip a step. Without a build, the release deployed and published is -Stamp's, else the newest in Builds\Release.
    [switch]$NoBuild,
    [switch]$NoDeploy,
    [switch]$NoPublish,
    [string]$Stamp,
    # The Unity editor to build with (default: the Unity Hub install of the project's version) and the project copy.
    [string]$Unity,
    [string]$BuildCopy
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$releases = Join-Path $project 'Builds\Release'
if (-not $BuildCopy) { $BuildCopy = Join-Path (Split-Path $project -Parent) ((Split-Path $project -Leaf) + '-build') }
$serverIp = ($ServerHost -split '@')[-1]
$site = if ($SiteUrl -and $SiteUrl -ne 'none') { $SiteUrl.TrimEnd('/') + '/' } else { $null }
$utf8 = New-Object Text.UTF8Encoding $false

# robocopy /MIR; its exit codes below 8 all mean success.
function Copy-Tree([string]$From, [string]$To, [string[]]$Extra = @()) {
    $null = & robocopy $From $To /MIR /R:2 /W:2 /NFL /NDL /NJH /NJS /NP /XJ @Extra
    if ($LASTEXITCODE -ge 8) { throw "robocopy $From -> $To failed (exit $LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
}

# Runs a bash script on the server machine. It travels base64-encoded: PowerShell 5 mangles quotes on native command
# lines and appends CRLF when piping to them.
function Invoke-Remote([string]$Script) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Script.Replace("`r`n", "`n")))
    & ssh -o BatchMode=yes -o ConnectTimeout=10 $ServerHost "echo $b64 | base64 -d | bash"
    if ($LASTEXITCODE -ne 0) { throw "The script on $ServerHost failed (exit $LASTEXITCODE)." }
}

# The world store's current build (the real-world map), kept in Builds\WorldStore so that a release fetches from the NAS
# only what changed since the last one. Returns that folder, or null when there's no map to carry.
function Update-WorldCopy {
    $store = if ($env:CLONEZ_WORLD) { $env:CLONEZ_WORLD } else { '\\192.168.1.170\CLONE-Z' }
    $copy = Join-Path $project 'Builds\WorldStore'
    $world = Join-Path $copy 'world'
    $currentFile = Join-Path $store 'world\current.json'
    if (Test-Path $currentFile) {
        $build = (Get-Content $currentFile -Raw | ConvertFrom-Json).build
        Write-Host "Bringing the copy of world store build $build up to date from $store..."
        New-Item -ItemType Directory -Force $world | Out-Null
        Copy-Tree (Join-Path $store "world\builds\$build") (Join-Path $world "builds\$build")
        $validation = Join-Path $store 'world\validation'
        if (Test-Path $validation) { Copy-Tree $validation (Join-Path $world 'validation') }
        # current.json goes in last, so a copy cut short is never taken for a whole one.
        Copy-Item $currentFile $world -Force
        Get-ChildItem (Join-Path $world 'builds') -Directory | Where-Object { $_.Name -ne $build } | Remove-Item -Recurse -Force
        return $copy
    }
    if (Test-Path (Join-Path $world 'current.json')) {
        Write-Warning "The world store at $($store) can't be read: the release carries the copy made last time."
        return $copy
    }
    Write-Warning "There's no world store at $($store): the release's raids into real places build the procedural town."
    return $null
}

# The files beside the game that say where to join (server.txt) and where to ask for a new build (updates.txt).
function Set-ClientFiles([string]$ClientDir) {
    $address = $PublicAddress
    if (-not $address -and $site) { $address = ([Uri]$site).Host }
    if (-not $address) {
        $address = $serverIp
        Write-Warning "No -PublicAddress, -SiteUrl or CLONEZ_PUBLIC_ADDRESS: the game will offer $serverIp, which only works on this network."
    }
    [IO.File]::WriteAllText((Join-Path $ClientDir 'server.txt'), "$($address):$Port`r`n", $utf8)
    $updates = Join-Path $ClientDir 'updates.txt'
    if ($site) { [IO.File]::WriteAllText($updates, $site + "latest.json`r`n", $utf8) }
    elseif (Test-Path $updates) { Remove-Item $updates }
}

# The release's version (its Application.version): BuildRelease notes it beside the builds.
function Get-ReleaseVersion {
    $file = Join-Path $release 'version.txt'
    if (Test-Path $file) { return (Get-Content $file -Raw).Trim() }
    $line = Select-String -Path (Join-Path $release 'build.log') -Pattern '^\[CloneZBatch\] Release (\S+): built' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($line) { return $line.Matches[0].Groups[1].Value }
    throw "Can't tell release $Stamp's version (no version.txt or build.log in $release)."
}

# The game as one zip with a WithTheDead folder inside, leaving out what Unity says not to ship.
function New-GameZip([string]$ClientDir, [string]$ZipPath) {
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    if (Test-Path $ZipPath) { Remove-Item $ZipPath }
    $base = (Resolve-Path $ClientDir).Path.TrimEnd('\') + '\'
    $zip = [IO.Compression.ZipFile]::Open($ZipPath, 'Create')
    try {
        Get-ChildItem $ClientDir -Recurse -File |
            Where-Object { $_.FullName.Substring($base.Length) -notmatch '^[^\\]*(_DoNotShip|_ButDontShipItWithYourGame)\\' } |
            ForEach-Object {
                $entry = 'WithTheDead/' + $_.FullName.Substring($base.Length).Replace('\', '/')
                [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal)
            }
    }
    finally { $zip.Dispose() }
}

# Copies the compose project (the server's image and the download page's file server) to the server machine.
function Copy-ComposeFiles {
    Invoke-Remote "mkdir -p ~/$ServerDir/site/files"
    $files = 'Dockerfile', 'compose.yaml', '.dockerignore' | ForEach-Object { Join-Path $PSScriptRoot "docker\$_" }
    & scp -q -o BatchMode=yes @files "$($ServerHost):$ServerDir/"
    if ($LASTEXITCODE -ne 0) { throw "scp to $ServerHost failed (exit $LASTEXITCODE)." }
}

function Get-Butler {
    $found = Get-Command butler -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    $dir = Join-Path $env:LOCALAPPDATA 'CloneZ\butler'
    $exe = Join-Path $dir 'butler.exe'
    if (-not (Test-Path $exe)) {
        Write-Host "Downloading butler (itch.io's upload tool) into $dir..."
        New-Item -ItemType Directory -Force $dir | Out-Null
        $zip = Join-Path $dir 'butler.zip'
        Invoke-WebRequest -UseBasicParsing 'https://broth.itch.zone/butler/windows-amd64/LATEST/archive/default' -OutFile $zip
        Expand-Archive $zip -DestinationPath $dir -Force
        Remove-Item $zip
    }
    return $exe
}

#region Build
if (-not $NoBuild) {
    if (-not $Unity) {
        $versionLine = Select-String -Path (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion:\s*(\S+)'
        $Unity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$($versionLine.Matches[0].Groups[1].Value)\Editor\Unity.exe"
    }
    if (-not (Test-Path $Unity)) { throw "No Unity editor at $Unity; pass -Unity <path to Unity.exe>." }
    # An open editor holds Temp\UnityLockfile exclusively; a leftover file from a closed one doesn't count.
    $lock = Join-Path $BuildCopy 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Dispose() }
        catch { throw "The build copy at $BuildCopy is open in a Unity editor (an earlier build still running?)." }
    }

    $commit = (& git -C $project rev-parse --short=8 HEAD).Trim()
    $Stamp = (Get-Date -Format 'yyyyMMdd.HHmm') + '.' + $commit
    $release = Join-Path $releases $Stamp

    Write-Host "Mirroring the project into $BuildCopy..."
    foreach ($d in 'Assets', 'Packages', 'ProjectSettings') { Copy-Tree (Join-Path $project $d) (Join-Path $BuildCopy $d) }
    # The copy keeps its own Library; the first time it starts from this one's, so it doesn't import everything again. Files
    # the open editor holds can't be copied: the copy's editor rebuilds whatever is missing.
    if (-not (Test-Path (Join-Path $BuildCopy 'Library'))) {
        Write-Host 'Seeding the copy''s Library from this project''s (first build only)...'
        $null = & robocopy (Join-Path $project 'Library') (Join-Path $BuildCopy 'Library') /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XJ /XF ilpp.pid burst.pid EditorInstance.json '*-lock'
        if ($LASTEXITCODE -ge 8) { Write-Warning "Some of the Library couldn't be copied (robocopy exit $LASTEXITCODE); the build copy rebuilds it." }
        $global:LASTEXITCODE = 0
    }
    # A batch editor stops the processes these files name: copied from here, they'd be the open editor's helpers.
    Remove-Item (Join-Path $BuildCopy 'Library\ilpp.pid'), (Join-Path $BuildCopy 'Library\burst.pid') -ErrorAction SilentlyContinue

    New-Item -ItemType Directory -Force $release | Out-Null
    $log = Join-Path $release 'build.log'
    $arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', "`"$BuildCopy`"",
        '-executeMethod', 'CloneZ.EditorTools.CloneZBatch.BuildRelease',
        '-clonez-release-out', "`"$release`"", '-clonez-release-stamp', $Stamp, '-logFile', "`"$log`"")
    Write-Host "Building release $Stamp (log: $log). The first build takes a while..."
    $started = Get-Date
    $editor = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
    Select-String -Path $log -Pattern '^\[CloneZBatch\]' -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_.Line }
    $client = Join-Path $release 'client\WithTheDead.exe'
    $server = Join-Path $release 'server\WithTheDead.x86_64'
    if ($editor.ExitCode -ne 0 -or -not (Test-Path $client) -or -not (Test-Path $server)) {
        Select-String -Path $log -Pattern 'error CS\d+|Error building|BuildFailedException|Exception' -ErrorAction SilentlyContinue |
            Select-Object -First 25 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
        throw "The build failed (editor exit $($editor.ExitCode)); see $log."
    }
    Write-Host ("Built in {0:0} min." -f ((Get-Date) - $started).TotalMinutes)

    $worldCopy = Update-WorldCopy
    if ($worldCopy) {
        foreach ($side in 'client', 'server') { Copy-Tree $worldCopy (Join-Path $release "$side\WorldStore") }
    }
}
else {
    if (-not $Stamp) {
        $newest = Get-ChildItem $releases -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path (Join-Path $_.FullName 'server\WithTheDead.x86_64') } | Sort-Object Name -Descending | Select-Object -First 1
        if (-not $newest) { throw "There's no release in $releases to deploy; build one first." }
        $Stamp = $newest.Name
    }
    $release = Join-Path $releases $Stamp
    if (-not (Test-Path $release)) { throw "There's no release $Stamp in $releases." }
}
# A new release always gets its client files; an earlier one keeps its own unless an address or a page is given.
$clientDir = Join-Path $release 'client'
if (-not $NoBuild -or $PSBoundParameters.ContainsKey('PublicAddress') -or $PSBoundParameters.ContainsKey('SiteUrl') -or
    -not (Test-Path (Join-Path $clientDir 'server.txt'))) { Set-ClientFiles $clientDir }
Write-Host "Release $Stamp in $release (the game joins $((Get-Content (Join-Path $clientDir 'server.txt') -TotalCount 1).Trim()))"
#endregion

#region Deploy
if (-not $NoDeploy) {
    $tgzName = "clonez-server-$Stamp.tgz"
    $tgz = Join-Path $env:TEMP $tgzName
    Write-Host 'Packing the server...'
    & tar -czf $tgz -C (Join-Path $release 'server') --exclude '*_DoNotShip' --exclude '*_ButDontShipItWithYourGame' .
    if ($LASTEXITCODE -ne 0) { throw "tar failed (exit $LASTEXITCODE)." }
    Write-Host ("Copying it to {0} ({1:0} MB)..." -f $ServerHost, ((Get-Item $tgz).Length / 1MB))
    Copy-ComposeFiles
    & scp -q -o BatchMode=yes $tgz "$($ServerHost):$ServerDir/"
    if ($LASTEXITCODE -ne 0) { throw "scp to $ServerHost failed (exit $LASTEXITCODE)." }
    Remove-Item $tgz
    Write-Host 'Building the image and restarting the server...'
    # Logs are read whole before they're searched: grep -q quitting early would fail the pipe under pipefail.
    $script = @'
set -euo pipefail
cd ~/{DIR}
rm -rf server.new
mkdir server.new
tar -xzf {TGZ} -C server.new
rm -f {TGZ}
rm -rf server
mv server.new server
echo "{STAMP}" > server/release.txt
export CLONEZ_PORT={PORT}
docker compose build --quiet
docker tag clonez-server:latest clonez-server:{STAMP}
docker compose up -d --force-recreate
# The last three releases' images stay for a rollback (docker tag clonez-server:<stamp> clonez-server:latest).
docker images clonez-server --format '{{.Tag}}' | grep -v '^latest$' | sort -r | tail -n +4 | xargs -r -I{} docker rmi clonez-server:{} > /dev/null || true
docker image prune -f > /dev/null
up=no
for i in $(seq 1 90); do
  logs="$(docker logs clonez-server 2>&1 || true)"
  if [[ "$logs" == *"waiting for a runner"* ]]; then up=yes; break; fi
  if [ "$(docker inspect -f '{{.State.Status}}' clonez-server)" != "running" ]; then break; fi
  sleep 2
done
docker logs --tail 30 clonez-server 2>&1
if [ "$up" != yes ]; then echo "The server didn't come up."; exit 1; fi
'@
    Invoke-Remote ($script.Replace('{TGZ}', $tgzName).Replace('{DIR}', $ServerDir).Replace('{STAMP}', $Stamp).Replace('{PORT}', "$Port"))
    Write-Host "The server is up on $($serverIp):$Port (UDP)." -ForegroundColor Green
}
#endregion

#region Publish
if (-not $NoPublish -and $site) {
    $version = Get-ReleaseVersion
    $zipName = "WithTheDead-$Stamp.zip"
    $zipPath = Join-Path $release $zipName
    Write-Host 'Zipping the game for the download page...'
    New-GameZip $clientDir $zipPath
    $size = (Get-Item $zipPath).Length
    $sha256 = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

    # latest.json (what the game asks) and the page, made from Tools\Server\site\index.html.
    $stage = Join-Path $env:TEMP "clonez-site-$Stamp"
    New-Item -ItemType Directory -Force $stage | Out-Null
    $latest = [ordered]@{ version = $version; file = "files/$zipName"; size = $size; sha256 = $sha256
        published = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'); page = $site }
    [IO.File]::WriteAllText((Join-Path $stage 'latest.json'), ($latest | ConvertTo-Json) + "`n", $utf8)
    $joins = (Get-Content (Join-Path $clientDir 'server.txt') -TotalCount 1).Trim()
    $html = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'site\index.html'))
    $html = $html.Replace('{{VERSION}}', [Net.WebUtility]::HtmlEncode($version)).Replace('{{FILE}}', $zipName).
        Replace('{{SIZE}}', ('{0:N0} MB' -f ($size / 1MB))).Replace('{{SHA256}}', $sha256).
        Replace('{{DATE}}', (Get-Date -Format 'd MMMM yyyy')).Replace('{{SERVER}}', [Net.WebUtility]::HtmlEncode($joins))
    [IO.File]::WriteAllText((Join-Path $stage 'index.html'), $html, $utf8)

    Write-Host ("Uploading {0} ({1:N0} MB) to the download page's server..." -f $zipName, ($size / 1MB))
    Copy-ComposeFiles
    # The zip goes up first, under a name nobody's offered; the page and latest.json point at it only once it's whole.
    foreach ($pair in @(@($zipPath, "site/files/$zipName.part"), @((Join-Path $stage 'latest.json'), 'site/latest.json.new'),
            @((Join-Path $stage 'index.html'), 'site/index.html.new'))) {
        & scp -q -o BatchMode=yes $pair[0] "$($ServerHost):$ServerDir/$($pair[1])"
        if ($LASTEXITCODE -ne 0) { throw "scp of $($pair[0]) to $ServerHost failed (exit $LASTEXITCODE)." }
    }
    Remove-Item $stage -Recurse -Force
    $script = @'
set -euo pipefail
cd ~/{DIR}/site
mv files/{ZIP}.part files/{ZIP}
mv index.html.new index.html
mv latest.json.new latest.json
# The newest three builds stay downloadable.
ls -1t files/WithTheDead-*.zip | tail -n +4 | xargs -r rm -f
cd ..
docker compose up -d site 2>&1 | grep -v -E '^\s*$' || true
'@
    Invoke-Remote ($script.Replace('{DIR}', $ServerDir).Replace('{ZIP}', $zipName))
    $served = Invoke-RestMethod -Uri "http://$($serverIp):8077/latest.json" -TimeoutSec 15
    if ($served.version -ne $version) { throw "The download page's server hands out $($served.version), not $version." }
    try {
        $public = Invoke-RestMethod -Uri ($site + 'latest.json') -TimeoutSec 15
        Write-Host "Published $version at $site (the game sees it: $($public.version))." -ForegroundColor Green
    }
    catch {
        Write-Warning "Published $version on $($serverIp):8077, but $($site)latest.json can't be reached yet ($($_.Exception.Message)). See the one-time setup in Tools\Server\README.md."
    }
}
if (-not $NoPublish -and $Itch) {
    $butler = Get-Butler
    $creds = Join-Path $env:USERPROFILE '.config\itch\butler_creds'
    if (-not $env:BUTLER_API_KEY -and -not (Test-Path $creds)) {
        Write-Host 'Log in to itch.io in the browser that opens (once only).'
        & $butler login
        if ($LASTEXITCODE -ne 0) { throw 'butler login failed.' }
    }
    & $butler push $clientDir "$($Itch):windows" --userversion $Stamp --ignore '*_DoNotShip' --ignore '*_ButDontShipItWithYourGame'
    if ($LASTEXITCODE -ne 0) { throw "butler push failed (exit $LASTEXITCODE)." }
    Write-Host "Pushed $Stamp to $($Itch):windows; the itch app updates your friends' copies." -ForegroundColor Green
}
#endregion
