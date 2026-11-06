<#
.SYNOPSIS
    Sets the release console (console.py beside this script) to start whenever you sign in to Windows, lets the LAN reach
    it through Windows Firewall, and starts it now.

.DESCRIPTION
    The console runs as you, with no window (pythonw.exe), from a scheduled task that starts when you sign in: as you, so
    that ssh to hts-llm and Unity's licence work as they do in your own shell. It runs at normal priority (a task's
    default is below normal, which would slow builds down) and for as long as it likes (a task's default is 3 days).

    The firewall rule (Firewall.ps1) lets in TCP -Port from the local subnet only, for pythonw.exe, on the network profile
    this PC is on now. It needs administrator rights, so Windows asks once (UAC). This script doesn't wait on that
    prompt: it watches for the rule for three minutes, then carries on without it (answer the prompt later and the rule
    still comes). Then it starts the console through its task, so it doesn't belong to this shell.

    Run it again after moving the project or Python. Uninstall.ps1 undoes it.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Server\Console\Install.ps1
#>
param(
    # The console's port (not 8090: Dell Repository Manager's service takes that on 127.0.0.1 now and then).
    [ValidateRange(1, 65535)][int]$Port = 8095,
    # pythonw.exe to run the console with (default: the one beside the python.exe on the PATH).
    [string]$Python
)
$ErrorActionPreference = 'Stop'
$name = 'WithTheDead release console'
$console = Join-Path $PSScriptRoot 'console.py'
$local = "http://localhost:$Port"

# pythonw.exe, beside a real python.exe (not the Microsoft Store's stand-in for one).
if (-not $Python) {
    $found = Get-Command python.exe -All -ErrorAction SilentlyContinue | Where-Object { $_.Source -notlike '*\WindowsApps\*' } |
        Select-Object -First 1
    if ($found) { $Python = Join-Path (Split-Path $found.Source) 'pythonw.exe' }
}
if (-not $Python -or -not (Test-Path $Python)) { throw 'No pythonw.exe found: pass -Python <path to pythonw.exe> (Python 3.10 or later).' }
$Python = (Resolve-Path $Python).Path
$version = (& (Join-Path (Split-Path $Python) 'python.exe') -c 'import sys; print(sys.version.split()[0])').Trim()
if ([version]$version -lt [version]'3.10') { throw "The console needs Python 3.10 or later; $Python is $version." }
Write-Host "Python $version ($Python)"

# A console that's running a job isn't stopped under it.
try { $busy = (Invoke-RestMethod "$local/api/jobs" -TimeoutSec 5).current } catch { $busy = $null }
if ($busy) { throw "The console is running ""$($busy.title)"": wait for it to end, or cancel it, then run this again." }

#region Firewall
$profiles = @(Get-NetConnectionProfile | ForEach-Object {
        Write-Host "Network: $($_.InterfaceAlias) ($($_.Name)) is $($_.NetworkCategory)."
        if ("$($_.NetworkCategory)" -eq 'DomainAuthenticated') { 'Domain' } else { "$($_.NetworkCategory)" }
    } | Select-Object -Unique)
if (-not $profiles) { $profiles = @('Private') }
if ($profiles -contains 'Public') {
    Write-Warning ('Windows counts this network as Public. The rule lets in only the local subnet, but if this is your home ' +
        'network, make it Private (Settings > Network & internet > its properties) and run this again.')
}
function Test-Rule {
    $rule = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $rule -or "$($rule.Enabled)" -ne 'True') { return $false }
    $ports = @(($rule | Get-NetFirewallPortFilter).LocalPort)
    $program = ($rule | Get-NetFirewallApplicationFilter).Program
    $on = "$($rule.Profile)" -split ',\s*'
    return ($ports -contains "$Port") -and ($program -eq $Python) -and -not ($profiles | Where-Object { $on -notcontains $_ })
}
if (Test-Rule) { Write-Host "The firewall rule is in place already (TCP $Port, $($profiles -join '/'))." }
else {
    & (Join-Path $PSScriptRoot 'Firewall.ps1') -Port $Port -Program $Python -NetworkProfile $profiles
    $deadline = (Get-Date).AddMinutes(3)
    while (-not (Test-Rule) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }
    if (Test-Rule) { Write-Host "The firewall rule is in place: TCP $Port from the local subnet, $($profiles -join '/')." -ForegroundColor Green }
    else {
        Write-Warning ("No firewall rule after 3 minutes, so other machines can't reach the console yet. Answer the UAC " +
            'prompt if it is still up (the rule then comes by itself), or run this script again.')
    }
}
#endregion

#region Task
$user = "$env:USERDOMAIN\$env:USERNAME"
$action = New-ScheduledTaskAction -Execute $Python -Argument "`"$console`" --port $Port" -WorkingDirectory $PSScriptRoot
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -Priority 5 -MultipleInstances IgnoreNew `
    -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
$principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) { Stop-ScheduledTask -TaskName $name }
$null = Register-ScheduledTask -TaskName $name -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force `
    -Description "The WithTheDead release console ($console) on port $Port, from Install.ps1."
Write-Host "The task ""$name"" starts the console when $user signs in."

$deadline = (Get-Date).AddSeconds(15)
# Anything bound to the port, listening or not (Dell Repository Manager's service binds 8090 that way now and then).
function Get-PortHolder {
    Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue | Where-Object { $_.RemotePort -eq 0 -and $_.OwningProcess } |
        Select-Object -First 1
}
while ((Get-PortHolder) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 500 }
$holder = Get-PortHolder
if ($holder) { throw "Port $Port is taken by process $($holder.OwningProcess) ($((Get-Process -Id $holder.OwningProcess).ProcessName)): stop it, or pass -Port." }
Start-ScheduledTask -TaskName $name
$deadline = (Get-Date).AddSeconds(45)
do {
    Start-Sleep -Seconds 1
    try { $status = Invoke-RestMethod "$local/api/status" -TimeoutSec 40 } catch { $status = $null }
    $state = "$((Get-ScheduledTask -TaskName $name).State)"
} until (($status -and $status.console) -or $state -ne 'Running' -or (Get-Date) -gt $deadline)
if (-not ($status -and $status.console)) {
    Get-Content (Join-Path $env:LOCALAPPDATA 'CloneZ\console\console.log') -Tail 5 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $_" }
    throw "The console didn't answer on $local (its task is $state); the end of its log, console.log in $env:LOCALAPPDATA\CloneZ\console, is above."
}
#endregion

Write-Host "The release console is running: $($status.console.urls -join '  ')" -ForegroundColor Green
