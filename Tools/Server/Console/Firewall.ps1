<#
.SYNOPSIS
    Adds the release console's Windows Firewall rule, or removes it with -Remove.

.DESCRIPTION
    The rule lets in TCP -Port from the local subnet only, for -Program (pythonw.exe) on the given network profiles. It
    needs administrator rights. Run without them, as Install.ps1 and Uninstall.ps1 run it, it asks Windows for them
    (one UAC prompt) from a hidden helper and returns at once without waiting on the prompt; they then watch for the rule
    to come or go. What the elevated copy did goes in %LOCALAPPDATA%\CloneZ\console\firewall.log.
#>
param(
    [ValidateRange(1, 65535)][int]$Port = 8095,
    [string]$Program,
    [ValidateSet('Domain', 'Private', 'Public')][string[]]$NetworkProfile = @('Private'),
    [switch]$Remove
)
$ErrorActionPreference = 'Stop'
$name = 'WithTheDead release console'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) {
    # The elevated copy's command line travels encoded, so no path's quotes go wrong; the helper is the one that waits
    # on the prompt (Start-Process -Verb RunAs holds until it's answered).
    $quote = { param($s) "'" + "$s".Replace("'", "''") + "'" }
    $command = "& $(& $quote $PSCommandPath) -Port $Port -NetworkProfile $(($NetworkProfile | ForEach-Object { & $quote $_ }) -join ',')"
    if ($Program) { $command += " -Program $(& $quote $Program)" }
    if ($Remove) { $command += ' -Remove' }
    $inner = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
    $helper = "Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', '$inner'"
    Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand',
        ([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($helper)))
    Write-Host 'Windows asks for administrator rights for the firewall rule (UAC); it may be behind other windows.'
    return
}

$log = Join-Path $env:LOCALAPPDATA 'CloneZ\console\firewall.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
try {
    Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    if ($Remove) { $done = 'removed the rule' }
    else {
        $rule = @{ DisplayName = $name; Direction = 'Inbound'; Action = 'Allow'; Protocol = 'TCP'; LocalPort = $Port
            RemoteAddress = 'LocalSubnet'; Profile = ($NetworkProfile -join ','); Description = 'The WithTheDead release console (Tools\Server\Console), for the LAN.' }
        if ($Program) { $rule.Program = $Program }
        New-NetFirewallRule @rule | Out-Null
        $done = "added the rule: TCP $Port from the local subnet, $($NetworkProfile -join '/'), $(if ($Program) { $Program } else { 'any program' })"
    }
    Add-Content $log "$(Get-Date -Format s) $done"
    Write-Host ($done.Substring(0, 1).ToUpper() + $done.Substring(1) + '.')
}
catch {
    Add-Content $log "$(Get-Date -Format s) failed: $_"
    throw
}
