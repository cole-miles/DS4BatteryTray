# Contributing

Thanks for improving DS4 Battery Tray.

## Before opening a bug

Run diagnostics and include the output:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

Also mention:

- Bluetooth or USB.
- HidHide status.
- Whether XOutput, ViGEm, DS4Windows, Steam Input, or another mapper is running.

## Development

Build with:

```powershell
.\Build.ps1
.\Test.ps1
```

The project intentionally avoids external dependencies. Keep changes small, Windows-native, and compatible with a no-installer workflow.

## Pull requests

Please include:

- What changed.
- Why it changed.
- How it was tested.
- Any controller tools involved in testing.
