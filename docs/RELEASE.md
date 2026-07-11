# Release Checklist

Before publishing a release:

1. Run `.\Test.ps1`.
2. Build with `.\Build.ps1`.
3. Test Bluetooth DS4 battery reading.
4. Test USB DS4 battery reading.
5. Test static, battery-following, off, and leave-unchanged light-bar modes over USB.
6. Test the same light-bar modes over Bluetooth.
7. Test `Start with Windows`.
8. Test HidHide whitelisting.
9. Test `Copy diagnostics` and `Save diagnostics...`.
10. Run one-shot diagnostics:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

11. Run `--lightbar-once` over both USB and Bluetooth.
12. Remove diagnostic files.
13. Update `CHANGELOG.md`.
14. Create and push a signed or annotated `vX.Y.Z` tag.
15. Confirm the GitHub release workflow publishes the ZIP and SHA-256 checksum.

The release workflow produces:

```text
DS4BatteryTray-vX.Y.Z.zip
DS4BatteryTray-vX.Y.Z.zip.sha256
```

Include:

- `DS4BatteryTray.exe`
- `Start-DS4BatteryTray.cmd`
- `Install-Startup.cmd`
- `Uninstall-Startup.cmd`
- `README.md`
- `docs/`
- `LICENSE`

Do not commit `DS4BatteryTray.exe`. GitHub Actions builds the executable from the tagged source.
