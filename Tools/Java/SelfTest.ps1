<#
.SYNOPSIS
    Checks the Java host inside a batch-mode Unity editor (CloneZ.EditorTools.JavaSelfTest, Assets/Editor/JavaSelfTest.cs).

.DESCRIPTION
    Builds the Java scripts, runs a Java session in the editor's own process with the self-test's scripts
    (Java/withthedead.selftest), then reloads the scripts' domain, as every script change does, and runs another on the
    JVM that outlived it. Uses the Unity Hub install of the project's editor version unless -Unity is given; the project
    must not be open in another editor (CloneZ > Java > Self Test runs a round in an open one, and -Project can name a
    copy of the project to run in instead). Needs the JDK (Tools/Java/Install-Jdk.ps1). Prints the [JavaSelfTest] and
    [Java] lines and exits 0 when everything passed.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Java\SelfTest.ps1
#>
param(
    [string]$Project,
    [string]$Unity,
    [string]$Log,
    [int]$TimeoutMinutes = 20
)

$ErrorActionPreference = 'Stop'
$project = if ($Project) { (Resolve-Path $Project).Path } else { (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
if (-not $Log) { $Log = Join-Path $env:TEMP 'clonez-java-selftest.log' }

if (-not $Unity) {
    $versionLine = Select-String -Path (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion:\s*(\S+)'
    $Unity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$($versionLine.Matches[0].Groups[1].Value)\Editor\Unity.exe"
}
if (-not (Test-Path $Unity)) { throw "No Unity editor at $Unity; pass -Unity <path to Unity.exe>." }

# An open editor holds Temp\UnityLockfile exclusively; a leftover file from a closed one doesn't count.
$lock = Join-Path $project 'Temp\UnityLockfile'
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Dispose() }
    catch { throw 'The project is open in a Unity editor; close it first, or use CloneZ > Java > Self Test in it.' }
}

Remove-Item $Log -ErrorAction SilentlyContinue
$arguments = @('-batchmode', '-nographics', '-projectPath', "`"$project`"",
    '-executeMethod', 'CloneZ.EditorTools.JavaSelfTest.Run', '-logFile', "`"$Log`"")
Write-Host "Running the Java self-test (log: $Log)..."
# Waits for the editor alone: -Wait would wait for the licensing client it leaves running too, which can take forever.
$editor = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
$null = $editor.Handle
if (-not $editor.WaitForExit($TimeoutMinutes * 60000)) {
    Stop-Process -Id $editor.Id -Force
    Write-Host "The editor didn't finish within $TimeoutMinutes minutes; see $Log." -ForegroundColor Red
    exit 1
}

Select-String -Path $Log -Pattern '^\[(JavaSelfTest|Java)\] ' -ErrorAction SilentlyContinue | ForEach-Object {
    $color = if ($_.Line -match '^\[JavaSelfTest\] (FAIL|RESULT FAIL)') { 'Red' } elseif ($_.Line -match 'RESULT PASS') { 'Green' } else { 'Gray' }
    Write-Host $_.Line -ForegroundColor $color
}
exit $editor.ExitCode
