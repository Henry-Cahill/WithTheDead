<#
.SYNOPSIS
    A harmless job for trying out the release console: it prints a line a second for ten minutes, with a child process
    (ping) running beside it, and changes nothing. Cancel on the page stops both.

.DESCRIPTION
    Its output stays in Windows PowerShell's own code page, so the line with accents also shows that the console reads
    that correctly.
#>
$ErrorActionPreference = 'Stop'
$child = Start-Process ping.exe -ArgumentList '-n', '601', '127.0.0.1' -WindowStyle Hidden -PassThru
Write-Host "Self-test as $env:USERNAME on $env:COMPUTERNAME, with ping (process $($child.Id)) running beside it."
Write-Host "Accents from Windows PowerShell's code page: caf$([char]0xE9), na$([char]0xEF)ve, se$([char]0xF1)or."
for ($i = 1; $i -le 600; $i++) {
    Write-Host "Self-test: line $i of 600"
    Start-Sleep -Seconds 1
}
Write-Host 'Self-test: done.'
