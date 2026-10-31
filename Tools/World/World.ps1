<#
.SYNOPSIS
    The CLONE-Z world store on the NAS (\\192.168.1.170\CLONE-Z, /mnt/clonez on the Linux machines), and the world
    builds made in it.

.DESCRIPTION
    connect   Mounts the store at /mnt/clonez on each Linux machine, once (see connect-nas.sh). It asks for each
              machine's sudo password in this window; machines that allow sudo without one don't ask.
    status    Which machines have the store mounted, the builds it holds and what each machine is doing.
    build     Starts a world build from Kentucky's OpenStreetMap extract, shared between the -Builders (each writes its
              own regions, at low priority so game servers aren't disturbed). Logs go to scratch/<machine>/.
    publish   Once every part of build -Id is done: checks it, writes its manifest and makes it the build the game
              and the servers load.
    atlas     Opens the Kentucky Atlas in the browser (atlas\atlas.py, on this PC): the current build's buildings over
              Kentucky's aerial photographs, to check each one's outline and kind and record corrections the game
              applies (world/validation in the store). Ctrl+C stops it.

.EXAMPLE
    .\World.ps1 connect
    .\World.ps1 build
    .\World.ps1 publish -Id ky-20261019-2310
    .\World.ps1 atlas
#>
param(
    [Parameter(Position = 0)][ValidateSet('connect', 'status', 'build', 'publish', 'atlas')][string]$Action = 'status',
    # The Linux machines that use the store: the Spark and the game servers.
    [string[]]$Machines = @('henrylawrencecahill@192.168.1.252', 'htsadmin@192.168.1.200', 'htsadmin@192.168.1.201',
        'htsadmin@192.168.1.202'),
    # The machines that build the world (the game servers; the Spark is busy with the night shift's model).
    [string[]]$Builders = @('htsadmin@192.168.1.200', 'htsadmin@192.168.1.201', 'htsadmin@192.168.1.202'),
    # The build to publish, or the name of a new one (default: ky-<date>-<time>).
    [string]$Id,
    [string]$Store = '\\192.168.1.170\CLONE-Z'
)

$ErrorActionPreference = 'Stop'
$Ssh = @('-o', 'ConnectTimeout=10')
# "powershell -File World.ps1 -Machines a,b" passes "a,b" as one string, so split lists here.
$Machines = @($Machines | ForEach-Object { $_ -split '[,;\s]+' } | Where-Object { $_ })
$Builders = @($Builders | ForEach-Object { $_ -split '[,;\s]+' } | Where-Object { $_ })

# Runs a bash script on a Linux machine. PowerShell 5 mangles quotes on native command lines, so the script travels
# base64-encoded and is decoded on the far side.
function Invoke-Remote([string]$Target, [string]$Script) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($Script -replace "`r", '') + "`n"))
    & ssh -o BatchMode=yes @Ssh $Target "echo $b64 | base64 -d | bash"
}

# uv runs worldbuild.py with the Python and the osmium package it declares; it installs into the user's home, no sudo.
$EnsureUv = 'export PATH="$HOME/.local/bin:$PATH"; command -v uv >/dev/null || curl -LsSf https://astral.sh/uv/install.sh | sh >/dev/null 2>&1'

switch ($Action) {
    'connect' {
        $script = Join-Path $PSScriptRoot 'connect-nas.sh'
        $failed = 0
        foreach ($m in $Machines) {
            Write-Host "`n== $m" -ForegroundColor Cyan
            # -t gives sudo a terminal to ask for the password in this window.
            & scp -q @Ssh $script "${m}:/tmp/connect-nas.sh"
            if ($LASTEXITCODE -ne 0) { Write-Host "Couldn't copy the script to $m" -ForegroundColor Red; $failed++; continue }
            & ssh -t @Ssh $m 'sudo bash /tmp/connect-nas.sh; rc=$?; rm -f /tmp/connect-nas.sh; exit $rc'
            if ($LASTEXITCODE -ne 0) { Write-Host "$m isn't connected yet (exit $LASTEXITCODE)" -ForegroundColor Red; $failed++ }
        }
        exit [int]($failed -gt 0)
    }
    'status' {
        foreach ($m in $Machines) {
            $out = Invoke-Remote $m 'ls /mnt/clonez >/dev/null 2>&1; findmnt -n -t nfs4 -o SOURCE /mnt/clonez || echo "not mounted"' 2>&1
            "{0,-36} {1}" -f $m, ("$out".Trim())
        }
        if (-not (Test-Path $Store)) { Write-Host "Can't reach $Store from this PC" -ForegroundColor Red; exit 1 }
        Invoke-Remote $Builders[0] "$EnsureUv; uv run -q --python 3.12 /mnt/clonez/tools/worldbuild.py status 2>&1 || true"
    }
    'build' {
        if (-not $Id) { $Id = 'ky-' + (Get-Date -Format 'yyyyMMdd-HHmm') }
        $tools = Join-Path $Store 'tools'
        New-Item -ItemType Directory -Force -Path $tools | Out-Null
        # Every builder runs the same copy, from the store; the build records its checksum.
        Copy-Item (Join-Path $PSScriptRoot 'worldbuild.py') (Join-Path $tools 'worldbuild.py') -Force
        $n = $Builders.Count
        for ($k = 0; $k -lt $n; $k++) {
            $m = $Builders[$k]
            Invoke-Remote $m @"
$EnsureUv
log=/mnt/clonez/scratch/`$(hostname)/extract-$Id.log
mkdir -p "`$(dirname "`$log")"
nohup nice -n 10 uv run -q --python 3.12 /mnt/clonez/tools/worldbuild.py extract --id $Id --part $k/$n >"`$log" 2>&1 </dev/null &
echo "`$(hostname): part $k/$n started (log: `$log)"
"@
        }
        Write-Host "Build $Id is under way on $n machines. When '.\World.ps1 status' lists all its parts:" -ForegroundColor Green
        Write-Host "  .\World.ps1 publish -Id $Id" -ForegroundColor Green
    }
    'publish' {
        if (-not $Id) { throw 'Say which build: -Id <name>' }
        Invoke-Remote $Builders[0] "$EnsureUv; uv run -q --python 3.12 /mnt/clonez/tools/worldbuild.py publish $Id"
        exit $LASTEXITCODE
    }
    'atlas' {
        $atlas = Join-Path $PSScriptRoot 'atlas\atlas.py'
        if (Get-Command python -ErrorAction SilentlyContinue) { & python $atlas --store $Store }
        elseif (Get-Command py -ErrorAction SilentlyContinue) { & py -3 $atlas --store $Store }
        else { throw 'The atlas needs Python 3.10 or later (winget install Python.Python.3.12)' }
        exit $LASTEXITCODE
    }
}
