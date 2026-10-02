# Current Status

Last updated: 2026-08-01

## Repository State

- Repository path: `C:\Projects\netwatch-lite-wallboard`
- GitHub repository: `https://github.com/dafermen/netwatch-lite-wallboard`
- Main branch: `main`
- Last confirmed synced commit: `0bd93fe Add Codex handoff documentation`
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

Documentation and CI foundation validation on 2026-07-28:

- Release build succeeded with `0 errors`.
- The known `MSB3277` warning remains.
- NuGet reported no vulnerable direct or transitive packages from the configured source.
- Local Markdown links resolved.
- `git diff --check` reported no whitespace errors.
- Manual acceptance, end-to-end, soak, compatibility, and portable-package tests were not run because this is not yet a deployable release candidate.

Documentation navigation validation on 2026-08-01:

- `scripts/validate-docs.ps1` passed.
- Checked 24 Markdown files and 154 local links.
- Checked 3 documentation images with non-empty alternative text.
- Confirmed every document under `docs/` has standardized navigation.
- Confirmed no linked `docs/docs/` or unsupported root-relative documentation path exists.
- Release build succeeded with `0 errors`.
- The known `MSB3277` warning remains.
- Browser, responsive, theme, and `/docs/` route checks are not applicable because this repository uses GitHub-rendered Markdown and has no hosted documentation website.

## Current Open Work

No critical implementation work is currently open.

Documentation and quality-system work completed after v0.3.0:

- Technical reference set for architecture, configuration, development, testing, deployment, operations, security, and troubleshooting.
- Architecture Decision Records, beginning with the explicit no-scraping product boundary.
- Contribution guide and third-party license inventory.
- GitHub issue and pull request templates.
- Monthly Dependabot checks for NuGet and GitHub Actions.
- Windows CI workflow for release build, test-project discovery, NuGet vulnerability review, and portable package inspection.
- Release policy covering 13 test areas with explicit evidence and approval.
- Standardized GitHub Markdown navigation grouped by Product, Operations and Delivery, Engineering, and Project Management.
- Added consistent documentation home, project, status, previous, and next links.
- Added a documentation standard that records why no VitePress or web-only `/docs/` route is used.
- Added `scripts/validate-docs.ps1` and CI validation for links, images, navigation, index coverage, headings, and duplicate paths.

Current test-automation gap:

- No automated test project exists yet.
- CI reports this honestly and will discover future test projects under `tests/`.
- Unit, property, mutation, fuzzing, integration, contract, and end-to-end automation remain future implementation work.

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
4. Introduce the first unit test project for JSON normalization and add it to the solution.
5. Establish property tests and a Stryker.NET mutation baseline after unit coverage is meaningful.
6. Automate selected integration and portable end-to-end checks.
7. Investigate the known `MSB3277` `WindowsBase` warning and decide whether it is worth suppressing or resolving.

## Handoff Notes For Future Codex Sessions

- Start by reading `AGENTS.md`.
- Do not assume scraping exists; it was removed.
- Do not edit generated artifacts under `publish/`, `bin/`, or `obj/`.
- Read `docs/TESTING.md` before release-level work.
- Start documentation work from `docs/README.md` and follow `docs/DOCUMENTATION_STANDARD.md`.
- Run `scripts/validate-docs.ps1` after documentation changes.
- Do not report build success as unit-test success while no test project exists.
- If creating a new release, update the version number and changelog first.
- If adding configuration fields, update:
  - `WallboardConfiguration.cs`
  - `WallboardConfigReader.cs`
  - `SettingsForm.cs`
  - `README.md`
  - `docs/user-guide.md`
  - `docs/developer-guide.md`
  - `Data/wallboard*.json`

## DOC-STD-20261002 — Documentation organization

The [documentation map](docs/README.md) now identifies canonical sources and maintenance rules. Existing implementation milestones and pending acceptance are unchanged. Validation and publication are tracked separately for this documentation-only change.
