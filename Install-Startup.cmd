@echo off
setlocal
set "APP_DIR=%~dp0"
if not exist "%APP_DIR%DS4BatteryTray.exe" (
    "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%APP_DIR%Build.ps1"
    if errorlevel 1 (
        pause
        exit /b 1
    )
)
"%APP_DIR%DS4BatteryTray.exe" --install-startup
pause
