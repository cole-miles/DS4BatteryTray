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

One-shot light-bar write:

```powershell
.\DS4BatteryTray.exe --lightbar-once '#0078D4' --status-file .\lightbar-check.txt
```

## Architecture

- `TrayApplicationContext`: notification-area UI and menu behavior.
- `BatteryReader`: source selection and final user-facing battery state.
- `WinRtBatteryApi`: late-bound Windows battery API access.
- `DirectHidBatteryReader`: DS4 HID enumeration and report decoding.
- `Core.Battery.Ds4BatteryReportParser`: transport-independent DS4 battery report parsing.
- `Core.Output.Ds4LightBarReportBuilder`: USB/Bluetooth output report construction, color policy, and CRC.
- `DirectHidLightBarWriter`: physical DS4 output transport with control-transfer and stream-write paths.
- `LightBarSettingsStore`: current-user mode/color persistence under `HKCU\Software\ColeMiles\DS4BatteryTray`.
- `TrayIconFactory`: dynamic tray icon rendering.

## DS4 battery decoding

The direct HID fallback supports:

- USB-style input report `0x01`, power byte at index `30`.
- Bluetooth extended input report `0x11`, power byte at index `32`.

The low nibble is decoded as the DS4 battery level. `0x0B` is treated as `Full`. Raw levels below 10 are displayed as coarse midpoint estimates (`5`, `15`, ... `95`); they are not one-percent measurements.

## DS4 light-bar output

The output builder follows the upstream `hid-playstation` report structures:

- USB report `0x05`, 32 bytes, LED-valid flag at index `1`, RGB at indexes `6-8`.
- Bluetooth report `0x11`, 78 bytes, hardware control `0xC4`, LED-valid flag at index `3`, RGB at indexes `8-10`.
- Bluetooth CRC-32 occupies the final four bytes and uses output seed `0xA2`.

Only the LED-valid flag is set. Motor/rumble fields are not marked valid, so a light-bar update does not intentionally alter rumble state.

## Release build

Run:

```powershell
.\Build.ps1
.\DS4BatteryTray.exe --status-once --status-file .\status-check.txt
```

Then inspect `status-check.txt` and remove it before committing.
