<#
.SYNOPSIS
    Installs the JDK 27 pinned in Java/jdk.json: it compiles the game's Java scripts, links the Java runtime that Windows
    builds ship, and runs the scripts in the editor.

.DESCRIPTION
    Downloads Eclipse Temurin for Windows (GPLv2 with the Classpath Exception, so builds may carry a runtime linked from
    it), checks the archive's SHA-256 against Java/jdk.json and unpacks it into
    %LOCALAPPDATA%\WithTheDead\Java\<release>\windows, where the editor (Assets/Editor/JavaBuild.cs) and
    Tools/Server/Release.ps1 look for it. One already there is left alone.
    Temurin 27 ships no jmods: jlink links from the JDK's own run-time image (JEP 493), which only makes runtimes for the
    platform it runs on. The Linux dedicated server's runtime is therefore linked on Linux, by its Docker image
    (Tools/Server/docker/Dockerfile), from the Linux archive pinned beside this one.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Java\Install-Jdk.ps1
#>
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$pin = Get-Content (Join-Path $project 'Java\jdk.json') -Raw | ConvertFrom-Json
$root = Join-Path $env:LOCALAPPDATA "WithTheDead\Java\$($pin.release)"
$dest = Join-Path $root 'windows'
$java = Join-Path $dest 'bin\java.exe'

if (Test-Path (Join-Path $dest 'release')) {
    Write-Host "$($pin.release) is already installed at $dest."
}
else {
    New-Item -ItemType Directory -Force $root | Out-Null
    $archive = Join-Path $root ([IO.Path]::GetFileName(([Uri]$pin.windows.url).AbsolutePath))
    # A download an earlier, interrupted install left behind is used if it's whole.
    if (-not (Test-Path $archive) -or (Get-FileHash $archive -Algorithm SHA256).Hash -ne $pin.windows.sha256) {
        Write-Host "Downloading $($pin.release)..."
        & curl.exe --fail --location --silent --show-error --retry 3 --output $archive $pin.windows.url
        if ($LASTEXITCODE -ne 0) { throw "Downloading $($pin.windows.url) failed (curl exit $LASTEXITCODE)." }
        $hash = (Get-FileHash $archive -Algorithm SHA256).Hash
        if ($hash -ne $pin.windows.sha256) {
            Remove-Item $archive -Force
            throw "$archive has SHA-256 $hash, not the $($pin.windows.sha256) Java/jdk.json pins; not installing it."
        }
    }

    # Unpacked beside its final place first, so an interrupted install never looks like a whole one.
    $staging = Join-Path $root 'windows.unpacking'
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $staging | Out-Null
    & tar.exe -xf $archive -C $staging
    if ($LASTEXITCODE -ne 0) { throw "Unpacking $archive failed (tar exit $LASTEXITCODE)." }
    Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
    Move-Item (Join-Path $staging $pin.release) $dest
    Remove-Item $staging -Recurse -Force
    Remove-Item $archive -Force
    Write-Host "Installed $($pin.release) at $dest." -ForegroundColor Green
}

# java -version writes to stderr, which PowerShell 5 would turn into a terminating error under 'Stop'.
$ErrorActionPreference = 'Continue'
& $java -version 2>&1 | ForEach-Object { Write-Host "  $_" }
