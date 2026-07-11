# Security Policy

## Supported versions

The latest release is the supported version.

## Reporting a vulnerability

Please open a private security advisory on GitHub if available, or contact the repository owner directly.

Do not include exploit details in a public issue until the issue has been reviewed.

## Security model

DS4 Battery Tray:

- Does not install a driver or service.
- Does not require administrator privileges for normal use.
- Does not collect telemetry.
- Does not send network requests.
- Reads local Windows device state and DS4 HID input reports.
- Writes DS4 light-bar output reports only after the user selects an active light-bar mode or invokes the one-shot diagnostic command.
- Stores light-bar mode and color under the current user's registry profile; it does not require administrator access.

Unsigned community builds may trigger Windows SmartScreen. Build from source if you prefer not to run downloaded binaries.
