# Release Checklist

Before publishing a release:

1. Run `.\Test.ps1`.
2. Build with `.\Build.ps1`.
3. Test Bluetooth DS4 battery reading.
4. Test USB DS4 battery reading.
5. Test `Start with Windows`.
6. Test HidHide whitelisting.
7. Test `Copy diagnostics` and `Save diagnostics...`.
8. Run one-shot diagnostics:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

9. Remove diagnostic files.
10. Update `CHANGELOG.md`.
11. Create and push a signed or annotated `vX.Y.Z` tag.
12. Confirm the GitHub release workflow publishes the ZIP and SHA-256 checksum.

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
