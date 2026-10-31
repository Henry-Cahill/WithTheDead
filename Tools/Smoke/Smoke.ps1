<#
.SYNOPSIS
    Runs one of SmokeTest's modes in a batch-mode Unity editor and prints its results.

.DESCRIPTION
    Plays the game headless with -clonez-smoke <mode> (see Assets/Scripts/Dev/SmokeTest.cs and MECHANICS_COVERAGE.md,
    "How it's checked"). Uses the Unity Hub install of the project's editor version unless -Unity is given. The project
    must not be open in another editor. The full log is kept at -Log; the [Smoke] summary, failures and skips are
    printed. Exits with the editor's code: 0 when everything passed, 2 when a check failed or the game logged an error.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1
    Plays Wave Survival through every key, mouse action, fixture action and button (the default mode, wave).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode solo -NoGraphics
#>
param(
    [ValidateSet('wave', 'solo', 'hunter', 'lobby')]
    [string]$Mode = 'wave',
    [string]$Unity,
    [string]$Log,
    [switch]$NoGraphics
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $Log) { $Log = Join-Path $env:TEMP "clonez-smoke-$Mode.log" }

if (-not $Unity) {
    $versionLine = Select-String -Path (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion:\s*(\S+)'
    $Unity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$($versionLine.Matches[0].Groups[1].Value)\Editor\Unity.exe"
}
if (-not (Test-Path $Unity)) { throw "No Unity editor at $Unity; pass -Unity <path to Unity.exe>." }

# An open editor holds Temp\UnityLockfile exclusively; a leftover file from a closed one doesn't count.
$lock = Join-Path $project 'Temp\UnityLockfile'
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Dispose() }
    catch { throw 'The project is open in a Unity editor; close it first (or run against a copy of the project).' }
}

$arguments = @('-batchmode', '-projectPath', "`"$project`"", '-executeMethod', 'CloneZ.EditorTools.CloneZBatch.SmokePlay',
    '-clonez-smoke', $Mode, '-logFile', "`"$Log`"")
if ($NoGraphics) { $arguments = @('-nographics') + $arguments }
Remove-Item $Log -ErrorAction SilentlyContinue
Write-Host "Running the $Mode smoke test (log: $Log)..."
$editor = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden

# The summary block at the end of the run lists every check again; print its head and what didn't pass.
$lines = @(if (Test-Path $Log) { Select-String -Path $Log -Pattern '^\[Smoke\] ' | ForEach-Object { $_.Line } })
$start = [Array]::FindIndex([string[]]$lines, [Predicate[string]] { param($l) $l.StartsWith('[Smoke] ----') })
if ($start -lt 0) { Write-Host 'The run ended without a [Smoke] summary; see the log.' -ForegroundColor Red }
else {
    $lines[$start..($lines.Count - 1)] | Where-Object { $_ -match '^\[Smoke\] (----|FAIL |SKIP |error:|RESULT)' } | ForEach-Object {
        $color = if ($_ -match '^\[Smoke\] (FAIL|error:)') { 'Red' } elseif ($_ -match '^\[Smoke\] SKIP') { 'Yellow' } else { 'Cyan' }
        Write-Host $_ -ForegroundColor $color
    }
}
exit $editor.ExitCode
