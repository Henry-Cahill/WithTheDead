<#
.SYNOPSIS
    Stops the release console and undoes Install.ps1: its scheduled task and its firewall rule (Windows asks once for
    administrator rights to remove that).

.DESCRIPTION
    A job the console is running stops with it. The job history, the logs and the settings (the itch.io project and API
    key) stay in %LOCALAPPDATA%\CloneZ\console unless you pass -Purge. butler and its itch.io login stay too.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Server\Console\Uninstall.ps1
#>
param([switch]$Purge)
$ErrorActionPreference = 'Stop'
$name = 'WithTheDead release console'

if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
    Stop-ScheduledTask -TaskName $name
    Unregister-ScheduledTask -TaskName $name -Confirm:$false
    Write-Host "Stopped the console and removed its task, ""$name""."
}
else { Write-Host "There's no task ""$name""." }

if (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue) {
    & (Join-Path $PSScriptRoot 'Firewall.ps1') -Remove
    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue) {
        Write-Warning "The firewall rule ""$name"" is still there: answer the UAC prompt, or run this again."
    }
    else { Write-Host 'Removed the firewall rule.' }
}

$data = Join-Path $env:LOCALAPPDATA 'CloneZ\console'
if ($Purge -and (Test-Path $data)) {
    Remove-Item $data -Recurse -Force
    Write-Host "Deleted $data (the job history, the logs and the settings)."
}
elseif (Test-Path $data) { Write-Host "The job history, the logs and the settings stay in $data (-Purge deletes them)." }
