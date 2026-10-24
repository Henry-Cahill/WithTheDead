<#
.SYNOPSIS
  One-click control for the CLONE-Z LLM asset pipeline running on the DGX Spark.

.DESCRIPTION
  The generation runs ON THE SPARK inside a tmux session, so it keeps going when this PC sleeps or
  VS Code closes. This script pushes the pipeline there, starts/stops it, shows progress, and pulls
  the finished models back into Assets/Resources/GeneratedItemModels.

  Run with no arguments for a menu, or:  .\AssetPipeline.ps1 start|status|pull|stop|watch|gallery

.EXAMPLE
  .\AssetPipeline.ps1 start -Critic
  .\AssetPipeline.ps1 start -Extra '--category','Clothing'
#>
param(
    [ValidateSet('menu', 'start', 'status', 'pull', 'stop', 'watch', 'gallery')]
    [string]$Action = 'menu',
    [string]$SparkHost = $(if ($env:CLONEZ_SPARK) { $env:CLONEZ_SPARK } else { 'henrylawrencecahill@192.168.1.252' }),
    [int]$Workers = 4,
    [switch]$Critic,
    [string[]]$Extra = @()
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$Remote = 'clonez-pipeline'          # under the Spark user's home
$Session = 'clonez'
$Ssh = @('-o', 'BatchMode=yes', '-o', 'ConnectTimeout=10')

# PowerShell 5 mangles quotes on native command lines and appends CRLF when piping to them, so remote
# scripts travel base64-encoded (only [A-Za-z0-9+/=], nothing to escape) and are decoded on the Spark.
function Invoke-Remote([string]$Script) {
    $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(($Script -replace "`r", '') + "`n"))
    & ssh @Ssh $SparkHost "echo $b64 | base64 -d | bash" | Out-Host
    return $LASTEXITCODE
}

function Push-Pipeline {
    Write-Host "Packing pipeline + item catalogue..." -ForegroundColor Cyan
    $tgz = Join-Path $env:TEMP 'clonez-push.tgz'
    $paths = @(
        'Tools/AssetPipeline/pipeline.py', 'Tools/AssetPipeline/catalog.py', 'Tools/AssetPipeline/geometry.py',
        'Tools/AssetPipeline/llm.py', 'Tools/AssetPipeline/prompts',
        'Assets/Resources/curated_items.txt', 'Assets/Resources/pz_items.txt',
        'Assets/Scripts/World/ItemModels.cs'
    )
    # Models already in the project are sent too, so the Spark resumes instead of redoing them.
    if (Test-Path (Join-Path $Root 'Assets/Resources/GeneratedItemModels')) { $paths += 'Assets/Resources/GeneratedItemModels' }
    & tar.exe -czf $tgz -C $Root --exclude '*.meta' --exclude '__pycache__' @paths
    if ($LASTEXITCODE -ne 0) { throw 'tar failed' }

    Write-Host "Uploading to $SparkHost..." -ForegroundColor Cyan
    & ssh @Ssh $SparkHost "mkdir -p ~/$Remote"
    & scp -q @Ssh $tgz "${SparkHost}:~/$Remote/push.tgz"
    if ($LASTEXITCODE -ne 0) { throw 'scp failed' }
    Remove-Item $tgz
}

function Start-Run {
    $running = Invoke-Remote "tmux has-session -t $Session 2>/dev/null"
    if ($running -eq 0) {
        Write-Host "A run is already going on the Spark. Use 'status' or 'watch'." -ForegroundColor Yellow
        return
    }
    Push-Pipeline

    $flags = @('--workers', $Workers) + $Extra
    if ($Critic) { $flags += '--critic' }
    $flagText = ($flags | ForEach-Object { "'$_'" }) -join ' '

    Write-Host "Starting generation on the Spark (tmux session '$Session')..." -ForegroundColor Cyan
    $code = Invoke-Remote @"
set -e
cd ~/$Remote
tar -xzf push.tgz && rm push.tgz
if [ ! -x .venv/bin/python ]; then
  echo 'Creating Python venv (first run only)...'
  python3 -m venv .venv
  .venv/bin/pip install -q --upgrade pip
fi
.venv/bin/python -c 'import numpy, PIL' 2>/dev/null || .venv/bin/pip install -q numpy pillow
mkdir -p Tools/AssetPipeline/out
cd Tools/AssetPipeline
tmux new-session -d -s $Session "PYTHONIOENCODING=utf-8 ../../.venv/bin/python -u pipeline.py generate --host http://127.0.0.1:11434 $flagText 2>&1 | tee -a out/run.log"
sleep 3
tail -n 5 out/run.log
"@
    if ($code -ne 0) { throw "Remote start failed ($code)" }
    Write-Host "`nRunning. It keeps going on the Spark even if this PC sleeps." -ForegroundColor Green
    Write-Host "Check in with:  status | watch | pull" -ForegroundColor Green
}

function Show-Status {
    Invoke-Remote @"
cd ~/$Remote/Tools/AssetPipeline 2>/dev/null || { echo 'Nothing has been started on the Spark yet.'; exit 0; }
if tmux has-session -t $Session 2>/dev/null; then echo '== RUNNING =='; else echo '== not running =='; fi
echo; echo 'Latest progress:'; tail -n 6 out/run.log 2>/dev/null
echo; PYTHONIOENCODING=utf-8 ../../.venv/bin/python pipeline.py status
"@ | Out-Null
}

function Get-Results {
    Write-Host "Packing results on the Spark..." -ForegroundColor Cyan
    $code = Invoke-Remote @"
cd ~/$Remote
mkdir -p Assets/Resources/GeneratedItemModels Tools/AssetPipeline/out
tar -czf pull.tgz Assets/Resources/GeneratedItemModels Tools/AssetPipeline/out
"@
    if ($code -ne 0) { throw 'Remote pack failed' }
    $tgz = Join-Path $env:TEMP 'clonez-pull.tgz'
    & scp -q @Ssh "${SparkHost}:~/$Remote/pull.tgz" $tgz
    if ($LASTEXITCODE -ne 0) { throw 'scp failed' }
    # Files changed here since they were last pulled (hand fixes, re-posed models) are newer than the
    # Spark's copies; keep them rather than silently reverting them.
    & tar.exe -xzf $tgz -C $Root --keep-newer-files
    if ($LASTEXITCODE -ne 0) { throw 'tar failed' }
    Remove-Item $tgz
    $n = (Get-ChildItem (Join-Path $Root 'Assets/Resources/GeneratedItemModels') -Filter *.json).Count
    Write-Host "$n generated models are now in Assets/Resources/GeneratedItemModels." -ForegroundColor Green
    Write-Host "Unity picks them up on its next asset refresh (focus the editor)." -ForegroundColor Green
}

function Stop-Run {
    # SIGINT lets the pipeline finish the items it is already working on, then exit cleanly.
    Invoke-Remote "pkill -INT -f 'pipeline.py generate' && echo 'Stopping after in-flight items...' || echo 'Not running.'" | Out-Null
}

function Watch-Run {
    Write-Host "Live log (Ctrl+C to stop watching - the run keeps going)." -ForegroundColor Cyan
    & ssh @Ssh $SparkHost "tail -n 20 -f ~/$Remote/Tools/AssetPipeline/out/run.log"
}

function Open-Gallery {
    $html = Join-Path $Root 'Tools/AssetPipeline/out/index.html'
    if (-not (Test-Path $html)) { Get-Results }
    if (Test-Path $html) { Invoke-Item $html } else { Write-Host 'No previews yet.' -ForegroundColor Yellow }
}

function Show-Menu {
    while ($true) {
        Write-Host ''
        Write-Host '=== CLONE-Z LLM Asset Pipeline (DGX Spark) ===' -ForegroundColor Cyan
        Write-Host ' 1) Start / resume generation'
        Write-Host ' 2) Start / resume with vision critic (slower, better)'
        Write-Host ' 3) Status'
        Write-Host ' 4) Watch live log'
        Write-Host ' 5) Pull finished models into Unity'
        Write-Host ' 6) Open preview gallery'
        Write-Host ' 7) Stop'
        Write-Host ' Q) Quit (the run keeps going on the Spark)'
        switch ((Read-Host 'Choose').Trim().ToUpper()) {
            '1' { Start-Run }
            '2' { $script:Critic = $true; Start-Run; $script:Critic = $false }
            '3' { Show-Status }
            '4' { try { Watch-Run } catch { } }
            '5' { Get-Results }
            '6' { Open-Gallery }
            '7' { Stop-Run }
            'Q' { return }
        }
    }
}

switch ($Action) {
    'start' { Start-Run }
    'status' { Show-Status }
    'pull' { Get-Results }
    'stop' { Stop-Run }
    'watch' { Watch-Run }
    'gallery' { Open-Gallery }
    default { Show-Menu }
}
