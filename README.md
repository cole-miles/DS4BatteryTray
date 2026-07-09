# DS4 Battery Tray

DS4 Battery Tray is a lightweight Windows 11 notification-area app that shows the battery level of a DualShock 4 controller connected over Bluetooth or USB.

It is designed for setups that use tools such as HidHide, ViGEm, XOutput, DS4Windows-style input stacks, and Xbox app games. The app reads the physical Sony controller, not the virtual Xbox controller.

## Features

- Live tray icon with battery fill and status color.
- Bluetooth and wired USB DualShock 4 support.
- Direct DS4 HID battery decoding when Windows does not expose a battery device.
- Windows device battery API support when available.
- Start with Windows toggle from the tray menu.
- Low-battery notifications.
- Copy or save diagnostics from the tray menu.
- Quick links to Bluetooth settings, Windows Game Controllers, the app folder, and troubleshooting.
- Single small executable, no installer, no service, no driver.

## Requirements

- Windows 11, or Windows 10 with the same desktop APIs available.
- Sony DualShock 4 controller.
- Microsoft .NET Framework 4.x runtime, included with modern Windows installations.
- Optional: HidHide whitelist entry for `DS4BatteryTray.exe` when HidHide is enabled.

## Quick Start

1. Download the repository or a release ZIP.
2. Run `Start-DS4BatteryTray.cmd`.
3. If HidHide is enabled, whitelist `DS4BatteryTray.exe`.
4. Right-click the tray icon and enable `Start with Windows` if desired.

The tray icon color indicates status:

- Green: more than 50%.
- Yellow: 21-50%.
- Red: 20% or lower.
- Blue lightning bolt: charging.
- Gray with an X: disconnected or unavailable.

## HidHide, ViGEm, and XOutput

Whitelist this exact executable in HidHide:

```text
DS4BatteryTray.exe
```

The app intentionally ignores the ViGEm/XOutput virtual Xbox controller. It reads battery data from the physical DS4 HID device. If another application opens the physical controller exclusively, DS4 Battery Tray may not be able to read the direct HID report until that app releases or shares access.

## Build From Source

The project builds with the Windows .NET Framework compiler that ships with Windows:

```powershell
.\Build.ps1
```

The output is:

```text
DS4BatteryTray.exe
```

No NuGet restore is required.

## Diagnostics

Use the tray menu:

- `Copy diagnostics`
- `Save diagnostics...`

Or run:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

Diagnostics include connection state, decoded percentage, battery status, read source, and the low-level detail needed for troubleshooting.

## Documentation

- [User Guide](docs/USER_GUIDE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Development](docs/DEVELOPMENT.md)
- [Release Checklist](docs/RELEASE.md)
- [Contributing](CONTRIBUTING.md)
- [Security](SECURITY.md)

## Safety and Privacy

DS4 Battery Tray does not collect telemetry, does not phone home, and does not install drivers or services. It reads local Windows device state and DS4 HID input reports only.

## License

MIT. See [LICENSE](LICENSE).
=======
# DS4BatteryLevel
A simple Windows application to show the battery level of a DS4 controller connected via Bluetooth
