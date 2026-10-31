<#
.SYNOPSIS
  One-click control for the CLONE-Z LLM asset pipeline running on the DGX Spark.

.DESCRIPTION
  The generation runs ON THE SPARK inside a tmux session, so it keeps going when this PC sleeps or
  VS Code closes. This script pushes the pipeline there, starts/stops it, shows progress, and pulls
  the finished models back into Assets/Resources/GeneratedItemModels.

  The mesh stage (mesh/README.md) turns items into textured meshes with TRELLIS.2 on the Spark's GPU and
  pulls them into Assets/Resources/GeneratedItemMeshes; its actions start with mesh-.

  Run with no arguments for a menu, or:  .\AssetPipeline.ps1 start|status|pull|stop|watch|gallery
  or for meshes:  .\AssetPipeline.ps1 mesh-build|mesh-prepare|mesh-pilot|mesh-start|mesh-view|mesh-status|mesh-watch|mesh-pull|mesh-gallery|mesh-stop|mesh-refit|mesh-view-stop

.EXAMPLE
  .\AssetPipeline.ps1 start -Critic
  .\AssetPipeline.ps1 start -Extra '--category','Clothing'
  .\AssetPipeline.ps1 mesh-start -Extra '--category','Firearm'
#>
param(
    [ValidateSet('menu', 'start', 'status', 'pull', 'stop', 'watch', 'gallery',
        'mesh-build', 'mesh-prepare', 'mesh-pilot', 'mesh-start', 'mesh-view', 'mesh-status', 'mesh-watch',
        'mesh-pull', 'mesh-gallery', 'mesh-stop', 'mesh-refit', 'mesh-view-stop')]
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
$SparkIp = ($SparkHost -split '@')[-1]
$MeshPort = 8765                     # where mesh/spark.sh serves the live view (its PORT)

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
        'Tools/AssetPipeline/llm.py', 'Tools/AssetPipeline/prompts', 'Tools/AssetPipeline/mesh',
        'Assets/Resources/curated_items.txt', 'Assets/Resources/pz_items.txt',
        'Assets/Scripts/World/ItemModels.cs'
    )
    # Models already in the project are sent too, so the Spark resumes instead of redoing them.
    if (Test-Path (Join-Path $Root 'Assets/Resources/GeneratedItemModels')) { $paths += 'Assets/Resources/GeneratedItemModels' }
    # The hand-built models exported from Unity (CloneZ > Asset Pipeline), which the mesh stage fits meshes onto.
    if (Test-Path (Join-Path $Root 'Tools/AssetPipeline/out/handbuilt')) { $paths += 'Tools/AssetPipeline/out/handbuilt' }
    & tar.exe -czf $tgz -C $Root --exclude '*.meta' --exclude '__pycache__' @paths
    if ($LASTEXITCODE -ne 0) { throw 'tar failed' }

    Write-Host "Uploading to $SparkHost..." -ForegroundColor Cyan
    & ssh @Ssh $SparkHost "mkdir -p ~/$Remote"
    & scp -q @Ssh $tgz "${SparkHost}:~/$Remote/push.tgz"
    if ($LASTEXITCODE -ne 0) { throw 'scp failed' }
    Remove-Item $tgz
}

function Start-Run {
    $running = Invoke-Remote "tmux has-session -t =$Session 2>/dev/null"
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
if tmux has-session -t =$Session 2>/dev/null; then echo '== RUNNING =='; else echo '== not running =='; fi
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

# --- Mesh stage (TRELLIS.2 on the Spark's GPU; see mesh/README.md) ---

function Sync-Pipeline {
    Push-Pipeline
    # Bash chokes on CRLF; a script saved by a Windows editor would otherwise break every remote command.
    $code = Invoke-Remote "cd ~/$Remote && tar -xzf push.tgz && rm push.tgz && sed -i 's/\r`$//' Tools/AssetPipeline/mesh/*.sh && chmod +x Tools/AssetPipeline/mesh/spark.sh"
    if ($code -ne 0) { throw "Remote unpack failed ($code)" }
}

function Invoke-Mesh([string]$Command) {
    return Invoke-Remote "bash ~/$Remote/Tools/AssetPipeline/mesh/spark.sh $Command"
}

function Start-Meshes([string[]]$Flags, [switch]$Interactive) {
    Sync-Pipeline
    $flagText = (@($Flags) + $Extra | ForEach-Object { "'$_'" }) -join ' '
    $code = Invoke-Mesh "start $flagText"
    # 4: the night shift is running, and the mesh run needs the Spark's memory to itself.
    if ($code -eq 4 -and $Interactive) {
        if ((Read-Host 'Stop the night shift now so the meshes can run? (y/N)').Trim().ToLower() -eq 'y') {
            & (Join-Path $Root 'Tools\NightShift\NightShift.ps1') stop
            $code = Invoke-Mesh "start $flagText"
        }
    }
    if ($code -eq 0) {
        Write-Host "Watch it live: http://${SparkIp}:$MeshPort/live.html  (or V in the menu)" -ForegroundColor Green
        Write-Host "Check in with:  mesh-view | mesh-status | mesh-watch | mesh-pull" -ForegroundColor Green
    }
}

# The live view in the browser: the item being made, its reference picture, the step it is on, the latest meshes.
# It is served from the Spark (spark.sh web), so it shows the run as it happens with nothing to pull.
function Open-MeshLive {
    Sync-Pipeline
    $code = Invoke-Mesh 'web'
    if ($code -ne 0) { throw "The live view didn't start on the Spark ($code)" }
    Start-Process "http://${SparkIp}:$MeshPort/live.html"
}

function Get-MeshResults {
    Write-Host "Packing meshes on the Spark..." -ForegroundColor Cyan
    # Reference pictures (out/mesh/refs, ~150 KB each) stay on the Spark; the gallery tiles already show them. Files the
    # run is still writing have temporary names (it renames each once complete), so they stay behind too.
    $code = Invoke-Remote @"
cd ~/$Remote
mkdir -p Assets/Resources/GeneratedItemMeshes Tools/AssetPipeline/out/mesh
tar -czf mesh-pull.tgz --exclude '*.tmp' Assets/Resources/GeneratedItemMeshes --exclude 'Tools/AssetPipeline/out/mesh/refs' Tools/AssetPipeline/out/mesh
"@
    if ($code -ne 0) { throw 'Remote pack failed' }
    $tgz = Join-Path $env:TEMP 'clonez-mesh-pull.tgz'
    & scp -q @Ssh "${SparkHost}:~/$Remote/mesh-pull.tgz" $tgz
    if ($LASTEXITCODE -ne 0) { throw 'scp failed' }
    & tar.exe -xzf $tgz -C $Root --keep-newer-files
    if ($LASTEXITCODE -ne 0) { throw 'tar failed' }
    Remove-Item $tgz
    $n = (Get-ChildItem (Join-Path $Root 'Assets/Resources/GeneratedItemMeshes') -Filter *.bytes).Count
    Write-Host "$n item meshes are now in Assets/Resources/GeneratedItemMeshes." -ForegroundColor Green
    Write-Host "Unity picks them up on its next asset refresh (focus the editor)." -ForegroundColor Green
}

function Open-MeshGallery {
    $html = Join-Path $Root 'Tools/AssetPipeline/out/mesh/index.html'
    if (-not (Test-Path $html)) { Get-MeshResults }
    if (Test-Path $html) { Invoke-Item $html } else { Write-Host 'No meshes yet.' -ForegroundColor Yellow }
}

function Show-MeshMenu {
    while ($true) {
        Write-Host ''
        Write-Host '=== Item meshes (TRELLIS.2 on the Spark) ===' -ForegroundColor Cyan
        Write-Host ' 1) Build the container (once, about an hour)'
        Write-Host ' 2) Download the model weights (once)'
        Write-Host ' 3) Pilot: 24 items across the categories'
        Write-Host ' 4) Start / resume every item'
        Write-Host ' V) Watch it live in the browser'
        Write-Host ' 5) Status'
        Write-Host ' 6) Watch the raw log'
        Write-Host ' 7) Pull finished meshes into Unity'
        Write-Host ' 8) Open the gallery'
        Write-Host ' 9) Stop after the item in progress'
        Write-Host ' R) Refit finished meshes (after a fitting change; generates nothing)'
        Write-Host ' B) Back'
        switch ((Read-Host 'Choose').Trim().ToUpper()) {
            '1' { Sync-Pipeline; Invoke-Mesh 'build' | Out-Null }
            '2' { Sync-Pipeline; Invoke-Mesh 'prepare' | Out-Null }
            '3' { Start-Meshes @('--pilot') -Interactive }
            '4' { Start-Meshes @() -Interactive }
            'V' { Open-MeshLive }
            '5' { Invoke-Mesh 'status' | Out-Null }
            '6' { try { & ssh @Ssh $SparkHost "tail -n 20 -f ~/$Remote/Tools/AssetPipeline/out/mesh/run.log" } catch { } }
            '7' { Get-MeshResults }
            '8' { Open-MeshGallery }
            '9' { Invoke-Mesh 'stop' | Out-Null }
            'R' { Sync-Pipeline; Invoke-Mesh 'refit' | Out-Null }
            'B' { return }
        }
    }
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
        Write-Host ' M) Item meshes (TRELLIS.2): build, pilot, start, pull...'
        Write-Host ' Q) Quit (the run keeps going on the Spark)'
        switch ((Read-Host 'Choose').Trim().ToUpper()) {
            '1' { Start-Run }
            '2' { $script:Critic = $true; Start-Run; $script:Critic = $false }
            '3' { Show-Status }
            '4' { try { Watch-Run } catch { } }
            '5' { Get-Results }
            '6' { Open-Gallery }
            '7' { Stop-Run }
            'M' { Show-MeshMenu }
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
    'mesh-build' { Sync-Pipeline; exit (Invoke-Mesh 'build') }
    'mesh-prepare' { Sync-Pipeline; exit (Invoke-Mesh 'prepare') }
    'mesh-pilot' { Start-Meshes @('--pilot') }
    'mesh-start' { Start-Meshes @() }
    'mesh-view' { Open-MeshLive }
    'mesh-view-stop' { exit (Invoke-Mesh 'web-stop') }
    'mesh-status' { exit (Invoke-Mesh 'status') }
    'mesh-watch' { & ssh @Ssh $SparkHost "tail -n 20 -f ~/$Remote/Tools/AssetPipeline/out/mesh/run.log" }
    'mesh-pull' { Get-MeshResults }
    'mesh-gallery' { Open-MeshGallery }
    'mesh-stop' { exit (Invoke-Mesh 'stop') }
    'mesh-refit' { Sync-Pipeline; exit (Invoke-Mesh ("refit " + (($Extra | ForEach-Object { "'$_'" }) -join ' '))) }
    default { Show-Menu }
}
