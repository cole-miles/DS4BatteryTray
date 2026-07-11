# DS4 Battery Tray

DS4 Battery Tray is a small, local-first Windows notification-area app that shows the battery state of a physical Sony DualShock 4 controller over Bluetooth or USB.

It is designed to coexist with HidHide, ViGEm, XOutput, Steam Input, and similar controller stacks. It reads the physical Sony controller rather than a virtual Xbox controller.

## Features

- Live battery icon in the Windows notification area.
- Bluetooth and wired USB DualShock 4 support.
- Windows battery API with direct HID fallback.
- Light-bar modes for battery status, a static color, or off.
- Automatic launch at Windows sign-in.
- Low-battery and connection notifications.
- Copyable and saveable diagnostics.
- No installer, service, driver, account, telemetry, or network access.

## Compatibility

| Controller | Connection | Battery | Light bar | Notes |
| --- | --- | --- | --- | --- |
| DualShock 4 v1 | USB | Supported | Supported | Direct physical HID access |
| DualShock 4 v1 | Bluetooth | Supported | Supported | HidHide whitelist may be required |
| DualShock 4 v2 | USB | Supported | Supported | Direct physical HID access |
| DualShock 4 v2 | Bluetooth | Supported | Supported | HidHide whitelist may be required |
| Licensed DS4 variants | USB/Bluetooth | Best effort | Best effort | Known Sony-compatible product IDs are included |
| DualSense / DualSense Edge | USB/Bluetooth | Not yet supported | Not yet supported | Planned as a separate controller profile |

Windows 11 is the primary supported operating system. Windows 10 may work when the same desktop APIs are available.

## Install

1. Download the latest portable ZIP from [GitHub Releases](https://github.com/cole-miles/DS4BatteryTray/releases/latest).
2. Extract the complete ZIP to a permanent folder.
3. Run `Start-DS4BatteryTray.cmd`.
4. Right-click the tray icon and enable `Start with Windows`.

If HidHide is enabled, add the extracted `DS4BatteryTray.exe` to HidHide's application whitelist, apply the configuration, and restart the app.

The tray icon uses green, yellow, and red for charge level, blue for charging, and gray when the controller is unavailable. Windows may initially place it in the notification-area overflow menu.

## Light Bar

Open `Light bar` from the tray menu and choose:

- `Follow battery`: green, yellow, red, or charging blue using the same thresholds as the tray icon.
- `Static color...`: choose a persistent RGB color with the Windows color picker.
- `Off`: send black to turn the light bar off.
- `Leave unchanged`: stop sending light-bar updates and leave control to the controller or another application.

`Leave unchanged` is the default, so installing or updating DS4 Battery Tray does not take light-bar ownership without an explicit choice. The selected mode and static color are stored for the current Windows user.

## Battery Precision

The app shows the best data the controller and Windows expose:

- The Windows battery API can provide a fine-grained percentage on some systems.
- A direct DS4 HID report exposes coarse battery steps. Values such as 65% or 75% are midpoint estimates for those steps, not exact one-percent measurements.

Diagnostics always identify the active source. This limitation comes from the controller report and cannot be improved by interpolating invented values.

## Controller Tools

HidHide, ViGEm, and XOutput do affect device visibility. DS4 Battery Tray does not replace those tools and does not emulate an Xbox controller.

- Whitelist the exact `DS4BatteryTray.exe` path in HidHide.
- Keep the app in a permanent folder before enabling startup or adding the whitelist entry.
- Another app may prevent direct battery reads if it opens the physical controller exclusively.
- Another app may overwrite or compete with light-bar updates. Use `Leave unchanged` when another controller tool owns lighting.
- The virtual Xbox controller does not contain the DS4 battery report; the physical Sony HID device must remain visible to this app.

See [Troubleshooting](docs/TROUBLESHOOTING.md) for step-by-step checks.

## Diagnostics

Use `Copy diagnostics` or `Save diagnostics...` from the tray menu, or run:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

Test one light-bar write without changing the saved mode:

```powershell
.\DS4BatteryTray.exe --lightbar-once '#0078D4' --status-file .\lightbar-check.txt
```

Include the generated output when reporting a controller-detection issue. It contains device state and errors, but no account credentials or network data.

## Build and Test

The project uses the .NET Framework compiler included with modern Windows and has no package restore:

```powershell
.\Build.ps1
.\Test.ps1
```

`Build.ps1` creates `DS4BatteryTray.exe`. Generated executables are distributed through GitHub Releases rather than committed to source control.

## Documentation

- [User guide](docs/USER_GUIDE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Development](docs/DEVELOPMENT.md)
- [Release process](docs/RELEASE.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

## Roadmap

Development is intentionally staged: the modular battery foundation and DS4 light-bar control are implemented; DualSense support is next, followed later by an optional Xbox-emulation backend. Emulation will remain optional so battery monitoring never requires a third-party driver.

## Privacy and License

DS4 Battery Tray performs local device reads and explicitly selected DS4 light-bar writes only. It does not collect telemetry or make network requests.

Licensed under the [MIT License](LICENSE). Third-party GPL code, including DS4Windows implementation code, must not be copied into this project.
