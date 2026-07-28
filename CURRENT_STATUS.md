# Current Status

Last updated: 2026-07-27

## Repository State

- Repository path: `C:\Projects\netwatch-lite-wallboard`
- GitHub repository: `https://github.com/dafermen/netwatch-lite-wallboard`
- Main branch: `main`
- Last confirmed synced commit before this continuity update: `250b95e Release wallboard v0.3.0`
- Current release version in project file: `0.3.0`

## Product Direction

NetWatch Lite Wallboard is now a focused WebView2 wallboard for showing operational pages.

The scraping/DOM monitoring/alarm feature set was intentionally removed. Future work should not bring it back unless the user explicitly asks for a product direction change.

## Finished In v0.3.0

- Removed DOM scraping and monitoring models.
- Removed alarm options, alarm sounds, severity colors, selector rules, and monitoring JSON editor.
- Removed `docs/scraping-test-page.html`.
- Simplified panel runtime to WebView2 page display, refresh, restart, rotation, profiles, diagnostics, and low-power behavior.
- Added per-panel **Refresh** and **Restart** actions.
- Added global **Refresh All** and **Restart All** actions.
- Added scheduled visible-panel restart with `autoRestartEnabled` and `autoRestartHours`.
- Added kiosk options with `startFullscreen` and `confirmExit`.
- Added memory monitor with `memoryMonitorEnabled` and `memoryWarningMegabytes`.
- Added top-bar auto-hide with `autoHideTopBarEnabled`.
- Added shell themes with `theme`: `dark`, `light`, `high-contrast`.
- Added `ThemePalette.cs`.
- Added environment profile examples:
  - `Data/wallboard.demo.json`
  - `Data/wallboard.production.json`
- Added portable publish script:
  - `scripts/publish-portable.ps1`
- Updated README, user guide, developer guide, and changelog.
- Created portable package:
  - `publish/NetWatch-Lite-Wallboard-WebView2-win-x64-v0.3.0.zip`

## Last Validation Performed

The v0.3.0 code was validated with:

```powershell
dotnet build .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
git diff --check
```

Result:

- Build succeeded.
- `0 errors`.
- Known warning: `MSB3277` conflict between `WindowsBase` versions from WebView2/WPF references.
- Portable publish succeeded.
- Portable was checked to ensure `wallboard.backup.json` was not included.
- GitHub `origin/main` was confirmed pointing to `250b95e`.

## Current Open Work

No critical implementation work is currently open.

Recommended next validation:

- Run the app manually from source.
- Open Settings and verify the Runtime options layout at common display widths.
- Test each theme visually.
- Test auto-hide top bar on a TV-like fullscreen window.
- Test `confirmExit` with `ESC` and the window close button.
- Let memory monitor run for several minutes and confirm status text remains readable.
- Test `--environment demo` and `--environment production`.
- Publish portable again after any new code or documentation changes.

## Possible Next Improvements

High-value next steps:

1. Add an optional UI command to open the active JSON file location.
2. Add a small profile selector in Settings so users can switch between `default`, `demo`, and `production` without command-line arguments.
3. Add an in-app "About" dialog showing version, build, config path, and portable folder.
4. Add lightweight tests for JSON normalization if a test project is introduced.
5. Add a release checklist section to documentation.
6. Investigate the known `MSB3277` `WindowsBase` warning and decide whether it is worth suppressing or resolving.

## Handoff Notes For Future Codex Sessions

- Start by reading `AGENTS.md`.
- Do not assume scraping exists; it was removed.
- Do not edit generated artifacts under `publish/`, `bin/`, or `obj/`.
- If creating a new release, update the version number and changelog first.
- If adding configuration fields, update:
  - `WallboardConfiguration.cs`
  - `WallboardConfigReader.cs`
  - `SettingsForm.cs`
  - `README.md`
  - `docs/user-guide.md`
  - `docs/developer-guide.md`
  - `Data/wallboard*.json`
