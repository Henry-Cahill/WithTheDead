<#
.SYNOPSIS
  Prepares this Windows PC to be a CLONE-Z Night Shift helper. Run it once ON that PC, in PowerShell as administrator.

.DESCRIPTION
  The Spark's coordinator drives its helpers over SSH: it runs bash scripts there and pushes and fetches git. So this:
    - installs the OpenSSH server, starts it with Windows and lets this network (only) reach it;
    - installs Git for Windows (with winget) if it's missing, and makes Git Bash the shell OpenSSH runs commands in, so
      the night shift's scripts and git over SSH work as on its Linux helpers;
    - installs Python 3 (with winget) if it's missing: the night shift's worker is a Python script;
    - authorizes your PC's SSH key for the helper account, so NightShift.ps1 on your PC can finish the setup.
  Nothing else changes. Then, on your PC, connect once (ssh <user>@<this PC>, to remember its host key) and run
  NightShift.ps1 helpers: it installs OpenCode and .NET for the helper account, lets the Spark in and uploads Unity's
  references. Running this again is safe.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .\WindowsHelper.ps1 -AuthorizedKey 'ssh-ed25519 AAAA... you@your-pc'
#>
param(
    # Your PC's public key: the one line in %USERPROFILE%\.ssh\id_ed25519.pub there. More than one may be given, each with
    # a from="address" in front if it should only work from there (as NightShift.ps1 helpers adds the Spark's).
    [Parameter(Mandatory = $true)][string[]]$AuthorizedKey,
    # The account the night shift works as on this PC (default: the one running this).
    [string]$User = $env:USERNAME
)

$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this in PowerShell as administrator (right-click Windows PowerShell, then Run as administrator).'
}
$keys = foreach ($key in $AuthorizedKey) {
    if ($key.Trim() -notmatch '^(from="[^"]*" )?(ssh-ed25519|ssh-rsa|ecdsa-sha2-nistp\d+) ([A-Za-z0-9+/]+=*)( .*)?$') {
        throw "That isn't an SSH public key (it should start with ssh-ed25519): $key"
    }
    [pscustomobject]@{ Line = $key.Trim(); Body = $Matches[3] }
}

function Write-Step([string]$Text) { Write-Host "`n$Text" -ForegroundColor Cyan }

function Install-Package([string]$Id) {
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) { throw "winget isn't available to install $Id; install it by hand, then run this again." }
    & winget install --id $Id --exact --source winget --scope machine --silent --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) { throw "winget couldn't install $Id (exit $LASTEXITCODE)." }
}

function Find-GitBash {
    foreach ($root in @((Get-ItemProperty 'HKLM:\SOFTWARE\GitForWindows' -ErrorAction SilentlyContinue).InstallPath, (Join-Path $env:ProgramFiles 'Git'))) {
        if ($root -and (Test-Path (Join-Path $root 'bin\bash.exe'))) { return (Join-Path $root 'bin\bash.exe') }
    }
}

# A Python 3.9 or newer that the helper account will find. The python.exe in WindowsApps only offers the Microsoft Store.
function Find-Python {
    $candidates = @(Get-Command python, py -CommandType Application -All -ErrorAction SilentlyContinue | Where-Object { $_.Source -notmatch '\\WindowsApps\\' } | ForEach-Object Source)
    $candidates += @(Join-Path $env:SystemRoot 'py.exe') + @(Get-ChildItem (Join-Path $env:ProgramFiles 'Python3*\python.exe') -ErrorAction SilentlyContinue | ForEach-Object FullName)
    foreach ($exe in $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique) {
        $pyArgs = @(if ((Split-Path $exe -Leaf) -eq 'py.exe') { '-3' })
        $version = & $exe @pyArgs -c 'import sys; print(sys.version.split()[0]); sys.exit(sys.version_info < (3, 9))' 2>$null
        if ($LASTEXITCODE -eq 0) { return "$exe ($version)" }
    }
}

Write-Step 'OpenSSH server...'
if (-not (Get-Service sshd -ErrorAction SilentlyContinue)) {
    $capability = Get-WindowsCapability -Online -Name 'OpenSSH.Server*' | Select-Object -First 1
    if (-not $capability) { throw "This Windows doesn't offer the OpenSSH server (Settings > System > Optional features)." }
    Add-WindowsCapability -Online -Name $capability.Name | Out-Null
}
Set-Service sshd -StartupType Automatic
Start-Service sshd
$rule = 'CLONE-Z Night Shift SSH'
if (-not (Get-NetFirewallRule -DisplayName $rule -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $rule -Description 'The CLONE-Z Night Shift reaches this helper over SSH from this network.' `
        -Direction Inbound -Protocol TCP -LocalPort 22 -RemoteAddress LocalSubnet -Action Allow -Profile Any | Out-Null
}
Write-Host '  running, starts with Windows, reachable from this network'

Write-Step 'Git for Windows...'
$bash = Find-GitBash
if (-not $bash) {
    Install-Package 'Git.Git'
    $bash = Find-GitBash
    if (-not $bash) { throw "Git for Windows didn't install; install it from https://git-scm.com/download/win, then run this again." }
}
Write-Host "  $bash"

Write-Step 'Python 3...'
$python = Find-Python
if (-not $python) {
    Install-Package 'Python.Python.3.12'
    $python = Find-Python
    if (-not $python) { throw "Python didn't install; install Python 3 from https://www.python.org (for all users), then run this again." }
}
Write-Host "  $python"

Write-Step 'Git Bash as the shell OpenSSH runs commands in...'
$openSsh = 'HKLM:\SOFTWARE\OpenSSH'
if (-not (Test-Path $openSsh)) { New-Item -Path $openSsh -Force | Out-Null }
$previous = (Get-ItemProperty $openSsh -ErrorAction SilentlyContinue).DefaultShell
if ($previous -and $previous -ne $bash) { Write-Host "  (it was $previous)" -ForegroundColor Yellow }
New-ItemProperty -Path $openSsh -Name DefaultShell -Value $bash -PropertyType String -Force | Out-Null
# bash takes -c, OpenSSH's default; a /c left from cmd.exe would break every command.
Remove-ItemProperty -Path $openSsh -Name DefaultShellCommandOption -ErrorAction SilentlyContinue
Write-Host "  $bash"

Write-Step "SSH keys for $User..."
$sid = (New-Object Security.Principal.NTAccount($User)).Translate([Security.Principal.SecurityIdentifier]).Value
try { $isAdmin = [bool](Get-LocalGroupMember -SID 'S-1-5-32-544' -ErrorAction Stop | Where-Object { $_.SID.Value -eq $sid }) }
catch { $isAdmin = $sid -eq $identity.User.Value }  # Get-LocalGroupMember trips over orphaned members; this account is one
if ($isAdmin) {
    # OpenSSH reads every administrator's keys from here, and only if nobody but SYSTEM and Administrators may open it.
    $keyFile = Join-Path $env:ProgramData 'ssh\administrators_authorized_keys'
}
else {
    $userHome = (Get-CimInstance Win32_UserProfile -Filter "SID = '$sid'").LocalPath
    if (-not $userHome) { throw "$User hasn't signed in to this PC yet; sign in as $User once, then run this again." }
    $keyFile = Join-Path $userHome '.ssh\authorized_keys'
}
New-Item -ItemType Directory -Force -Path (Split-Path $keyFile) | Out-Null
foreach ($key in $keys) {
    $existing = if (Test-Path $keyFile) { [IO.File]::ReadAllText($keyFile) } else { '' }
    if (-not $existing.Contains($key.Body)) {
        $separator = if ($existing -and -not $existing.EndsWith("`n")) { "`n" } else { '' }
        [IO.File]::AppendAllText($keyFile, "$separator$($key.Line)`n", (New-Object Text.UTF8Encoding($false)))
    }
}
if ($isAdmin) {
    & icacls.exe $keyFile /inheritance:r /grant '*S-1-5-32-544:F' /grant '*S-1-5-18:F' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Couldn't restrict access to $keyFile (icacls exit $LASTEXITCODE)." }
}
Write-Host "  in $keyFile"

# The night shift can't use a PC while it sleeps.
$sleep = [regex]::Matches((powercfg /query SCHEME_CURRENT SUB_SLEEP STANDBYIDLE) -join "`n", '0x[0-9a-fA-F]{8}')
if ($sleep.Count -ge 2 -and [Convert]::ToInt64($sleep[$sleep.Count - 2].Value, 16) -gt 0) {
    $minutes = [Convert]::ToInt64($sleep[$sleep.Count - 2].Value, 16) / 60
    Write-Host "`nThis PC goes to sleep after $minutes minutes on mains power. Turn that off (Settings > System > Power) if it should work through the night." -ForegroundColor Yellow
}

$name = $env:COMPUTERNAME.ToLower()
Write-Host "`nThis PC is ready. On your PC, finish with:" -ForegroundColor Green
Write-Host "  ssh $(($User -split '\\')[-1].ToLower())@$name exit        (once: answer yes to remember this PC)" -ForegroundColor Green
Write-Host '  Tools\NightShift\NightShift.ps1 helpers' -ForegroundColor Green
