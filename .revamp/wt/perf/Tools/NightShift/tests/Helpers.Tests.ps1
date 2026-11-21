$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../NightShift.ps1'
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw ($errors -join "`n") }
$definition = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Test-DrivesWindowsHelpers'
}, $true)
. ([scriptblock]::Create($definition.Extent.Text))

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message (expected '$Expected', got '$Actual')" }
}

$current = Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot '../nightshift.py')
Assert-Equal $true (Test-DrivesWindowsHelpers $current) 'This nightshift.py drives Windows helpers'
# The head of a nightshift.py from before Windows helpers, whose worker fails on Windows at "import fcntl".
$older = @('import subprocess', 'import fcntl', 'HERE = Path(__file__).resolve().parent', '        windows = None')
Assert-Equal $false (Test-DrivesWindowsHelpers $older) 'A nightshift.py from before Windows helpers'
Assert-Equal $false (Test-DrivesWindowsHelpers @()) 'No nightshift.py on the Spark'
Assert-Equal $false (Test-DrivesWindowsHelpers $null) 'No answer from the Spark'
Write-Host 'All helper tests passed.'
