@echo off
rem Double-click to open the CLONE-Z map scraper menu (zomboidmap.com rip).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0MapScraper.ps1" %*
if "%~1"=="" pause
