# User Guide

## Launching the app

Run `Start-DS4BatteryTray.cmd` or launch `DS4BatteryTray.exe` directly.

The app appears in the Windows notification area. If Windows hides the icon, open the notification-area overflow menu and drag DS4 Battery Tray into the visible taskbar area.

## Tray menu

Right-click the tray icon for:

- `Refresh`: immediately re-check controller battery state.
- `Light bar`: choose battery-following color, a static color, off, or leave lighting unchanged.
- `Bluetooth settings`: open Windows Bluetooth settings.
- `Game controllers`: open the Windows controller panel.
- `Start with Windows`: create or remove the user Startup shortcut.
- `Copy diagnostics`: copy current battery/device status to the clipboard.
- `Save diagnostics...`: save current diagnostics to a text file.
- `Troubleshooting guide`: open the local troubleshooting guide.
- `Open app folder`: open the application folder.
- `About`: show app information.
- `Exit`: close the tray app.

Left-click the tray icon to show a short status balloon. Double-click it to open Bluetooth settings.

## Light bar

The app starts in `Leave unchanged` mode and does not write lighting data until you make a selection.

- `Follow battery` mirrors the tray status colors and updates when the battery band or charging state changes.
- `Static color...` opens the Windows color picker and remembers the selected RGB color.
- `Off` turns the light bar off while the physical controller is available.
- `Leave unchanged` stops DS4 Battery Tray from sending light-bar updates.

The app reapplies an active mode when the controller reconnects. If another controller tool also manages the light bar, the most recent writer wins; select `Leave unchanged` to avoid competing updates.

## Startup

Enable `Start with Windows` from the tray menu, or run:

```cmd
Install-Startup.cmd
```

To remove the startup shortcut:

```cmd
Uninstall-Startup.cmd
```

## HidHide setup

If HidHide is enabled, add `DS4BatteryTray.exe` to HidHide's application whitelist. Do not whitelist `powershell.exe`; the supported app is the dedicated executable.

After changing HidHide settings, restart DS4 Battery Tray.

## Battery sources

The app reads battery state in this order:

1. Windows device battery API.
2. Direct DS4 HID input report.

The HID fallback supports both USB-style reports and Bluetooth extended reports.

Windows may expose a fine-grained percentage. The direct HID fallback exposes coarse controller steps, displayed as midpoint estimates such as 65% or 75%. Use diagnostics to see which source is active.

## Light-bar diagnostics

Copied and saved diagnostics include the selected mode, static color, last write detail, and last write error. A one-time hardware check is also available:

```powershell
.\DS4BatteryTray.exe --lightbar-once '#0078D4' --status-file .\lightbar-check.txt
```

This command does not change the mode saved in the tray app.
