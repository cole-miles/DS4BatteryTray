# Changelog

## Unreleased

- Improve DS4 Bluetooth light-bar delivery by using both Windows HID output paths.
- Support Windows Bluetooth DS4 collections that require a 547-byte HID output buffer.
- Reapply active light-bar settings during refreshes so controller mapping tools cannot leave a stale default color in place.

## Unreleased

- Fixed DS4 light-bar writes that Windows accepted but the controller ignored by preferring interrupt HID output over control-transfer fallback.
- Added DS4 light-bar control over USB and Bluetooth.
- Added battery-following, static color, off, and leave-unchanged modes.
- Added persistent current-user light-bar settings and a Windows color picker.
- Added one-shot light-bar diagnostics and HID write details.
- Added tested USB/Bluetooth report layouts and shared Bluetooth CRC handling.
- Extracted DS4 battery report parsing into a testable core module.
- Added deterministic USB, Bluetooth, charging, full, and invalid-report tests.
- Added automated tagged GitHub Releases with portable ZIP and SHA-256 checksum.
- Documented coarse direct-HID battery precision and the compatibility roadmap.
- Removed generated executables from source-control policy.
- Repaired README merge-conflict artifacts.

## 1.0.0

- Initial public release.
- Added Windows 11 tray battery icon.
- Added direct DS4 HID battery decoding for Bluetooth and USB.
- Added Windows battery API fallback.
- Added HidHide-friendly dedicated executable.
- Added startup shortcut support.
- Added low-battery notifications.
- Added copy/save diagnostics.
- Added full public documentation and troubleshooting guides.
