# Codex Instructions For This Repository

These instructions are mandatory for future Codex sessions working on NetWatch Lite Wallboard.

## Start Here

Before changing code, read these files in this order:

1. `CURRENT_STATUS.md`
2. `README.md`
3. `docs/README.md`
4. `docs/DOCUMENTATION_STANDARD.md`
5. `docs/ARCHITECTURE.md`
6. `docs/CONFIGURATION.md`
7. `docs/DEVELOPMENT.md`
8. `docs/TESTING.md`
9. `docs/developer-guide.md`
10. `docs/user-guide.md`
11. `CHANGELOG.md`

Then inspect the current Git state:

```powershell
git status --short --branch
git log -3 --oneline
```

## Project Scope

NetWatch Lite Wallboard is a Windows desktop wallboard built with .NET 10 LTS, WinForms, and Microsoft Edge WebView2.

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
- `scripts/test-portable.ps1`: isolated exact-ZIP/folder package checks; evidence under `artifacts/verification/`.
- `scripts/build-launcher.ps1`, `src/NetWatchLite.Wallboard.Launcher/Launcher.cs`, and `PackageIntegrity.cs`: Windows Framework startup validator, preserving arguments and checking the embedded/external manifest before loading the .NET 10 payload. The validator source is linked into xUnit and compiled independently during publish.
- `docs/PORTABLE_INTEGRITY.md`: authoritative protected/editable scope, verification exit codes, diagnostics, limits, and recovery.
- `RollingFileLog.cs`, `SupportReport.cs`, and `MemoryProtectionPolicy.cs`: bounded log retention, allowlisted local reports, and explicitly enabled sustained-memory protection. Never export raw operator strings/logs or reset the two-attempt session budget on reload.
- `.github/workflows/prerelease.yml` and `scripts/publish-prerelease.ps1`: main-only manual artifact/draft/publish workflow, immutable source/ZIP binding, scoped permissions and download verification. Existing versions are never replaced; default workflow mode is artifact-only and local script mode is Draft. The user authorized only a real rc.5 draft in the 2026-11-11 work, not public publication.
- `src/NetWatchLite.Wallboard.WebView2/PortablePaths.cs`: shared root resolution for JSON and local pages; preserve old flat/development layouts.
- `AppIcon.cs`, `Assets/netwatch-lite.png`, `Assets/netwatch-lite.ico`, and `scripts/build-icon.ps1`: one embedded multi-resolution icon for all native windows and both executables. Keep per-window disposal and the icon regression tests; do not depend on loose assets at runtime.
- `docs/TESTING.md`: mandatory quality areas and release evidence rules.
- `docs/RELEASE_CHECKLIST.md`: release approval template.
- `docs/adr/`: durable architecture decisions.
- `docs/README.md`: authoritative documentation navigation.
- `docs/DOCUMENTATION_STANDARD.md`: mandatory documentation structure and navigation rules.
- `scripts/validate-docs.ps1`: local and CI documentation validation.
- `src/NetWatchLite.Wallboard.WebView2/MemoryMonitor.cs`: distinct-process sampling and bounded history.
- `src/NetWatchLite.Wallboard.WebView2/PanelRecoveryPolicy.cs`: persistent retry budget and recovery delays.
- `tests/NetWatchLite.Wallboard.Tests/`: xUnit unit, configuration integration, contract, and opt-in real-WebView2 tests.

## Development Rules

- Prefer small, safe changes that match existing WinForms patterns.
- Keep JSON backward-compatible when adding configuration fields.
- Normalize every user-editable JSON value in `WallboardConfigReader`.
- If a setting is user-facing, expose it in `SettingsForm`, document it in README/user guide, and mention it in developer guide when relevant.
- Keep per-panel behavior inside `WebViewPanelControl`.
- Keep whole-wallboard behavior inside `WallboardForm`.
- Dispose WebView2 controls and stop timers when rebuilding or closing panels.
- Avoid changing unrelated files.
- Do not commit generated `publish/`, `artifacts/`, `bin/`, `obj/`, `*.zip`, `*.backup.json`, or local logs. Only approved clean package files are distributable.
- Do not add React/Node conventions such as `package.json` or `.env` files unless the technology stack actually changes.
- Do not claim a planned or manual test is automated.
- Keep every document under `docs/` reachable from its index and preserve the `docs-nav` block.
- Do not introduce a documentation framework or web route without a real hosting requirement and an ADR.

## Validation Commands

Run these before finishing code changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\validate-docs.ps1
dotnet build .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
dotnet test .\netwatch-lite-wallboard.slnx -c Release
git diff --check
```

Expected current build note:

- Build succeeds.
- Warning `MSB3277` about `WindowsBase` and WebView2 may appear. This warning is known and was present in v0.3.0.

Keep `global.json`, CI, app, and test projects on .NET 10. Preserve retry state across automatic panel replacements. Never count unrelated `msedgewebview2` processes. Real-browser tests require an interactive Windows session and `NETWATCH_WEBVIEW2_TESTS=1`; the long resource test is run explicitly through `scripts/test-soak.ps1`. A short run does not establish 24-hour stability.

For a portable release:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-portable.ps1
```

Verify the generated folder does not include `wallboard.backup.json`.

Portable output uses `artifacts/portable/<full-version>/` and `artifacts/packages/`, includes .NET 10 Desktop Runtime inside `app/`, and keeps JSON/local pages at the root. The root launcher uses Windows .NET Framework 4.x; WebView2 Runtime remains external. Publishing refuses existing outputs, builds in staging, and tests the exact ZIP before promotion. Never delete or overwrite an existing deployment to rebuild. `-NoZip` still tests a disposable folder copy. Preserve profile aliases, command-line precedence, root save/backup paths, LocalAppData browser sessions, and legacy flat/development paths. The read-only `--package-check` probe never saves configuration or opens WebView2; its report must be a new absolute path.

Every new portable delivered after a build MUST include a versioned ZIP and matching `.zip.sha256` in this project's `artifacts/packages/`, alongside the extracted version under `artifacts/portable/`. Keep previous versions as local history; never overwrite them. Use the default ZIP publication path and test that exact archive. `-NoZip` is only for preliminary validation or an explicit user request, never the final deliverable. A development-only build does not itself create a portable. When publishing a binary version on GitHub, attach the validated ZIP and checksum to its Release (or explicitly authorized evaluation prerelease), not to Git history; a source push alone does not upload the local ZIP. Report source synchronization and binary publication separately and preserve release-approval gates.

Stage 2 checks every `app/` file and `portable-layout.json` against size/SHA-256 metadata embedded in the Framework launcher, requiring an identical external `integrity-manifest.xml`. Generate only from finalized fresh output. Never hash/parse/repair editable wallboard JSON or browser data in the launcher, regenerate a manifest to silence failure, or add an integrity-disable setting. Reject unsafe paths, extra payload files and reparse points; preserve the documented local-folder restriction and bounds. `--verify-only` never starts the payload or overwrites a report; quiet verification/package-check failures do not write live startup logs. Real package tests mutate only fresh disposable copies, prove failed checks block the child, and restore fixtures. Hashes are not publisher signing or protection against replacement of the whole launcher/reference, changes after checking, or direct payload starts.

Memory protection stays off in old/example JSON unless explicitly enabled by the operator. Ignore partial/discontinuous samples and editing/minimized/rendering states; use existing serialized panel recreation, never kill processes or clear sessions. Profile switching is session-only and strictly validates the exact selected file before changing active state; CLI startup precedence/fallback/save contracts remain unchanged. Support export is an allowlist, not regex-redacted logs; keep Copy/raw diagnostics separate. Run both xUnit and `scripts/test-release-package.ps1` for these boundaries. Do not equate a mocked publisher, a real draft, and a public prerelease.

Release scripts run under Windows PowerShell 5.1 on hosted Windows. Do not interpolate quoted string literals into native `gh --jq` arguments: legacy native argument passing can remove those quotes. Extract fixed JSON fields and compare using PowerShell objects/values instead. Test live read-only CLI boundaries separately from mocks before a draft run.

Before any deployable release, follow all 13 quality areas in `docs/TESTING.md` and complete `docs/RELEASE_CHECKLIST.md`. Every area must be `Passed`, `Failed`, or `Not applicable` with a reason. Do not publish with an applicable failure or untested area.

## Git And Release Notes

The main branch is `main`.

When making a release-level change:

1. Update version in `src/NetWatchLite.Wallboard.WebView2/NetWatchLite.Wallboard.WebView2.csproj`.
2. Update `CHANGELOG.md`.
3. Complete and record the release gates in `docs/TESTING.md`.
4. Build.
5. Create and inspect the portable package.
6. Test the exact ZIP intended for distribution.
7. Commit.
8. Push to `origin/main` after user approval or explicit request.

## User Preference

The user prefers Spanish conversation and practical, direct updates.

When explaining app changes, use non-technical language first, then include file paths or commands only when useful.
