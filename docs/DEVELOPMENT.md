# Development

## Toolchain

The app is intentionally dependency-light:

- C# / WinForms.
- .NET Framework compiler at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.
- No NuGet packages.

Build:

```powershell
.\Build.ps1
```

Run protocol tests:

```powershell
.\Test.ps1
```

Run:

```powershell
.\DS4BatteryTray.exe
```

One-shot diagnostics:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\status-check.txt
```

## Architecture

- `TrayApplicationContext`: notification-area UI and menu behavior.
- `BatteryReader`: source selection and final user-facing battery state.
- `WinRtBatteryApi`: late-bound Windows battery API access.
- `DirectHidBatteryReader`: DS4 HID enumeration and report decoding.
- `Core.Battery.Ds4BatteryReportParser`: transport-independent DS4 battery report parsing.
- `TrayIconFactory`: dynamic tray icon rendering.

## DS4 battery decoding

The direct HID fallback supports:

- USB-style input report `0x01`, power byte at index `30`.
- Bluetooth extended input report `0x11`, power byte at index `32`.

The low nibble is decoded as the DS4 battery level. `0x0B` is treated as `Full`. Raw levels below 10 are displayed as coarse midpoint estimates (`5`, `15`, ... `95`); they are not one-percent measurements.

## Release build

Run:

```powershell
.\Build.ps1
.\DS4BatteryTray.exe --status-once --status-file .\status-check.txt
```

Then inspect `status-check.txt` and remove it before committing.
