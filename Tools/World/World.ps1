<#
.SYNOPSIS
    The CLONE-Z world store on the NAS (\\192.168.1.170\CLONE-Z, /mnt/clonez on the Linux machines).

.DESCRIPTION
    connect   Mounts the store at /mnt/clonez on each Linux machine, once (see connect-nas.sh). It asks for each
              machine's sudo password in this window; machines that allow sudo without one don't ask.
    status    Which machines have the store mounted, and what it holds.

.EXAMPLE
    .\World.ps1 connect
    .\World.ps1 connect -Machines htsadmin@192.168.1.203
#>
param(
    [Parameter(Position = 0)][ValidateSet('connect', 'status')][string]$Action = 'status',
    # The Linux machines that build the world: the Spark and the game servers.
    [string[]]$Machines = @('henrylawrencecahill@192.168.1.252', 'htsadmin@192.168.1.200', 'htsadmin@192.168.1.201',
        'htsadmin@192.168.1.202'),
    [string]$Store = '\\192.168.1.170\CLONE-Z'
)

$ErrorActionPreference = 'Stop'
$Ssh = @('-o', 'ConnectTimeout=10')
# "powershell -File World.ps1 -Machines a,b" passes "a,b" as one string, so split lists here.
$Machines = @($Machines | ForEach-Object { $_ -split '[,;\s]+' } | Where-Object { $_ })

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
            # No double quotes in remote commands: PowerShell 5 doesn't escape them for native programs.
            $out = & ssh -o BatchMode=yes @Ssh $m 'ls /mnt/clonez >/dev/null 2>&1; findmnt -n -t nfs4 -o SOURCE /mnt/clonez || echo not mounted' 2>&1
            "{0,-36} {1}" -f $m, ("$out".Trim())
        }
        if (Test-Path $Store) {
            Write-Host "`n$Store" -ForegroundColor Cyan
            Get-ChildItem $Store -Force | Where-Object { $_.Name -notlike '.*' -and $_.Name -ne '#recycle' } |
                Format-Table Mode, LastWriteTime, Name -AutoSize | Out-String | Write-Host
        }
        else { Write-Host "Can't reach $Store from this PC" -ForegroundColor Red }
    }
}
