@echo off
rem Double-click to open the CLONE-Z LLM asset pipeline menu.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0AssetPipeline.ps1" %*
if "%~1"=="" pause
