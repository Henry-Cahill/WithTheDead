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

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode nightwalk -Shots C:\Temp\nightwalk
    Walks the town's lit rooms by night and prints each stop's frame times and shadow-atlas use; -Shots keeps a
    picture of each stop and the numbers (nightwalk.csv). It renders, so it can't run with -NoGraphics.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode grounditems -Shots C:\Temp\grounditems
    Scatters 3,000 ground items round the survivor in the Kettle and prints the ground-item renderer's CPU time a frame
    standing still, beside what the old per-frame gather cost; -Shots keeps the frame-by-frame numbers
    (grounditems.csv). The renderer needs a graphics device, so it can't run with -NoGraphics either.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode meshcache -Zone bardstown -Shots C:\Temp\meshcache
    Walks across the statewide map, dropping items with generated meshes that nothing has drawn yet at every stop, and
    prints what the generated meshes' cache and the engine hold at each one. It fails unless the memory levels off at
    the cache's budget. -Shots keeps the numbers (meshcache.csv). It draws the items, so it can't run with -NoGraphics.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode horde -NoGraphics
    Raises 200 zombies round the survivor in the procedural town and plays them at 60, 30 and 20 frames a second,
    printing the visibility checks and rays a frame and their CPU time. It fails if any zombie in view goes more than
    0.2 s between checks, or if the Sight Occluder layer hides the dead behind anything other than what the whole world
    did. -Shots keeps the frame-by-frame numbers (horde.csv); -Zone plays it elsewhere.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode cutaway -Shots C:\Temp\cutaway
    In the densest block of Bardstown, indoors and out, by night and at noon, prints what the isometric cutaway hides
    that still casts shadows, and the shadow passes' times as the game has them, with none of that casting shadows, and
    with only the survivor's own building's casting them. Then it walks east across the state and fails if the cutaway
    still remembers colliders of pieces, interiors or windows that were let go of. -Shots keeps a picture of each stop
    each way and the numbers (cutaway.csv, cutaway-cache.csv); -Zone intro profiles the procedural town, where the
    cutaway cuts the houses' walls itself. It renders, so it can't run with -NoGraphics.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode solo -Zone bardstown -Threaded
    The streaming run as the editor renders it: -Threaded gives the batch editor a render thread (-force-gfx-mt) and
    draws the screen's cameras at 1920x1080 every frame, as the Game view would. Any mode fails then if the main thread
    ever had to wait for room in the render thread's command ring, which is what Unity's "Ran out of Graphics Ring
    Buffer space" counts (SmokeTest.Ring.cs). The modes that time their own renders count that extra one in.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode attachments -Shots C:\Temp\attachments
    By night in a raid, kits out a pistol and four long guns in turn and fails unless every attachment builds onto the gun
    in hand and sits on it, a red dot and a suppressor change its outline, the lot hides with the body in first person,
    the weapon light switches with L and brightens where it points, follows the aim, runs flat and takes a battery, and
    the laser's dot lands where the barrel points (on the shot's line once aimed). -Shots keeps pictures of each gun from
    either side, of the scene and through the bodycam. It measures the light in a rendered frame, so it can't run with
    -NoGraphics.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode guns -Shots C:\Temp\guns
    The period guns (ZoneContent: the .38, the .45, the .22, the police pump, the .30-06, the .30-30, the AR-15, the SVD, the
    PKM, the AKS-74U). On paper: each one's rounds, model, hold, report and reload (rendered), price and loot; every kind of
    building that keeps guns (farmhouse cabinet, gun shop counter, police locker) with guns of its own, rolled; every
    gunsmith loyalty level with guns of its own. Then in a raid, each gun in every look it has: muzzle first, the fist on the
    grip and the left hand (IK) on the forend, a sight seated on it, a round, a held trigger (a burst only from the
    automatics), a reload. -Shots keeps a picture of the guns' models and generated looks side by side, and of each in the
    hands. It runs with -NoGraphics too (no pictures).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode rounds -Shots C:\Temp\rounds
    The special rounds (SmokeTest.Rounds.cs). On paper: the tracer and the two incendiaries carry their effect, their cards
    name it, and each is sold and found; no other round has an effect, and the rounds whose cards promised what nothing did
    are gone. Then in a raid, the trigger pulled with the cursor on the target: a plain round draws no trail and a tracer round
    draws its streak; a plain 9mm round sets nobody alight, a 9mm and a 12g incendiary set the zombie they hit alight, and it
    burns and falls, its death the gun's. -Shots keeps pictures of a tracer's streak in flight. It runs with -NoGraphics too
    (no pictures).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode lobby -FloatingProp -Shots C:\Temp\floating
    The lobby and wave runs fail on floating geometry: twice each, on the world as played, they run the
    floating-geometry report (FloatingGeometryReport.cs) and fail on anything it flags that isn't a known false positive
    (SmokeTest.Floating.cs). -FloatingProp hangs a test prop in the air beside the survivor for each of those checks, so
    the run must fail; -Shots keeps the reports and a picture of each kind of thing flagged.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode melee -Shots C:\Temp\melee
    Melee with weight (SmokeTest.Melee.cs). On paper: the knife, the bat and the axe differ in reach, arc, wind-up, recovery,
    stagger and knockdown, and a swing carries through one body (the knife), two (the axe) or three (the bat). Then in a raid,
    in a corridor walled in ahead of the survivor: one swing of each at three of the dead lands its wind-up after the press
    and strikes that many, the first taking the full blow; held, it swings again once the recovery is over; over 16 blows
    each staggers and fells as often as its chances say (the knife never fells); a crawler at the feet is stomped, and a
    swing passes over one on the ground while one of the dead stands in it; a shove puts one of the dead down and the attack
    stomps it, on its body for half while its head is out of reach, then on its head in full, to death before it gets up;
    and each weapon fights its way through three of the dead coming down the
    corridor, its time, swings, staggers, knockdowns, lunges and bites logged side by side. -Shots keeps pictures of the
    opening blows, one of the dead on the ground and a stomp. It runs with -NoGraphics too (no pictures).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Smoke\Smoke.ps1 -Mode throwables
    The molotov and the noise lures against the dead (SmokeTest.Throwables.cs): a molotov and a pipe bomb crafted in the
    field, the pipe bomb thrown; a horde set on the survivor shut in a house, then pulled off it by an alarm clock tossed out
    of the door (off the chase within seconds of the ring, away to the clock, the clock left lying when it rings out);
    firecrackers and the timer radio; and a molotov thrown into a doorway the dead are coming through, which must burn them.
    It runs with -NoGraphics too.
#>
param(
    [ValidateSet('wave', 'solo', 'hunter', 'lobby', 'nightwalk', 'grounditems', 'meshcache', 'horde', 'cutaway', 'attachments', 'guns', 'rounds', 'melee', 'throwables')]
    [string]$Mode = 'wave',
    [string]$Unity,
    [string]$Log,
    [string]$Shots,
    [string]$Zone,
    [switch]$NoGraphics,
    [switch]$Threaded,
    [switch]$FloatingProp
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $Log) { $Log = Join-Path $env:TEMP "clonez-smoke-$Mode.log" }
if ($Mode -eq 'nightwalk' -and $NoGraphics) { throw 'nightwalk renders the game to measure it; leave out -NoGraphics.' }
if ($Mode -eq 'grounditems' -and $NoGraphics) { throw 'grounditems needs a graphics device to draw ground items; leave out -NoGraphics.' }
if ($Mode -eq 'meshcache' -and $NoGraphics) { throw 'meshcache needs a graphics device to draw ground items; leave out -NoGraphics.' }
if ($Mode -eq 'cutaway' -and $NoGraphics) { throw 'cutaway renders the game to measure it; leave out -NoGraphics.' }
if ($Mode -eq 'attachments' -and $NoGraphics) { throw 'attachments measures the weapon light in a rendered frame; leave out -NoGraphics.' }
if ($Threaded -and $NoGraphics) { throw '-Threaded draws the game through the render thread; leave out -NoGraphics.' }
if ($FloatingProp -and $Mode -notin 'lobby', 'wave') { throw '-FloatingProp tests the floating-geometry check, which only the lobby and wave runs make.' }

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
if ($Shots) { $arguments += @('-clonez-smoke-shots', "`"$Shots`"") }
if ($Zone) { $arguments += @('-clonez-smoke-zone', $Zone) }
if ($Threaded) { $arguments += @('-force-gfx-mt', '-clonez-smoke-view', '1920x1080') }
if ($FloatingProp) { $arguments += '-clonez-smoke-floating-prop' }
Remove-Item $Log -ErrorAction SilentlyContinue
Write-Host "Running the $Mode smoke test (log: $Log)..."
$editor = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden

# The summary block at the end of the run lists every check again; print its head and what didn't pass.
$lines = @(if (Test-Path $Log) { Select-String -Path $Log -Pattern '^\[Smoke\] ' | ForEach-Object { $_.Line } })
# The night walk's measurements, a line a stop, the ground items' standing-still numbers, the mesh cache's walk, the
# horde's frame rates, the cutaway's stops and cache walk, the weapon light's measured spot, the melee weapons' profiles,
# blows and fights, and what the floating-geometry checks flagged.
$lines | Where-Object { $_ -match '^\[Smoke\] (nightwalk|grounditems|meshcache|horde) |^\[Smoke\] cutaway[ :]|^\[Smoke\] attachments light |^\[Smoke\] melee (profile|blows|fight) |^\[Smoke\] floating ' } | ForEach-Object { Write-Host $_ }
$start = [Array]::FindIndex([string[]]$lines, [Predicate[string]] { param($l) $l.StartsWith('[Smoke] ----') })
if ($start -lt 0) { Write-Host 'The run ended without a [Smoke] summary; see the log.' -ForegroundColor Red }
else {
    $lines[$start..($lines.Count - 1)] | Where-Object { $_ -match '^\[Smoke\] (----|FAIL |SKIP |error:|RESULT)' } | ForEach-Object {
        $color = if ($_ -match '^\[Smoke\] (FAIL|error:)') { 'Red' } elseif ($_ -match '^\[Smoke\] SKIP') { 'Yellow' } else { 'Cyan' }
        Write-Host $_ -ForegroundColor $color
    }
}
exit $editor.ExitCode
