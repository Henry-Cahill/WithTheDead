$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../NightShift.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw ($errors -join "`n") }
foreach ($name in @('Invoke-Tool', 'Get-PublishCommit', 'Test-PublishDue', 'Test-MergedPullRequest', 'Get-SparkMainScript')) {
    $definition = $ast.Find({ param($node)
        $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name
    }, $true)
    . ([scriptblock]::Create($definition.Extent.Text))
}

function Invoke-TestGit {
    $output = & git -C $Root @args 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git $args failed: $output" }
    return ($output -join "`n")
}

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message (expected '$Expected', got '$Actual')" }
}

function New-TestCommit([string]$File, [string]$Text) {
    [IO.File]::WriteAllText((Join-Path $Root $File), $Text)
    Invoke-TestGit add -- $File | Out-Null
    Invoke-TestGit commit -qm $Text | Out-Null
    return Invoke-TestGit rev-parse HEAD
}

$Root = Join-Path ([IO.Path]::GetTempPath()) ("clonez-publish-test-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $Root | Out-Null
try {
    Invoke-TestGit init -q | Out-Null
    Invoke-TestGit config user.name 'Publication test' | Out-Null
    Invoke-TestGit config user.email 'test@localhost' | Out-Null
    Invoke-TestGit config commit.gpgSign false | Out-Null
    # A local origin exercises real fetch/merge operations without network access or pushes.
    Invoke-TestGit remote add origin $Root | Out-Null
    $base = New-TestCommit 'shared.txt' 'base'
    $spark = New-TestCommit 'spark.txt' 'spark'
    $branch = 'nightshift/test'
    Assert-Equal $spark (Get-PublishCommit $branch $spark '') 'New branch'
    Assert-Equal $spark (Get-PublishCommit $branch $spark $spark) 'Already published'

    Invoke-TestGit update-ref "refs/heads/$branch" $base | Out-Null
    Assert-Equal $spark (Get-PublishCommit $branch $spark $base) 'GitHub behind'
    $ahead = New-TestCommit 'remote.txt' 'ahead'
    Invoke-TestGit update-ref "refs/heads/$branch" $ahead | Out-Null
    Assert-Equal $ahead (Get-PublishCommit $branch $spark $ahead) 'GitHub ahead'

    Invoke-TestGit checkout -q --detach $base | Out-Null
    $remote = New-TestCommit 'remote.txt' 'remote'
    Invoke-TestGit update-ref "refs/heads/$branch" $remote | Out-Null
    Invoke-TestGit config merge.ff only | Out-Null
    [IO.File]::WriteAllText((Join-Path $Root 'shared.txt'), 'uncommitted')
    $merged = Get-PublishCommit $branch $spark $remote
    Assert-Equal "$spark $remote" (Invoke-TestGit show -s --format=%P $merged) 'Merge preserves both parents'
    Assert-Equal 'spark' (Invoke-TestGit show "${merged}:spark.txt") 'Spark content preserved'
    Assert-Equal 'remote' (Invoke-TestGit show "${merged}:remote.txt") 'GitHub content preserved'
    Assert-Equal $remote (Invoke-TestGit rev-parse HEAD) 'Checkout HEAD unchanged'
    Assert-Equal 'uncommitted' ([IO.File]::ReadAllText((Join-Path $Root 'shared.txt'))) 'Local edits unchanged'
    Assert-Equal 1 (@(Invoke-TestGit worktree list --porcelain | Select-String '^worktree ').Count) 'Temporary worktree removed'
    Invoke-TestGit update-ref "refs/heads/$branch" $merged | Out-Null
    Assert-Equal $merged (Get-PublishCommit $branch $spark $merged) 'Repeated publication reuses merged commit'

    Invoke-TestGit checkout -q -- shared.txt | Out-Null
    Invoke-TestGit checkout -q --detach $spark | Out-Null
    $nextSpark = New-TestCommit 'later.txt' 'later task'
    $nextMerged = Get-PublishCommit $branch $nextSpark $merged
    Assert-Equal "$nextSpark $merged" (Invoke-TestGit show -s --format=%P $nextMerged) 'Later tasks preserve published history'
    Assert-Equal 'later task' (Invoke-TestGit show "${nextMerged}:later.txt") 'Later Spark task preserved'
    Assert-Equal 'remote' (Invoke-TestGit show "${nextMerged}:remote.txt") 'Earlier GitHub edit preserved'

    Invoke-TestGit checkout -q --detach $base | Out-Null
    $left = New-TestCommit 'shared.txt' 'left'
    Invoke-TestGit checkout -q --detach $base | Out-Null
    $right = New-TestCommit 'shared.txt' 'right'
    Invoke-TestGit update-ref "refs/heads/$branch" $right | Out-Null
    $failure = $null
    try { Get-PublishCommit $branch $left $right | Out-Null }
    catch { $failure = $_.Exception.Message }
    if (-not $failure -or $failure -notlike '*nothing was pushed*retry P*') { throw "Missing conflict guidance: $failure" }
    Assert-Equal $right (Invoke-TestGit rev-parse HEAD) 'Conflict leaves checkout unchanged'
    Assert-Equal $right (Invoke-TestGit rev-parse "refs/heads/$branch") 'Conflict leaves branch unchanged'
    Assert-Equal 1 (@(Invoke-TestGit worktree list --porcelain | Select-String '^worktree ').Count) 'Conflicted worktree removed'

    # pr -WhenDone: publish once the agents run out of work, or every publish_every minutes while the planner keeps
    # the queue full.
    $stamp = Join-Path $Root 'last-publish.txt'
    $now = Get-Date
    $busy = [pscustomobject]@{ running = $true; working = 2; queued = 5; dispatchable = 5; idle_since = $null }
    Assert-Equal $true (Test-PublishDue ([pscustomobject]@{ running = $false; working = 0; queued = 3 }) $stamp $now) 'Stopped: publish'
    Assert-Equal $true (Test-PublishDue ([pscustomobject]@{ running = $true; working = 0; queued = 1; dispatchable = 0 }) $stamp $now) 'Nothing left: publish'
    Assert-Equal $true (Test-PublishDue ([pscustomobject]@{ running = $true; working = 0; queued = 0; idle_since = '20:56' }) $stamp $now) 'Old coordinator idle: publish'
    Assert-Equal $false (Test-PublishDue $busy $stamp $now) 'Busy without the planner: wait'
    $planning = [pscustomobject]@{ running = $true; working = 4; queued = 6; dispatchable = 6; autoplan = $true; publish_every = 120 }
    Assert-Equal $true (Test-PublishDue $planning $stamp $now) 'Planner on, never published: publish'
    Set-Content -Path $stamp -Value $now.AddMinutes(-30).Ticks -Encoding ASCII
    Assert-Equal $false (Test-PublishDue $planning $stamp $now) 'Planner on, published 30 min ago: wait'
    Set-Content -Path $stamp -Value $now.AddMinutes(-121).Ticks -Encoding ASCII
    Assert-Equal $true (Test-PublishDue $planning $stamp $now) 'Planner on, published 121 min ago: publish'
    $planning.publish_every = 0
    Assert-Equal $false (Test-PublishDue $planning $stamp $now) 'Planner on, periodic publishing off: wait'
    Set-Content -Path $stamp -Value 'garbage' -Encoding ASCII
    $planning.publish_every = 120
    Assert-Equal $true (Test-PublishDue $planning $stamp $now) 'Unreadable stamp counts as never published'

    # Squash and rebase merges leave the branch's commits off main, so the merged pull request's head must hold them.
    Assert-Equal $true (Test-MergedPullRequest $spark @([pscustomobject]@{ number = 1; headRefOid = $spark })) 'Merged head is the commit'
    Assert-Equal $true (Test-MergedPullRequest $spark @([pscustomobject]@{ number = 2; headRefOid = $merged })) 'Merged head holds the commit'
    Assert-Equal $false (Test-MergedPullRequest $nextSpark @([pscustomobject]@{ number = 2; headRefOid = $merged })) 'Work after the merged head'
    Assert-Equal $false (Test-MergedPullRequest $spark @()) 'No merged pull request'
    Assert-Equal $false (Test-MergedPullRequest $spark @([pscustomobject]@{ number = 3; headRefOid = ('0' * 39) + '1' })) 'Unfetchable head'
}
finally {
    Remove-Item -Recurse -Force $Root
}

# The Spark's side of moving back to main, run with Git Bash (or bash elsewhere) against throwaway repositories: a bare
# one standing in for GitHub, "pc" for merges made on GitHub, and clones standing in for the Spark's project.
$bash = if ($env:OS -eq 'Windows_NT') {
    @('..\bin\bash.exe', '..\..\bin\bash.exe') | ForEach-Object { Join-Path (Split-Path (Get-Command git).Source) $_ } |
        Where-Object { Test-Path $_ } | Select-Object -First 1
} else { (Get-Command bash -ErrorAction SilentlyContinue).Source }
if (-not $bash) { throw 'No bash to run the Spark main tests with.' }

function Invoke-RepoGit {
    $dir, $rest = $args
    $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $output = & git -C $dir @rest 2>&1 }
    finally { $ErrorActionPreference = $eap }
    if ($LASTEXITCODE -ne 0) { throw "git $rest failed in ${dir}: $output" }
    return ($output -join "`n")
}

function New-RepoCommit([string]$Dir, [string]$File, [string]$Text) {
    [IO.File]::WriteAllText((Join-Path $Dir $File), $Text)
    Invoke-RepoGit $Dir add -- $File | Out-Null
    Invoke-RepoGit $Dir commit -qm $File | Out-Null
    return Invoke-RepoGit $Dir rev-parse HEAD
}

function Set-RepoConfig([string]$Dir) {
    foreach ($pair in @(@('user.name', 'Spark main test'), @('user.email', 'test@localhost'), @('commit.gpgSign', 'false'), @('core.autocrlf', 'false'))) {
        Invoke-RepoGit $Dir config $pair[0] $pair[1] | Out-Null
    }
}

function Invoke-SparkMain([string]$Dir, [string]$Branch, [string]$Spark, [string]$Running = 'false') {
    $file = Join-Path $Work 'sync.sh'
    $text = (Get-SparkMainScript ($Dir -replace '\\', '/') $Branch $Spark $Running) -replace "`r", ''
    [IO.File]::WriteAllText($file, $text, (New-Object Text.UTF8Encoding $false))
    return "$(@(& $bash $file)[-1])".Trim()
}

function Assert-Answer([string]$Pattern, [string]$Answer, [string]$Message) {
    if ($Answer -notlike $Pattern) { throw "$Message (answer: '$Answer')" }
}

function Assert-Queued([string]$Dir, [string]$Message) {
    if (-not [IO.File]::ReadAllText((Join-Path $Dir 'tasks.md')).Contains('- [ ] queued')) { throw "$Message (the queued task is gone)" }
}

$Work = Join-Path ([IO.Path]::GetTempPath()) ("clonez-main-test-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $Work | Out-Null
try {
    $github = Join-Path $Work 'github.git'
    $pc = Join-Path $Work 'pc'
    $sparkDir = Join-Path $Work 'spark'
    Invoke-RepoGit $Work init -q --bare --initial-branch=main $github | Out-Null
    Invoke-RepoGit $Work init -q --initial-branch=main $pc | Out-Null
    Set-RepoConfig $pc
    New-RepoCommit $pc 'tasks.md' "- [ ] one`n" | Out-Null
    Invoke-RepoGit $pc remote add origin $github | Out-Null
    Invoke-RepoGit $pc push -q origin main | Out-Null
    Invoke-RepoGit $Work clone -q $github $sparkDir | Out-Null
    Set-RepoConfig $sparkDir

    # A night's branch, published and merged on GitHub with a merge commit, plus an edit on GitHub after the merge.
    Invoke-RepoGit $sparkDir switch -q -c nightshift/t | Out-Null
    New-RepoCommit $sparkDir 'task.txt' 'task' | Out-Null
    $night = New-RepoCommit $sparkDir 'tasks.md' "- [x] one`n"
    Invoke-RepoGit $sparkDir push -q origin nightshift/t | Out-Null
    Invoke-RepoGit $pc fetch -q origin | Out-Null
    Invoke-RepoGit $pc merge -q --no-ff -m 'Merge pull request' origin/nightshift/t | Out-Null
    New-RepoCommit $pc 'fix.txt' 'fix on GitHub' | Out-Null
    Invoke-RepoGit $pc push -q origin main | Out-Null
    [IO.File]::WriteAllText((Join-Path $sparkDir 'tasks.md'), "- [x] one`n- [ ] queued`n")  # queued with "add", uncommitted

    Assert-Answer 'skip The night shift is running*' (Invoke-SparkMain $sparkDir 'nightshift/t' $night 'true') 'Running: leave it'
    Assert-Answer 'skip New work*' (Invoke-SparkMain $sparkDir 'nightshift/t' (Invoke-RepoGit $sparkDir rev-parse HEAD~1)) 'Moved past the checked commit: leave it'
    Assert-Answer "skip The Spark's project is on 'nightshift/t' now*" (Invoke-SparkMain $sparkDir 'nightshift/other' $night) 'Another branch: leave it'
    Assert-Equal 'nightshift/t' (Invoke-RepoGit $sparkDir rev-parse --abbrev-ref HEAD) 'Skips stay on the branch'

    Assert-Answer 'done Moved*nightshift/t*' (Invoke-SparkMain $sparkDir 'nightshift/t' $night) 'Merged branch: move to main'
    Assert-Equal 'main' (Invoke-RepoGit $sparkDir rev-parse --abbrev-ref HEAD) 'On main after the move'
    Assert-Equal (Invoke-RepoGit $pc rev-parse HEAD) (Invoke-RepoGit $sparkDir rev-parse HEAD) "On GitHub's latest main"
    Assert-Equal 'origin/main' (Invoke-RepoGit $sparkDir rev-parse --abbrev-ref '@{upstream}') 'Main still tracks GitHub'
    Assert-Queued $sparkDir 'Move'
    Assert-Answer 'same *' (Invoke-SparkMain $sparkDir 'main' '') 'Already on the latest main'

    New-RepoCommit $pc 'more.txt' 'more' | Out-Null
    Invoke-RepoGit $pc push -q origin main | Out-Null
    Assert-Answer 'done Brought*' (Invoke-SparkMain $sparkDir 'main' '') 'Main behind GitHub: fast-forward'
    Assert-Equal (Invoke-RepoGit $pc rev-parse HEAD) (Invoke-RepoGit $sparkDir rev-parse HEAD) 'Main caught up'
    Assert-Queued $sparkDir 'Fast-forward'

    $before = Invoke-RepoGit $sparkDir rev-parse HEAD
    New-RepoCommit $pc 'tasks.md' "- [x] one`n- [ ] from GitHub`n" | Out-Null
    Invoke-RepoGit $pc push -q origin main | Out-Null
    Assert-Answer "fail Couldn't bring the Spark's main up to date*" (Invoke-SparkMain $sparkDir 'main' '') 'Local changes in the way: refuse'
    Assert-Equal $before (Invoke-RepoGit $sparkDir rev-parse HEAD) 'Refused fast-forward leaves main alone'
    Assert-Queued $sparkDir 'Refused fast-forward'

    New-RepoCommit $sparkDir 'local.txt' 'local' | Out-Null
    $before = Invoke-RepoGit $sparkDir rev-parse HEAD
    Assert-Answer "fail The Spark's main has commits*" (Invoke-SparkMain $sparkDir 'main' '') 'Diverged main: refuse'
    Assert-Equal $before (Invoke-RepoGit $sparkDir rev-parse HEAD) 'Diverged main left alone'

    # Merged, but GitHub's main changed tasks.md since, so switching would overwrite the queued task: git refuses.
    $spark2 = Join-Path $Work 'spark2'
    Invoke-RepoGit $Work clone -q $github $spark2 | Out-Null
    Set-RepoConfig $spark2
    Invoke-RepoGit $spark2 switch -q -c nightshift/u | Out-Null
    $night2 = New-RepoCommit $spark2 'task2.txt' 'task 2'
    Invoke-RepoGit $spark2 push -q origin nightshift/u | Out-Null
    Invoke-RepoGit $pc fetch -q origin | Out-Null
    Invoke-RepoGit $pc merge -q --no-ff -m 'Merge pull request' origin/nightshift/u | Out-Null
    New-RepoCommit $pc 'tasks.md' "- [x] one`n- [x] from GitHub`n" | Out-Null
    Invoke-RepoGit $pc push -q origin main | Out-Null
    [IO.File]::WriteAllText((Join-Path $spark2 'tasks.md'), "- [x] one`n- [ ] from GitHub`n- [ ] queued`n")
    Assert-Answer "fail Couldn't move the Spark's project from nightshift/u to main*" (Invoke-SparkMain $spark2 'nightshift/u' $night2) 'Switch would overwrite: refuse'
    Assert-Equal 'nightshift/u' (Invoke-RepoGit $spark2 rev-parse --abbrev-ref HEAD) 'Refused switch stays on the branch'
    Assert-Queued $spark2 'Refused switch'
    Write-Host 'All publication regression tests passed.'
}
finally {
    Remove-Item -Recurse -Force $Work
}
