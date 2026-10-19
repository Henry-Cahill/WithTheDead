<#
.SYNOPSIS
  Control the CLONE-Z Night Shift from this PC: local-LLM coding agents that work through
  Tools/NightShift/tasks.md overnight on the DGX Spark and the helper machines.

.DESCRIPTION
  The agents (OpenCode driving a model in the Spark's Ollama) run inside the AI Workbench project container
  and on helper machines, so no cloud AI tokens are spent and everything keeps going while this PC sleeps. In
  AI Workbench the same thing is the "night-shift" toggle under Applications. This script adds the PC parts:

    refs     Export Unity's reference assemblies from this PC and upload them to the Spark and the helpers.
             Every change is compiled against them (there is no Unity editor on those machines). Needed
             once, and again after upgrading Unity or changing packages.
    helpers  Set up the helper machines (tools, SSH access from the Spark, Unity references).
    add      Queue a task (one line, plain English).
    start    Start working through the queue (same as switching the Workbench toggle on). With no stop time
             set in nightshift.env it keeps going into the day, picking up new tasks, until you stop it.
    status   Progress, current tasks and recent commits.
    watch    Follow the live log (Ctrl+C stops watching, not the run).
    finish   Stop after the tasks in progress.
    stop     Stop now; unfinished tasks are rolled back and stay queued.
    fetch    Fetch the night's branches from the Spark into this repo for review (no GitHub needed).
    pr       Push the night shift's branch to GitHub and open (or update) its pull request. With -WhenDone
             it does so only once nothing is left to do (idle or stopped).
    autopr   Turn on the automatic pull request: a scheduled task on this PC runs "pr -WhenDone" every
             15 minutes (it needs this PC on and logged in; gh is already signed in here). -Off turns it off.

  Run with no arguments for a menu.

.EXAMPLE
  .\NightShift.ps1 add -Task "Let the player drink straight from a kitchen tap with E"
  .\NightShift.ps1 helpers -Helpers htsadmin@192.168.1.200, htsadmin@192.168.1.201
  .\NightShift.ps1 fetch
  .\NightShift.ps1 autopr
#>
param(
    [ValidateSet('menu', 'refs', 'helpers', 'add', 'start', 'status', 'watch', 'finish', 'stop', 'fetch', 'pr', 'autopr')]
    [string]$Action = 'menu',
    [string]$Task,
    # pr: only when the night shift has nothing left to do. autopr: turn the scheduled task off.
    [switch]$WhenDone,
    [switch]$Off,
    [string]$SparkHost = $(if ($env:CLONEZ_SPARK) { $env:CLONEZ_SPARK } else { 'henrylawrencecahill@192.168.1.252' }),
    # The AI Workbench project folder on the Spark, relative to the Spark user's home.
    [string]$SparkProject = 'nvidia-workbench/Henry-Cahill-CLONE-Z',
    [string]$Container = 'project-henry-cahill-clone-z',
    # Helper machines (user@host) that run extra agents; each needs SSH access from this PC.
    [string[]]$Helpers = $(if ($env:CLONEZ_HELPERS) { $env:CLONEZ_HELPERS -split '[,; ]+' } else { @('htsadmin@192.168.1.200', 'htsadmin@192.168.1.201', 'htsadmin@192.168.1.202') }),
    # The Unity project whose generated .csproj files and Library/ hold the reference assemblies.
    [string]$UnityProject
)

$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
$Root = (Resolve-Path (Join-Path $Here '..\..')).Path
if (-not $UnityProject) { $UnityProject = $Root }
$UnityProject = (Resolve-Path $UnityProject).Path.TrimEnd('\')
$Ssh = @('-o', 'BatchMode=yes', '-o', 'ConnectTimeout=10')
$RemoteDir = "~/$SparkProject/Tools/NightShift"
$HelperDir = 'clonez-nightshift'
$OpenCodeVersion = '1.19.3'
$SparkIp = ($SparkHost -split '@')[-1]

# PowerShell 5 mangles quotes on native command lines and appends CRLF when piping to them, so remote
# scripts travel base64-encoded (only [A-Za-z0-9+/=], nothing to escape) and are decoded on the far side.
function Invoke-Remote([string]$Script, [string]$Target = $SparkHost) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($Script -replace "`r", '') + "`n"))
    & ssh @Ssh $Target "echo $b64 | base64 -d | bash" | Out-Host
    return $LASTEXITCODE
}

# Runs nightshift.sh inside the Workbench project container on the Spark.
function Invoke-Container([string]$Command) {
    return Invoke-Remote @"
if ! docker ps --format '{{.Names}}' | grep -qx '$Container'; then
  echo 'The project container is not running. In AI Workbench, open Henry-Cahill-CLONE-Z on spark-f146 and start the Project Container.'
  exit 3
fi
docker exec $Container bash /project/Tools/NightShift/nightshift.sh $Command
"@
}

# Starts the night shift through AI Workbench's CLI, like switching on the night-shift toggle, so the toggle shows the
# truth. That also starts the project container when it isn't running.
function Start-NightShift {
    return Invoke-Remote @"
NVWB="`$HOME/.nvwb/bin/nvwb-cli"
if [ -x "`$NVWB" ]; then
  "`$NVWB" start night-shift -c local -p "`$HOME/$SparkProject" --no-browser || exit `$?
elif docker ps --format '{{.Names}}' | grep -qx '$Container'; then
  docker exec $Container bash /project/Tools/NightShift/nightshift.sh start || exit `$?
else
  echo 'The project container is not running. In AI Workbench, open Henry-Cahill-CLONE-Z on spark-f146 and start the Project Container.'
  exit 3
fi
sleep 3
if docker exec $Container bash /project/Tools/NightShift/nightshift.sh health; then
  echo 'The night shift is running. Watch it with 4 in the menu (or: NightShift.bat watch).'
else
  echo "The night shift didn't start:"; docker exec $Container tail -n 15 /project/Tools/NightShift/.state/nightshift.log; exit 1
fi
"@
}

# Stops the night shift now, then tells AI Workbench so the toggle shows it as off (Workbench also stops the project
# container if no other app, such as JupyterLab, is using it).
function Stop-NightShift {
    return Invoke-Remote @"
if docker ps --format '{{.Names}}' | grep -qx '$Container'; then
  docker exec $Container bash /project/Tools/NightShift/nightshift.sh stop
else
  echo 'The night shift is not running (the project container is stopped).'
fi
NVWB="`$HOME/.nvwb/bin/nvwb-cli"
[ -x "`$NVWB" ] && "`$NVWB" stop night-shift -c local -p "`$HOME/$SparkProject" >/dev/null 2>&1
exit 0
"@
}

#region Unity reference assemblies

function Resolve-ProjectPath([string]$p) {
    if ([IO.Path]::IsPathRooted($p)) { return [IO.Path]::GetFullPath($p) }
    return [IO.Path]::GetFullPath((Join-Path $UnityProject $p))
}

function Get-CsprojInfo([string]$Name) {
    $path = Join-Path $UnityProject $Name
    if (-not (Test-Path $path)) {
        throw "$Name is missing from $UnityProject. Open the project in Unity once (or Assets > Open C# Project) so Unity generates it."
    }
    [xml]$x = Get-Content $path -Raw
    $props = @{}
    foreach ($pg in $x.Project.PropertyGroup) {
        foreach ($n in $pg.ChildNodes) { if ($n.NodeType -eq 'Element') { $props[$n.LocalName] = $n.InnerText } }
    }
    $refs = @(foreach ($r in $x.Project.ItemGroup.Reference) { if ($r -and $r.HintPath) { Resolve-ProjectPath $r.HintPath } })
    # The Visual Studio Tools for Unity analyzer only adds IDE suggestions; Unity's own generators are kept.
    $analyzers = @(foreach ($a in $x.Project.ItemGroup.Analyzer) {
            if ($a -and $a.Include -and $a.Include -notmatch 'Visual Studio') { Resolve-ProjectPath $a.Include }
        })
    [pscustomobject]@{ Props = $props; References = $refs; Analyzers = $analyzers }
}

# Where a DLL from this PC goes in the bundle: editor/... (the Unity install), project/... (Library), other/...
function Get-BundlePath([string]$Full, [string]$EditorData) {
    if ($Full.StartsWith($EditorData, [StringComparison]::OrdinalIgnoreCase)) {
        return 'editor/' + $Full.Substring($EditorData.Length).Replace('\', '/')
    }
    if ($Full.StartsWith($UnityProject + '\', [StringComparison]::OrdinalIgnoreCase)) {
        return 'project/' + $Full.Substring($UnityProject.Length + 1).Replace('\', '/')
    }
    return 'other/' + [IO.Path]::GetFileName($Full)
}

function New-CheckProject([string]$Kind, $Info, [string]$EditorData) {
    $p = $Info.Props
    $esc = { param($s) [Security.SecurityElement]::Escape($s) }
    $refLines = foreach ($r in $Info.References) {
        $name = [IO.Path]::GetFileNameWithoutExtension($r)
        "    <Reference Include=`"$(& $esc $name)`"><HintPath>`$(MSBuildThisFileDirectory)refs/$(& $esc (Get-BundlePath $r $EditorData))</HintPath><Private>false</Private></Reference>"
    }
    $analyzerLines = foreach ($a in $Info.Analyzers) {
        "    <Analyzer Include=`"`$(MSBuildThisFileDirectory)refs/$(& $esc (Get-BundlePath $a $EditorData))`" />"
    }
    if ($Kind -eq 'Runtime') {
        $compile = '    <Compile Include="$(ProjectRoot)Assets/**/*.cs" Exclude="$(ProjectRoot)Assets/**/Editor/**" />'
    }
    else {
        $compile = '    <Compile Include="$(ProjectRoot)Assets/**/Editor/**/*.cs" />' + "`n" +
        '    <Reference Include="Assembly-CSharp"><HintPath>$(NightShiftOut)bin/Assembly-CSharp.dll</HintPath><Private>false</Private></Reference>'
    }
    $noWarn = if ($p.NoWarn) { $p.NoWarn } else { '0169' }
    $unsafe = if ($p.AllowUnsafeBlocks) { $p.AllowUnsafeBlocks } else { 'False' }
    $lang = if ($p.LangVersion) { $p.LangVersion } else { '9.0' }
    @"
<Project>
  <!-- Generated by Tools/NightShift/NightShift.ps1 from $(& $esc $p.AssemblyName).csproj (Unity $(& $esc $p.UnityVersion)). Do not edit. -->
  <PropertyGroup>
    <ProjectRoot Condition="'`$(ProjectRoot)' == ''">`$([System.IO.Path]::GetFullPath('`$(MSBuildThisFileDirectory)../../../'))</ProjectRoot>
    <NightShiftOut Condition="'`$(NightShiftOut)' == ''">`$(MSBuildThisFileDirectory)</NightShiftOut>
    <BaseIntermediateOutputPath>`$(NightShiftOut)obj/$Kind/</BaseIntermediateOutputPath>
    <IntermediateOutputPath>`$(BaseIntermediateOutputPath)</IntermediateOutputPath>
    <OutputPath>`$(NightShiftOut)bin/</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
  <PropertyGroup>
    <AssemblyName>$(& $esc $p.AssemblyName)</AssemblyName>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>$lang</LangVersion>
    <OutputType>Library</OutputType>
    <EnableDefaultItems>false</EnableDefaultItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
    <NoStandardLibraries>true</NoStandardLibraries>
    <NoStdLib>true</NoStdLib>
    <NoConfig>true</NoConfig>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
    <AllowUnsafeBlocks>$unsafe</AllowUnsafeBlocks>
    <NoWarn>$(& $esc $noWarn)</NoWarn>
    <MSBuildWarningsAsMessages>MSB3277</MSBuildWarningsAsMessages>
    <DefineConstants>$(& $esc $p.DefineConstants)</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
$compile
  </ItemGroup>
  <ItemGroup>
$($refLines -join "`n")
  </ItemGroup>
  <ItemGroup>
$($analyzerLines -join "`n")
  </ItemGroup>
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
</Project>
"@
}

function Export-UnityRefs {
    Write-Host "Reading Unity's project files from $UnityProject..." -ForegroundColor Cyan
    $runtime = Get-CsprojInfo 'Assembly-CSharp.csproj'
    $editor = Get-CsprojInfo 'Assembly-CSharp-Editor.csproj'
    $sample = @($runtime.References + $editor.References) | Where-Object { $_ -match '\\Editor\\Data\\' } | Select-Object -First 1
    if (-not $sample) { throw 'Could not find the Unity editor install in the project references.' }
    $editorData = $sample.Substring(0, $sample.IndexOf('\Editor\Data\', [StringComparison]::OrdinalIgnoreCase) + '\Editor\Data\'.Length)

    $out = Join-Path $Here '.unityrefs'
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    New-Item -ItemType Directory -Path (Join-Path $out 'refs') | Out-Null
    $files = @($runtime.References + $editor.References + $runtime.Analyzers + $editor.Analyzers) | Sort-Object -Unique
    $missing = @(); $bytes = 0
    foreach ($f in $files) {
        if (-not (Test-Path $f)) { $missing += $f; continue }
        $dest = Join-Path (Join-Path $out 'refs') (Get-BundlePath $f $editorData)
        New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
        Copy-Item $f $dest
        $bytes += (Get-Item $f).Length
    }
    if ($missing.Count -gt 0) { Write-Host "Skipped $($missing.Count) missing file(s), e.g. $($missing[0])" -ForegroundColor Yellow }
    $utf8 = New-Object Text.UTF8Encoding($false)
    [IO.File]::WriteAllText((Join-Path $out 'Runtime.csproj'), (New-CheckProject 'Runtime' $runtime $editorData).Replace("`r", ''), $utf8)
    [IO.File]::WriteAllText((Join-Path $out 'Editor.csproj'), (New-CheckProject 'Editor' $editor $editorData).Replace("`r", ''), $utf8)
    Write-Host ("Bundled {0} assemblies ({1:N0} MB)." -f ($files.Count - $missing.Count), ($bytes / 1MB)) -ForegroundColor Green
    return $out
}

function Test-RefsLocally([string]$Out) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return }
    Write-Host 'Checking that the bundle compiles the project on this PC...' -ForegroundColor Cyan
    $build = Join-Path $env:TEMP 'clonez-refcheck\'
    foreach ($kind in 'Runtime', 'Editor') {
        $log = & dotnet build (Join-Path $Out "$kind.csproj") -nologo -v:q -clp:NoSummary "-p:ProjectRoot=$UnityProject\" "-p:NightShiftOut=$build" 2>&1
        if ($LASTEXITCODE -ne 0) {
            $log | Select-String ': error ' | Select-Object -First 15 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
            throw "The $kind check build failed on this PC; fix the errors above (or reopen the project in Unity) and retry."
        }
    }
    Remove-Item -Recurse -Force $build -ErrorAction SilentlyContinue
    Write-Host 'It compiles cleanly here.' -ForegroundColor Green
}

function New-RefsArchive {
    $out = Export-UnityRefs
    Test-RefsLocally $out
    $tgz = Join-Path $env:TEMP 'clonez-unityrefs.tgz'
    & tar.exe -czf $tgz -C $out .
    if ($LASTEXITCODE -ne 0) { throw 'tar failed' }
    return $tgz
}

# Uploads the bundle and unpacks it at $Dest (a path on $Target, relative to the home directory).
function Send-RefsArchive([string]$Tgz, [string]$Target, [string]$Dest) {
    & ssh @Ssh $Target "mkdir -p $Dest"
    & scp -q @Ssh $Tgz "${Target}:$Dest.tgz"
    if ($LASTEXITCODE -ne 0) { throw "scp to $Target failed" }
    $code = Invoke-Remote "rm -rf $Dest && mkdir -p $Dest && tar -xzf $Dest.tgz -C $Dest && rm $Dest.tgz && echo 'Unity references are on $Target.'" $Target
    if ($code -ne 0) { throw "Unpacking on $Target failed ($code)" }
}

function Send-UnityRefs {
    $tgz = New-RefsArchive
    Write-Host "Uploading to the Spark..." -ForegroundColor Cyan
    Send-RefsArchive $tgz $SparkHost "$RemoteDir/.unityrefs"
    foreach ($h in (Get-SparkHelpers)) {
        Write-Host "Uploading to $($h.Name)..." -ForegroundColor Cyan
        Send-RefsArchive $tgz $h.Target "$HelperDir/unityrefs"
    }
    Remove-Item $tgz
}

#endregion

function Get-RemoteOutput([string]$Script, [string]$Target = $SparkHost) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($Script -replace "`r", '') + "`n"))
    $out = & ssh @Ssh $Target "echo $b64 | base64 -d | bash"
    if ($LASTEXITCODE -ne 0) { throw "A command on $Target failed (exit $LASTEXITCODE): $out" }
    return $out
}

# Helpers the Spark's coordinator knows about (.state/helpers.txt: "name user@host" per line).
function Get-SparkHelpers {
    $lines = & ssh @Ssh $SparkHost "cat $RemoteDir/.state/helpers.txt 2>/dev/null"
    return @(foreach ($l in $lines) { $p = "$l".Trim() -split '\s+'; if ($p.Count -eq 2) { [pscustomobject]@{ Name = $p[0]; Target = $p[1] } } })
}

function Install-Helpers {
    if (-not $Helpers -or $Helpers.Count -eq 0) { throw 'No helper machines given (-Helpers user@host, ...).' }
    # The coordinator's own key: kept in the project's ignored .state folder on the Spark and accepted by the
    # helpers only from the Spark's address.
    Write-Host 'Preparing the SSH key the Spark uses to reach the helpers...' -ForegroundColor Cyan
    $pub = (Get-RemoteOutput @"
d=$RemoteDir/.state/ssh
mkdir -p "`$d" && chmod 700 "`$d"
[ -f "`$d/id_ed25519" ] || ssh-keygen -q -t ed25519 -N '' -C nightshift@spark -f "`$d/id_ed25519"
cat "`$d/id_ed25519.pub"
"@ | Select-Object -Last 1).Trim()
    if ($pub -notmatch '^ssh-ed25519 (\S+)') { throw "Unexpected key from the Spark: $pub" }
    $keyBody = $Matches[1]
    $knownHosts = Join-Path $env:USERPROFILE '.ssh\known_hosts'
    $ready = @(); $hostKeys = @()
    foreach ($target in $Helpers) {
        $ip = ($target -split '@')[-1]
        Write-Host "`nSetting up $target..." -ForegroundColor Cyan
        try {
            $out = Get-RemoteOutput @"
set -e
DIR="`$HOME/$HelperDir"
mkdir -p "`$DIR/repo" "`$HOME/.local/bin" "`$HOME/.ssh" && chmod 700 "`$HOME/.ssh"
cd "`$DIR/repo" && (git rev-parse --git-dir >/dev/null 2>&1 || git init -q)
touch "`$HOME/.ssh/authorized_keys" && chmod 600 "`$HOME/.ssh/authorized_keys"
grep -qF '$keyBody' "`$HOME/.ssh/authorized_keys" || echo 'from="$SparkIp" $pub' >> "`$HOME/.ssh/authorized_keys"
case "`$(uname -m)" in x86_64) arch=x64 ;; aarch64|arm64) arch=arm64 ;; *) echo "unsupported CPU `$(uname -m)"; exit 1 ;; esac
if [ "`$("`$HOME/.local/bin/opencode" --version 2>/dev/null)" != "$OpenCodeVersion" ]; then
  echo "installing OpenCode $OpenCodeVersion..."
  curl -fsSL "https://github.com/anomalyco/opencode/releases/download/v$OpenCodeVersion/opencode-linux-`$arch.tar.gz" | tar -xz -C "`$HOME/.local/bin" opencode
fi
if [ ! -x "`$HOME/.dotnet/dotnet" ]; then
  echo "installing the .NET SDK..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/nightshift-dotnet-install.sh
  bash /tmp/nightshift-dotnet-install.sh --channel 10.0 --install-dir "`$HOME/.dotnet" >/dev/null
  rm -f /tmp/nightshift-dotnet-install.sh
fi
curl -fs --max-time 5 http://${SparkIp}:11434/api/version >/dev/null || { echo "can't reach Ollama on the Spark"; exit 1; }
echo "READY `$(hostname) opencode `$("`$HOME/.local/bin/opencode" --version) dotnet `$("`$HOME/.dotnet/dotnet" --version)"
"@ $target
            $out | Where-Object { $_ -notmatch '^READY' } | ForEach-Object { Write-Host "  $_" }
            $readyLine = $out | Where-Object { $_ -match '^READY' } | Select-Object -Last 1
            if (-not $readyLine) { throw 'setup did not finish' }
            $name = ($readyLine -split ' ')[1]
            $keys = @(& ssh-keygen -F $ip -f $knownHosts | Where-Object { $_ -and $_ -notmatch '^#' })
            if ($keys.Count -eq 0) { throw "$ip isn't in this PC's known_hosts; connect to it once with ssh first" }
            $hostKeys += $keys
            $ready += [pscustomobject]@{ Name = $name; Target = $target }
            Write-Host "  ready: $readyLine" -ForegroundColor Green
        }
        catch { Write-Host "  skipped ${target}: $($_.Exception.Message)" -ForegroundColor Yellow }
    }
    if ($ready.Count -eq 0) { throw 'No helper machine could be set up.' }

    $tgz = New-RefsArchive
    foreach ($h in $ready) { Send-RefsArchive $tgz $h.Target "$HelperDir/unityrefs" }
    Remove-Item $tgz

    $helpersB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes((($ready | ForEach-Object { "$($_.Name) $($_.Target)" }) -join "`n") + "`n"))
    $hostsB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($hostKeys -join "`n") + "`n"))
    Invoke-Remote @"
echo $helpersB64 | base64 -d > $RemoteDir/.state/helpers.txt
echo $hostsB64 | base64 -d > $RemoteDir/.state/ssh/known_hosts
echo 'The Spark will use: ' `$(cut -d' ' -f1 $RemoteDir/.state/helpers.txt | tr '\n' ' ')
"@ | Out-Null
}

function Add-Task {
    $text = $Task
    if (-not $text) { $text = Read-Host 'Task (plain English, one line)' }
    $text = ($text -replace '\s+', ' ').Trim()
    if (-not $text) { return }
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($text))
    Invoke-Remote @"
python3 - <<'PY'
import base64, fcntl, pathlib
p = pathlib.Path.home() / '$SparkProject' / 'Tools' / 'NightShift' / 'tasks.md'
lock_path = p.parent / '.state' / 'tasks.lock'
lock_path.parent.mkdir(parents=True, exist_ok=True)
text = base64.b64decode('$b64').decode('utf-8')
with lock_path.open('a+b') as lock:
   fcntl.flock(lock, fcntl.LOCK_EX)
   body = p.read_text(encoding='utf-8')
   tmp = p.with_name(p.name + '.tmp')
   tmp.write_text(body + ('' if body.endswith('\n') else '\n') + '- [ ] ' + text + '\n', encoding='utf-8')
   tmp.replace(p)
   fcntl.flock(lock, fcntl.LOCK_UN)
print('Queued: ' + text)
PY
"@ | Out-Null
}

function Get-NightBranches {
    $old = $env:GIT_SSH_COMMAND
    # Git for Windows ships its own ssh; use the Windows one so the same keys and known_hosts apply.
    $env:GIT_SSH_COMMAND = 'C:/Windows/System32/OpenSSH/ssh.exe -o BatchMode=yes -o ConnectTimeout=10'
    try {
        Write-Host 'Fetching night shift branches from the Spark...' -ForegroundColor Cyan
        & git -C $Root fetch --prune "${SparkHost}:$SparkProject" '+refs/heads/nightshift/*:refs/remotes/spark/nightshift/*'
        if ($LASTEXITCODE -ne 0) { throw 'git fetch from the Spark failed' }
    }
    finally { $env:GIT_SSH_COMMAND = $old }
    $branches = @(& git -C $Root for-each-ref --sort=-committerdate --format='%(refname:short)' refs/remotes/spark/nightshift)
    if ($branches.Count -eq 0 -or -not $branches[0]) { Write-Host 'No night shift branches on the Spark yet.' -ForegroundColor Yellow; return }
    $latest = $branches[0]
    $local = $latest -replace '^spark/', ''
    Write-Host "`nLatest: $latest" -ForegroundColor Green
    $base = & git -C $Root merge-base origin/main $latest 2>$null
    if ($base) { & git -C $Root --no-pager log --oneline --no-decorate "$base..$latest" }
    Write-Host "`nTry it in Unity:      git switch -c $local $latest" -ForegroundColor Green
    Write-Host "Publish it for a PR:  git push origin ${latest}:refs/heads/$local   (or press Push in AI Workbench)" -ForegroundColor Green
    Write-Host "The night's report is Tools/NightShift/reports/ on that branch." -ForegroundColor Green
}

function Watch-Log { try { & ssh -t @Ssh $SparkHost "bash $RemoteDir/nightshift.sh log" } catch { } }

#region Pull request

$AutoPrTask = 'CLONE-Z Night Shift pull request'

# What the Spark's night shift is doing: running or not, tasks in progress and queued, and its branch.
function Get-NightState {
    $out = Get-RemoteOutput @"
if docker ps --format '{{.Names}}' | grep -qx '$Container'; then
  docker exec $Container bash /project/Tools/NightShift/nightshift.sh state
else
  printf '{"running": false, "branch": "%s", "working": 0, "queued": 0, "idle_since": null}\n' "`$(git -C ~/$SparkProject rev-parse --abbrev-ref HEAD)"
fi
"@
    return (@($out)[-1] | ConvertFrom-Json)
}

function Get-GitHubRepo {
    $url = "$(& git -C $Root remote get-url origin)".Trim()
    if ($url -notmatch 'github\.com[:/](.+?)(\.git)?/?$') { throw "origin ($url) isn't a GitHub repository" }
    return $Matches[1]
}

function Write-PrLog([string]$Message) {
    Write-Host $Message
    $dir = Join-Path $Here '.state'
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Add-Content -Path (Join-Path $dir 'pr.log') -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm') $Message" -Encoding UTF8
}

# Runs git or gh and returns its output lines. Network hiccups get retried; if it still fails, the error says why
# (the scheduled task has no window, so pr.log is the only place anyone sees it).
function Invoke-Tool([string]$What, [string]$Exe, [string[]]$Arguments, [int]$Tries = 3) {
    for ($try = 1; ; $try++) {
        $out = New-Object Collections.Generic.List[string]
        $err = New-Object Collections.Generic.List[string]
        $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'  # stderr isn't failure; the exit code says
        try {
            & $Exe @Arguments 2>&1 | ForEach-Object { if ($_ -is [Management.Automation.ErrorRecord]) { $err.Add("$_") } else { $out.Add("$_") } }
            $code = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $eap }
        if ($code -eq 0) { return $out.ToArray() }
        if ($try -ge $Tries) {
            $why = (@($err) + @($out) | Where-Object { $_ -match '\S' } | Select-Object -Last 3) -join ' / '
            throw "$What failed (exit $code): $why"
        }
        Start-Sleep -Seconds (15 * $try)
    }
}

# Pushes the Spark's night shift branch to GitHub and opens its pull request, or updates the open one.
function Publish-NightShift {
    [Console]::OutputEncoding = [Text.Encoding]::UTF8  # the reports are UTF-8; PowerShell 5 would read git's output as OEM
    $state = Get-NightState
    $branch = "$($state.branch)"
    if ($branch -notlike 'nightshift/*') { Write-Host "The Spark's project is on '$branch', not a night shift branch, so there's nothing to publish."; return }
    # Coordinators from before "dispatchable" only say whether they've gone idle.
    $left = if ($null -ne $state.dispatchable) { $state.dispatchable } elseif ($state.idle_since) { 0 } else { $state.queued }
    $done = (-not $state.running) -or ($state.working -eq 0 -and $left -eq 0)
    if ($WhenDone -and -not $done) {
        Write-Host "Still working on $branch ($($state.working) in progress, $($state.queued) queued); the pull request waits until it's done."
        return
    }
    $old = $env:GIT_SSH_COMMAND
    $env:GIT_SSH_COMMAND = 'C:/Windows/System32/OpenSSH/ssh.exe -o BatchMode=yes -o ConnectTimeout=10'
    try { Invoke-Tool "git fetch of $branch from the Spark" git @('-C', $Root, 'fetch', '-q', "${SparkHost}:$SparkProject", "+refs/heads/${branch}:refs/remotes/spark/$branch") | Out-Null }
    finally { $env:GIT_SSH_COMMAND = $old }
    $env:GIT_TERMINAL_PROMPT = '0'; $env:GCM_INTERACTIVE = 'never'  # fail rather than wait for a sign-in nobody sees
    Invoke-Tool 'git fetch of main from GitHub' git @('-C', $Root, 'fetch', '-q', 'origin', 'main') | Out-Null
    $sha = "$(& git -C $Root rev-parse "refs/remotes/spark/$branch")".Trim()
    if ([int]"$(& git -C $Root rev-list --count "origin/main..$sha")" -eq 0) { Write-Host "$branch has nothing that isn't on main yet."; return }
    # Never publish what must stay on the Spark (its SSH key for the helpers, Unity's DLLs) or anything like a private key.
    $private = @(& git -C $Root log --format= --name-only "origin/main..$sha" | Where-Object {
            $_ -match '(^|/)(\.state|\.unityrefs)/|(^|/)id_(rsa|ecdsa|ed25519)$|\.(pem|pfx|p12)$' } | Select-Object -Unique)
    $private += @(& git -C $Root log --format=%h -G 'BEGIN [A-Z ]*PRIVATE KEY' "origin/main..$sha" | ForEach-Object { "a private key in commit $_" })
    if ($private.Count -gt 0) { throw "$branch has files that must not leave the Spark ($(($private | Select-Object -First 3) -join ', ')), so it wasn't pushed" }
    $repo = Get-GitHubRepo
    $remote = "$(Invoke-Tool 'git ls-remote on GitHub' git @('-C', $Root, 'ls-remote', 'origin', "refs/heads/$branch"))".Split("`t ")[0]
    $prs = Invoke-Tool 'gh pr list (check: gh auth status)' gh @('pr', 'list', '--repo', $repo, '--head', $branch, '--state', 'open', '--json', 'number,url')
    $open = @(if ("$prs".Trim()) { ($prs -join "`n") | ConvertFrom-Json | ForEach-Object { $_ } })
    # Refresh PR metadata even when the branch is already pushed, so failed edits can be retried.
    if ($remote -ne $sha) {
        Invoke-Tool "git push of $branch to GitHub" git @('-C', $Root, 'push', '-q', 'origin', "${sha}:refs/heads/$branch") | Out-Null
    }

    # Description: what to check before merging, then every run's report on the branch, newest first.
    $reports = @(& git -C $Root diff --name-only --diff-filter=AM "origin/main...$sha" -- 'Tools/NightShift/reports/*.md' |
        Sort-Object { $_ -replace '\.md$', '' } -Descending)
    $texts = foreach ($r in $reports) {
        $base = "https://github.com/$repo/blob/$branch/" + ($r -replace '[^/]+$', '')
        # Links in a report are relative to the reports folder; point them at the branch on GitHub.
        [regex]::Replace(((& git -C $Root show "${sha}:$r") -join "`n"), '\]\((?![A-Za-z][A-Za-z0-9+.-]*:|#|/)([^)\s]+)\)',
            { param($m) '](' + $base + $m.Groups[1].Value + ')' })
    }
    $all = $texts -join "`n"
    $landed = ([regex]::Matches($all, '(?m)^\*\*landed\*\* by')).Count
    $failed = ([regex]::Matches($all, '(?m)^\*\*(failed|gave up)\*\* by')).Count
    $none = ([regex]::Matches($all, '(?m)^\*\*no change\*\* by')).Count
    $head = @"
Work by the CLONE-Z Night Shift: local AI agents (Ollama on the DGX Spark and the helper machines, no cloud tokens) working through ``Tools/NightShift/tasks.md``.

**$landed landed, $failed failed, $none no change.** Every change compiled against Unity's assemblies before it landed, but the agents can't play the game, so **play-test in Unity before merging**: ``git fetch origin $branch`` then ``git switch $branch``.

``NightShift.ps1 pr`` opened this and keeps it up to date whenever the night shift has nothing left to do; tasks finished later land on the same branch.
"@
    $body = (@($head) + @($texts | Where-Object { $_ })) -join "`n`n---`n`n"
    if ($body.Length -gt 60000) {
        $body = $body.Substring(0, 60000) + "`n`n_(Cut short at GitHub's limit; the full reports are in Tools/NightShift/reports/ on the branch.)_"
    }
    $title = "Night shift: $landed task$(if ($landed -ne 1) { 's' }) landed on $branch"
    $file = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($file, $body, (New-Object Text.UTF8Encoding $false))
        if ($open.Count -gt 0) {
            # No retries for edits and creates: a retry after a reply that got lost could open a second pull request.
            Invoke-Tool 'gh pr edit' gh @('pr', 'edit', "$($open[0].number)", '--repo', $repo, '--title', $title, '--body-file', $file) 1 | Out-Null
            Write-PrLog "Updated $($open[0].url): $landed landed, $failed failed"
        }
        else {
            $url = Invoke-Tool 'gh pr create' gh @('pr', 'create', '--repo', $repo, '--base', 'main', '--head', $branch, '--title', $title, '--body-file', $file) 1 |
                Where-Object { $_ -match '^https://' } | Select-Object -Last 1
            Write-PrLog "Opened $url : $landed landed, $failed failed"
        }
    }
    finally { Remove-Item $file -ErrorAction SilentlyContinue }
}

function Get-AutoPr { return [bool](Get-ScheduledTask -TaskName $AutoPrTask -ErrorAction SilentlyContinue) }

function Set-AutoPr([bool]$On) {
    if (-not $On) {
        Unregister-ScheduledTask -TaskName $AutoPrTask -Confirm:$false -ErrorAction SilentlyContinue
        Write-Host 'The automatic pull request is off.'
        return
    }
    $ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
    # conhost --headless keeps the check every 15 minutes from flashing a console window.
    $action = New-ScheduledTaskAction -Execute (Join-Path $env:SystemRoot 'System32\conhost.exe') -WorkingDirectory $Here `
        -Argument "--headless `"$ps`" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$PSCommandPath`" pr -WhenDone -SparkHost `"$SparkHost`" -SparkProject `"$SparkProject`" -Container `"$Container`""
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 15)
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 10)
    Register-ScheduledTask -TaskName $AutoPrTask -Action $action -Trigger $trigger -Settings $settings -Force `
        -Description "Opens or updates the GitHub pull request for the CLONE-Z Night Shift's branch once the agents have nothing left to do ($PSCommandPath pr -WhenDone)." | Out-Null
    Write-Host 'The automatic pull request is on. Every 15 minutes this PC checks the Spark; once nothing is left to do, it pushes'
    Write-Host "the night shift's branch to GitHub and opens or updates its pull request. Log: $Here\.state\pr.log"
}

#endregion

function Show-Menu {
    while ($true) {
        Write-Host ''
        Write-Host '=== CLONE-Z Night Shift (local AI on the DGX Spark) ===' -ForegroundColor Cyan
        Write-Host ' 1) Add a task'
        Write-Host ' 2) Start the night shift'
        Write-Host ' 3) Status'
        Write-Host ' 4) Watch the live log'
        Write-Host ' 5) Finish the tasks in progress, then stop'
        Write-Host ' 6) Stop now'
        Write-Host ' 7) Fetch the results into this repo'
        Write-Host ' 8) Upload Unity references (once, and after Unity/package changes)'
        Write-Host ' 9) Set up helper machines'
        Write-Host ' P) Open or update the GitHub pull request now'
        Write-Host " A) Automatic pull request when the work is done: $(if (Get-AutoPr) { 'ON (A turns it off)' } else { 'off (A turns it on)' })"
        Write-Host ' Q) Quit (a running night shift keeps going on the Spark)'
        switch ((Read-Host 'Choose').Trim().ToUpper()) {
            '1' { $script:Task = $null; Add-Task }
            '2' { Start-NightShift | Out-Null }
            '3' { Invoke-Container 'status' | Out-Null }
            '4' { Watch-Log }
            '5' { Invoke-Container 'finish' | Out-Null }
            '6' { Stop-NightShift | Out-Null }
            '7' { Get-NightBranches }
            '8' { Send-UnityRefs }
            '9' { Install-Helpers }
            'P' { try { Publish-NightShift } catch { Write-Host $_.Exception.Message -ForegroundColor Red } }
            'A' { Set-AutoPr (-not (Get-AutoPr)) }
            'Q' { return }
        }
    }
}

if ($MyInvocation.InvocationName -eq '.') { return }  # dot-sourced (tests): only define the functions

switch ($Action) {
    'refs' { Send-UnityRefs }
    'helpers' { Install-Helpers }
    'add' { Add-Task }
    'start' { exit (Start-NightShift) }
    'status' { exit (Invoke-Container 'status') }
    'watch' { Watch-Log }
    'finish' { exit (Invoke-Container 'finish') }
    'stop' { exit (Stop-NightShift) }
    'fetch' { Get-NightBranches }
    'pr' {
        try { Publish-NightShift }
        catch { Write-PrLog "Pull request not made: $($_.Exception.Message)"; exit 1 }
    }
    'autopr' { Set-AutoPr (-not $Off) }
    default { Show-Menu }
}
