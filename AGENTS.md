# Codex Instructions For This Repository

These instructions are mandatory for future Codex sessions working on NetWatch Lite Wallboard.

## Start Here

Before changing code, read these files in this order:

1. `CURRENT_STATUS.md`
2. `README.md`
3. `docs/developer-guide.md`
4. `docs/user-guide.md`
5. `CHANGELOG.md`

Then inspect the current Git state:

```powershell
git status --short --branch
git log -3 --oneline
```

## Project Scope

NetWatch Lite Wallboard is a Windows desktop wallboard built with .NET 8, WinForms, and Microsoft Edge WebView2.

The current project scope is page display, page refresh, panel restart, rotation, profiles, diagnostics, kiosk behavior, memory visibility, low-power behavior, auto-hide top bar, and native shell themes.

Do not reintroduce the removed scraping subsystem unless the user explicitly changes the product direction.

Removed concepts that should stay out:

- DOM scraping.
- DOM selector polling.
- Monitoring JSON rules.
- Alarm sound configuration.
- Severity colors.
- Page-content alarm banners.
- JavaScript injected to inspect page content.
- `docs/scraping-test-page.html`.

## Current Architecture

Important files:

- `src/NetWatchLite.Wallboard.WebView2/Program.cs`: app startup and global exception handlers.
- `src/NetWatchLite.Wallboard.WebView2/WallboardForm.cs`: main window, layout, rotation, fullscreen, low-power mode, restart all, memory monitor, top-bar auto-hide, settings, diagnostics.
- `src/NetWatchLite.Wallboard.WebView2/WebViewPanelControl.cs`: one WebView2 panel, refresh timer, per-panel refresh/restart, navigation status, error page.
- `src/NetWatchLite.Wallboard.WebView2/SettingsForm.cs`: visual JSON configuration editor.
- `src/NetWatchLite.Wallboard.WebView2/WallboardConfigReader.cs`: JSON path resolution, profile selection, normalization, safe save, backup, verification.
- `src/NetWatchLite.Wallboard.WebView2/WallboardConfiguration.cs`: top-level config schema.
- `src/NetWatchLite.Wallboard.WebView2/WallboardPanel.cs`: panel config schema.
- `src/NetWatchLite.Wallboard.WebView2/ThemePalette.cs`: native shell color themes.
- `src/NetWatchLite.Wallboard.WebView2/DiagnosticsForm.cs`: read-only runtime diagnostics.
- `Data/wallboard.json`: default development configuration.
- `Data/wallboard.demo.json`: demo profile.
- `Data/wallboard.production.json`: production-style profile.
- `scripts/publish-portable.ps1`: portable publish helper.

## Development Rules

- Prefer small, safe changes that match existing WinForms patterns.
- Keep JSON backward-compatible when adding configuration fields.
- Normalize every user-editable JSON value in `WallboardConfigReader`.
- If a setting is user-facing, expose it in `SettingsForm`, document it in README/user guide, and mention it in developer guide when relevant.
- Keep per-panel behavior inside `WebViewPanelControl`.
- Keep whole-wallboard behavior inside `WallboardForm`.
- Dispose WebView2 controls and stop timers when rebuilding or closing panels.
- Avoid changing unrelated files.
- Do not commit or publish `publish/`, `bin/`, `obj/`, `*.zip`, `*.backup.json`, or local logs.

## Validation Commands

Run these before finishing code changes:

```powershell
dotnet build .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
git diff --check
```

Expected current build note:

- Build succeeds.
- Warning `MSB3277` about `WindowsBase` and WebView2 may appear. This warning is known and was present in v0.3.0.

For a portable release:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-portable.ps1
```

Verify the generated folder does not include `wallboard.backup.json`.

## Git And Release Notes

The main branch is `main`.

When making a release-level change:

1. Update version in `src/NetWatchLite.Wallboard.WebView2/NetWatchLite.Wallboard.WebView2.csproj`.
2. Update `CHANGELOG.md`.
3. Build.
4. Create portable package.
5. Commit.
6. Push to `origin/main` after user approval or explicit request.

## User Preference

The user prefers Spanish conversation and practical, direct updates.

When explaining app changes, use non-technical language first, then include file paths or commands only when useful.
