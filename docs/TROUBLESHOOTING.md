# Troubleshooting

## DS4 connected, battery unavailable

Common causes:

- HidHide is hiding the physical DS4 from `DS4BatteryTray.exe`.
- XOutput, DS4Windows, Steam Input, or another mapper has exclusive access to the physical DS4.
- Windows is exposing only a virtual Xbox controller, not the physical Sony HID device.
- The controller needs to be re-paired or reconnected.

Steps:

1. Confirm `DS4BatteryTray.exe` is whitelisted in HidHide.
2. Restart DS4 Battery Tray after changing HidHide settings.
3. Temporarily close XOutput, DS4Windows, Steam, and other controller tools.
4. Reconnect the controller.
5. Run diagnostics:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

## HidHide checklist

In HidHide Configuration Client:

- Add the full path to `DS4BatteryTray.exe`.
- Confirm the physical DS4 is not hidden from whitelisted applications.
- Apply changes.
- Restart DS4 Battery Tray.

## Bluetooth checklist

- Remove and re-pair the controller in Windows Bluetooth settings.
- Hold `Share + PS` until the light bar flashes for pairing.
- Avoid low-quality Bluetooth adapters if reports are intermittent.
- Prefer the Microsoft Bluetooth stack.

## USB checklist

- Use a data-capable USB cable, not a charge-only cable.
- Try another USB port.
- Confirm the controller appears in `joy.cpl`.
- Close tools that may exclusively own the physical controller.

## XOutput and ViGEm checklist

DS4 Battery Tray reads the physical Sony controller. XOutput and ViGEm expose a virtual Xbox controller, which normally does not carry the DS4 battery report.

If the virtual Xbox controller works but battery is unavailable:

1. Confirm the physical DS4 is visible to DS4 Battery Tray.
2. Confirm XOutput is not using exclusive access.
3. Start DS4 Battery Tray before XOutput as a test.

## SmartScreen warning

If you build or download an unsigned executable, Windows may show a SmartScreen prompt. This is expected for unsigned community builds. Review the source, build locally if preferred, and only run binaries you trust.

## Useful diagnostic fields

- `Connected`: whether the app found a readable DS4 battery source.
- `Percent`: decoded battery percentage.
- `BatteryStatus`: `Charging`, `Discharging`, `Full`, or `Unknown`.
- `Source`: Windows battery API or direct DS4 HID input report.
- `Detail`: low-level device/report detail.
- `Error`: last read error if battery is unavailable.
