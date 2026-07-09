# Release Checklist

Before publishing a release:

1. Build with `.\Build.ps1`.
2. Test Bluetooth DS4 battery reading.
3. Test USB DS4 battery reading.
4. Test `Start with Windows`.
5. Test HidHide whitelisting.
6. Test `Copy diagnostics` and `Save diagnostics...`.
7. Run one-shot diagnostics:

```powershell
.\DS4BatteryTray.exe --status-once --status-file .\DS4BatteryTray-diagnostics.txt
```

8. Remove diagnostic files.
9. Update `CHANGELOG.md`.
10. Tag the release.

Recommended release asset:

```text
DS4BatteryTray.zip
```

Include:

- `DS4BatteryTray.exe`
- `Start-DS4BatteryTray.cmd`
- `Install-Startup.cmd`
- `Uninstall-Startup.cmd`
- `README.md`
- `docs/`
- `LICENSE`
