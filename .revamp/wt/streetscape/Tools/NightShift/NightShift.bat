@echo off
rem Double-click for the CLONE-Z Night Shift menu (local AI coding on the DGX Spark).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0NightShift.ps1" %*
if "%~1"=="" pause
