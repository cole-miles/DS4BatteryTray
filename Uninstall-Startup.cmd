@echo off
setlocal
set "APP_DIR=%~dp0"
if exist "%APP_DIR%DS4BatteryTray.exe" (
    "%APP_DIR%DS4BatteryTray.exe" --uninstall-startup
) else (
    "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -Command "$shortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'DS4 Battery Tray.lnk'; if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force; Write-Host ('Removed startup shortcut: ' + $shortcut) } else { Write-Host 'Startup shortcut was not installed.' }"
)
pause
