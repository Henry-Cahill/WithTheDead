$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../NightShift.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw ($errors -join "`n") }
foreach ($name in @('Invoke-Tool', 'Get-PublishCommit', 'Test-PublishDue')) {
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
    Write-Host 'All publication regression tests passed.'
}
finally {
    Remove-Item -Recurse -Force $Root
}
