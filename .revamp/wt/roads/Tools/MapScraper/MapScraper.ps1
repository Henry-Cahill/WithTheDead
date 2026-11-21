<#
.SYNOPSIS
  One-click control for the zomboidmap.com map rip (Project Zomboid Build 42), running on this PC.

.DESCRIPTION
  The rip runs as a hidden background Python process, so it keeps going after this window or VS Code
  closes. It keeps the PC awake while it works, skips everything already on disk (so 'start' always
  resumes), and stops cleanly on request.

  Run with no arguments for a menu, or:  .\MapScraper.ps1 plan|start|status|watch|stop|export|stitch|topdown|open

.EXAMPLE
  .\MapScraper.ps1 start                           # Louisville: all data + every floor at every zoom level
  .\MapScraper.ps1 start -DataOnly                 # only the data files and region exports (a few minutes)
  .\MapScraper.ps1 start -Region all -Levels 3-11  # whole map, without the three most detailed levels
  .\MapScraper.ps1 start -Extra '--retry-missing'  # pass extra 'mapscraper.py run' flags
  .\MapScraper.ps1 topdown -PxPerSquare 8          # rebuild the top-down maps from L3 tiles
#>
param(
    [ValidateSet('menu', 'plan', 'start', 'status', 'watch', 'stop', 'export', 'stitch', 'topdown', 'open')]
    [string]$Action = 'menu',
    [string]$Region = 'louisville',
    [string]$Levels = 'all',
    [string]$Floors = 'all',
    [int]$Workers = 6,
    [double]$Rate = 8,
    [switch]$DataOnly,
    [switch]$Force,
    [string]$Level = 'auto',
    [int]$PxPerSquare = 4,
    [string]$Out = $(if ($env:CLONEZ_MAP_OUT) { $env:CLONEZ_MAP_OUT } else { Join-Path $PSScriptRoot 'out' }),
    [string[]]$Extra = @()
)

$ErrorActionPreference = 'Stop'
$Here = $PSScriptRoot
$Script = Join-Path $Here 'mapscraper.py'
$Out = [IO.Path]::GetFullPath($Out)
# PowerShell 5.1 does not escape a trailing backslash when it quotes a path for python.exe.
if ($Out.Length -gt 3) { $Out = $Out.TrimEnd('\') }
$State = Join-Path $Out 'state'
$LogFile = Join-Path $Out 'run.log'
$PidFile = Join-Path $State 'run.pid'
$StopFile = Join-Path $State 'STOP'
try { [Console]::OutputEncoding = [Text.Encoding]::UTF8 } catch { }

function Find-Python {
    if ($script:PythonExe) { return }
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        foreach ($name in 'python', 'py') {
            if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { continue }
            $exe = & $name -c 'import sys; print(sys.executable)' 2>$null | Select-Object -First 1
            if ($exe -and (Test-Path $exe.Trim())) { $script:PythonExe = $exe.Trim(); break }
        }
    } finally { $ErrorActionPreference = $saved }
    if (-not $script:PythonExe) {
        throw 'Python 3 was not found. Install it from https://www.python.org/ and tick "Add python.exe to PATH".'
    }
    # pythonw has no console window, so the background run cannot be closed by accident.
    $windowless = Join-Path (Split-Path $script:PythonExe) 'pythonw.exe'
    $script:PythonBackground = $(if (Test-Path $windowless) { $windowless } else { $script:PythonExe })
}

function Format-Number([double]$Value) { $Value.ToString([Globalization.CultureInfo]::InvariantCulture) }

# Start-Process joins arguments with spaces and no quoting, so quote them the way Windows parses them.
function Join-Arguments([string[]]$Items) {
    ($Items | ForEach-Object {
        if ($_ -eq '') { '""' }
        elseif ($_ -match '[\s"]') { '"' + (($_ -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"' }
        else { $_ }
    }) -join ' '
}

function Invoke-Py([string[]]$PyArgs) {
    Find-Python
    $env:PYTHONIOENCODING = 'utf-8'
    & $script:PythonExe -X utf8 $Script @PyArgs | Out-Host
    return $LASTEXITCODE
}

function Get-RegionArgs { @('--out', $Out, "--region=$Region") }

function Get-RunProcess {
    if (-not (Test-Path $PidFile)) { return $null }
    $procId = (Get-Content $PidFile -ErrorAction SilentlyContinue | Select-Object -First 1)
    if (-not ($procId -match '^\s*\d+\s*$')) { return $null }
    $proc = Get-CimInstance Win32_Process -Filter "ProcessId = $($procId.Trim())" -ErrorAction SilentlyContinue
    if ($proc -and $proc.CommandLine -like '*mapscraper.py*') { return $proc }
    return $null
}

function Test-Pillow {
    Find-Python
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $script:PythonExe -c 'import PIL' 2>$null
    $ok = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $saved
    if (-not $ok) {
        Write-Host 'Pillow is not installed, so heatmap crops, mosaics and top-down maps will be skipped.' -ForegroundColor Yellow
        Write-Host "Install it with:  & '$($script:PythonExe)' -m pip install pillow" -ForegroundColor Yellow
    }
}

function Invoke-Plan {
    $null = Invoke-Py (@('plan') + (Get-RegionArgs) + @("--levels=$Levels", "--floors=$Floors", '--rate', (Format-Number $Rate)) + $Extra)
}

function Start-Rip {
    $running = Get-RunProcess
    if ($running) {
        Write-Host "A rip is already running (PID $($running.ProcessId)). Use status, watch or stop." -ForegroundColor Yellow
        return
    }
    Find-Python
    Test-Pillow
    New-Item -ItemType Directory -Force -Path $State | Out-Null
    Remove-Item $StopFile -ErrorAction SilentlyContinue
    $pyArgs = @('-X', 'utf8', $Script, 'run') + (Get-RegionArgs) +
              @("--levels=$Levels", "--floors=$Floors", '--workers', "$Workers", '--rate', (Format-Number $Rate))
    if ($DataOnly) { $pyArgs += '--no-tiles' }
    $pyArgs += $Extra
    $proc = Start-Process -FilePath $script:PythonBackground -ArgumentList (Join-Arguments $pyArgs) `
        -WorkingDirectory $Here -WindowStyle Hidden -PassThru
    $null = $proc.Handle  # keeps the exit code readable after the process ends
    Set-Content -Path $PidFile -Value $proc.Id -Encoding Ascii
    $what = $(if ($DataOnly) { 'data-only rip' } else { 'full rip' })
    $where = $(if (($Extra -contains '--cells') -or ($Extra -contains '--bbox')) { 'a custom region' } else { "'$Region'" })
    Write-Host "Started the $what of $where in the background (PID $($proc.Id))." -ForegroundColor Green
    Start-Sleep -Seconds 5
    if ($proc.HasExited) {
        Write-Host "It stopped straight away (exit code $($proc.ExitCode)). Latest log lines:" -ForegroundColor Red
        if (Test-Path $LogFile) { Get-Content $LogFile -Tail 15 | Out-Host }
        return
    }
    if (Test-Path $LogFile) { Get-Content $LogFile -Tail 5 | Out-Host }
    Write-Host ''
    Write-Host 'It keeps running if you close this window, and keeps the PC awake until it is done.' -ForegroundColor Green
    Write-Host 'Check in with:  status | watch | stop' -ForegroundColor Green
}

function Show-Status {
    $proc = Get-RunProcess
    if ($proc) { Write-Host "== RUNNING (PID $($proc.ProcessId)) ==" -ForegroundColor Green }
    else { Write-Host '== not running ==' -ForegroundColor Yellow }
    $null = Invoke-Py @('status', '--out', $Out)
    if (Test-Path $LogFile) {
        Write-Host ''
        Write-Host 'Latest log lines:' -ForegroundColor Cyan
        Get-Content $LogFile -Tail 6 | Out-Host
    }
}

function Watch-Rip {
    if (-not (Test-Path $LogFile)) { Write-Host 'No log yet - start a rip first.' -ForegroundColor Yellow; return }
    Write-Host 'Live log. Press Q or Esc to stop watching (the rip keeps going).' -ForegroundColor Cyan
    Get-Content $LogFile -Tail 20 | Out-Host
    $stream = [IO.File]::Open($LogFile, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]'ReadWrite, Delete')
    $reader = New-Object IO.StreamReader($stream, [Text.Encoding]::UTF8)
    try {
        [void]$stream.Seek(0, [IO.SeekOrigin]::End)
        while ($true) {
            $line = $reader.ReadLine()
            if ($null -ne $line) { Write-Host $line; continue }
            try {
                if ([Console]::KeyAvailable) {
                    $key = [Console]::ReadKey($true)
                    if ($key.Key -eq 'Q' -or $key.Key -eq 'Escape') { break }
                }
            } catch { }  # no interactive console (output redirected): Ctrl+C ends watching
            Start-Sleep -Milliseconds 500
        }
    } finally { $reader.Dispose() }
}

function Stop-Rip {
    $proc = Get-RunProcess
    if (-not $proc) {
        Write-Host 'Not running.' -ForegroundColor Yellow
        Remove-Item $StopFile -ErrorAction SilentlyContinue
        return
    }
    if ($Force) {
        Stop-Process -Id $proc.ProcessId -Force
        Remove-Item $PidFile, $StopFile -ErrorAction SilentlyContinue
        Write-Host "Killed PID $($proc.ProcessId). Unfinished files are thrown away; start resumes." -ForegroundColor Yellow
        return
    }
    New-Item -ItemType File -Force -Path $StopFile | Out-Null
    Write-Host 'Stop requested - letting the downloads in flight finish' -NoNewline -ForegroundColor Cyan
    for ($i = 0; $i -lt 120; $i++) {
        Start-Sleep -Seconds 1
        if (-not (Get-RunProcess)) {
            Write-Host ''
            Write-Host 'Stopped. Run start again to resume where it left off.' -ForegroundColor Green
            return
        }
        Write-Host '.' -NoNewline
    }
    Write-Host ''
    Write-Host 'Still finishing up (it may be saving an image). It exits on its own; use  stop -Force  to kill it.' -ForegroundColor Yellow
}

function Invoke-Export { $null = Invoke-Py (@('export') + (Get-RegionArgs) + $Extra) }

function Invoke-Stitch {
    $pyArgs = @('stitch') + (Get-RegionArgs) + @("--level=$Level")
    if ($script:FloorsGiven) { $pyArgs += "--floors=$Floors" }
    $null = Invoke-Py ($pyArgs + $Extra)
}

function Invoke-Topdown {
    $pyArgs = @('topdown') + (Get-RegionArgs) + @('--ppsq', "$PxPerSquare")
    if ($script:FloorsGiven) { $pyArgs += "--floors=$Floors" }
    $null = Invoke-Py ($pyArgs + $Extra)
}

function Open-Output {
    New-Item -ItemType Directory -Force -Path $Out | Out-Null
    Invoke-Item $Out
}

function Show-Menu {
    while ($true) {
        $proc = Get-RunProcess
        $now = $(if ($proc) { "RUNNING (PID $($proc.ProcessId))" } else { 'idle' })
        Write-Host ''
        Write-Host "=== CLONE-Z Map Scraper (zomboidmap.com) - region '$Region' - $now ===" -ForegroundColor Cyan
        Write-Host ' 1) Plan: what a full rip downloads (files, size, time)'
        Write-Host ' 2) Start / resume FULL rip (data + every floor at every zoom level)'
        Write-Host ' 3) Start / resume DATA ONLY (JSON / GeoJSON / CSV exports, a few minutes)'
        Write-Host ' 4) Status'
        Write-Host ' 5) Watch live log'
        Write-Host ' 6) Stop (finishes the downloads in flight; start resumes)'
        Write-Host ' 7) Rebuild region exports'
        Write-Host ' 8) Rebuild stitched mosaics'
        Write-Host ' 9) Rebuild top-down maps'
        Write-Host ' O) Open the output folder'
        Write-Host ' Q) Quit (a running rip keeps going in the background)'
        switch ((Read-Host 'Choose').Trim().ToUpper()) {
            '1' { Invoke-Plan }
            '2' { $script:DataOnly = $false; Start-Rip }
            '3' { $script:DataOnly = $true; Start-Rip; $script:DataOnly = $false }
            '4' { Show-Status }
            '5' { Watch-Rip }
            '6' { Stop-Rip }
            '7' { Invoke-Export }
            '8' { Invoke-Stitch }
            '9' { Invoke-Topdown }
            'O' { Open-Output }
            'Q' { return }
        }
    }
}

$script:FloorsGiven = $PSBoundParameters.ContainsKey('Floors')

switch ($Action) {
    'plan' { Invoke-Plan }
    'start' { Start-Rip }
    'status' { Show-Status }
    'watch' { Watch-Rip }
    'stop' { Stop-Rip }
    'export' { Invoke-Export }
    'stitch' { Invoke-Stitch }
    'topdown' { Invoke-Topdown }
    'open' { Open-Output }
    default { Show-Menu }
}
