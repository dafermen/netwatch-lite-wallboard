# Changelog

All notable changes to NetWatch Lite Wallboard are documented here.

## Unreleased

## v0.4.0-rc.5 - 2026-11-11

Evaluation candidate; only a workflow draft is authorized, not public/stable promotion. Long production acceptance remains pending.

- Bounded the general error log to 1 MiB plus three backups with capped entries and oversized legacy-tail retention.
- Added allowlisted support JSON export with bounded memory/events and no operator names/URLs/paths, cookies, configuration or raw logs; existing files are never overwritten.
- Added opt-in sustained-memory protection, cooldown and a two-attempt session budget, preserving sessions and using existing serialized control recreation.
- Added session-only profile switching in Settings with unsaved-discard confirmation and strict selection; startup/save/fallback contracts remain compatible.
- Updated WebView2 to stable `1.0.4314.35` and reviewed/pinned official GitHub actions for hosted Windows/Node 24.
- Added manual artifact-only/draft/publish workflow, exact source/ZIP/checksum binding, refusal to replace existing releases, scoped write permissions and download verification before public publication.
- Added deterministic operational-safety, desktop layout/profile-discard and simulated-publisher regression tests; retained portable startup-integrity gates and versioned ZIP history.

## v0.4.0-rc.4 - 2026-11-07

Evaluation candidate for portable integrity stage 2; stable acceptance and long production runs remain pending.

- Embedded a finalized path/length/SHA-256 manifest in the independent Framework launcher, requiring an identical external manifest before protected file checks and .NET 10 startup.
- Protected all `app/` files and `portable-layout.json`; rejected missing/changed/unreadable/extra files, malformed metadata/paths, XML DTDs, and redirected payloads with bounded enumeration.
- Kept editable profiles/backups, local pages, browser sessions, and existing logs outside hashing and repair; profile arguments and fallback/save behavior remain unchanged.
- Added quiet verification with new-file-only reports, clear normal startup errors and a bounded separate diagnostic log. No automatic repair, signing, updater, or integrity-disable setting was introduced.
- Added shared-validator xUnit coverage and real-launcher package tests for corruption, locking, junctions, report overwrite refusal, mutable data, and blocked child startup; included CI diagnostics/timing evidence.
- Updated operator/developer/security/deployment documentation, ADR 0004, and handoff guidance. Preserved older portable packages.

## v0.4.0-rc.3 - 2026-11-07

Evaluation candidate; the existing stable-release and long-session gates remain open.

- Added an original generated dashboard-display icon, transparent PNG master, reproducible multi-resolution ICO conversion script, and recorded generation prompt.
- Applied the same embedded icon to the main, Settings, and Diagnostics windows, the application executable, and the root portable launcher.
- Gave each native window its own disposable icon clone, without external asset lookup or changes to configuration/profiles.
- Added icon frame/lifetime regressions and embedded-icon package checks. Preserved native message-box status symbols and website favicons.
- Prepared the organized .NET 10 portable and icon changes for GitHub synchronization; generated packages remain excluded from source control.

## v0.4.0-rc.2 - 2026-11-07

Evaluation candidate for portable packaging stage 1. Stable acceptance and long production runs remain pending.

- Organized versioned output under `artifacts/portable/` and `artifacts/packages/`, preserving historical `publish/` packages and refusing to overwrite existing version outputs.
- Included the .NET 10 Desktop Runtime in `app/`, without trimming or single-file extraction. WebView2 Runtime remains an external prerequisite.
- Added a lightweight root Windows Framework launcher that preserves quoted arguments and all environment-profile aliases; startup per-file integrity validation remains deferred.
- Kept editable profiles/local pages at the portable root, with legacy flat/development compatibility and unchanged LocalAppData sessions/logs.
- Added xUnit path/save/backup/local-page regressions and isolated real-process tests of the exact ZIP, bundled-runtime use, profile selection, argument forwarding, unchanged JSON, and missing required-file detection.
- Updated CI to build/test the ZIP and upload package-check evidence, plus user/developer/deployment guidance and ADR 0003.

## v0.4.0-rc.1 - 2026-11-07

Evaluation candidate. Long-running production validation and the full acceptance checklist remain pending; this is not an approved stable release.

- Migrated the Windows application, SDK selection, tests, and CI to .NET 10 LTS; updated Desktop Runtime requirements while retaining compatibility with existing JSON profiles.
- Expanded memory monitoring to the host plus distinct processes in the active WebView2 environment, with partial-sample indication, peak use, and bounded history in Diagnostics.
- Added configurable automatic panel recovery with 5/15/30-second delays, a persistent three-attempt budget, and disposal/close cancellation.
- Added xUnit coverage for configuration, file integration, legacy contracts, numeric invariants, memory accounting, and recovery, plus opt-in real-WebView2 crash/resource tests and a timed soak-test script.
- Added complete Markdown documentation, third-party notices, package-content checks, and SHA-256 output to portable packaging. Publishing now stops if `dotnet publish` fails.
- Made the panel editor scrollable so Delete and other lower commands remain reachable at small Settings window sizes; added a regression assertion.

- Standardized documentation navigation with an audience-based index and previous/next reading paths.
- Added a documentation standard tailored to the native .NET desktop architecture.
- Added automated documentation validation for local links, image alt text, navigation blocks, index coverage, headings, and duplicated paths.
- Added documentation validation to GitHub Actions.
- Added focused technical documentation for architecture, configuration, development, testing, deployment, operations, security, and troubleshooting.
- Added a 13-area release quality policy and reusable release-readiness checklist.
- Added Architecture Decision Records and documented the no-scraping product boundary.
- Added GitHub CI for release builds, test-project discovery, NuGet vulnerability review, portable publication, and package-content validation.
- Added structured bug, feature, release-readiness, and pull request templates.
- Added monthly Dependabot checks for NuGet and GitHub Actions.
- Added contribution guidance, test-project organization guidance, and third-party license inventory.
- Documented why React-specific `package.json`, `.env.example`, and API documentation do not apply to the current .NET desktop architecture.

## v0.3.0 - 2026-07-01

- Added repository handoff files for future Codex sessions: `AGENTS.md` and `CURRENT_STATUS.md`.
- Removed the DOM scraping/monitoring subsystem, including monitoring JSON models, rule editor, selector validation, scraping controls, alarm preview, bundled scraping test page, and related documentation.
- Simplified settings, diagnostics, configuration, and WebView panel runtime around page display, refresh, restart, rotation, profiles, and low-power mode.
- Improved settings window layout so top-level fields, panel details, panel actions, and footer actions are easier to read.
- Split settings footer actions into utility actions on the left and primary Save/Cancel actions on the right.
- Added scheduled visible-panel restart for long-running sessions.
- Added configurable fullscreen startup and close confirmation for kiosk-style use.
- Added memory monitor with configurable warning threshold and restart recommendation.
- Added top-bar auto-hide for TV wallboard mode.
- Added native shell themes: dark, light, and high contrast.
- Clarified global versus per-panel actions by renaming the top-bar refresh action to **Refresh All** and adding panel-specific tooltips.
- Added a dedicated end-user guide in `docs/user-guide.md`.
- Rewrote the developer guide so a junior developer can understand runtime flow, configuration loading, panel lifecycle, low-power mode, restart behavior, and extension points.
- Reworked the GitHub README as the main project entry point with current scope, setup, configuration, publishing, and documentation links.
- Changed the default configuration so automatic rotation starts disabled.
- Changed the settings keyboard shortcut from `Ctrl+,` to `C`.
- Allowed `file:///` panel URLs so trusted local HTML pages can be loaded by the wallboard.
- Made `ESC` reliable from hosted WebView2 content and close the app when fullscreen is not active.
- Added a **Diagnostics** window with runtime paths, version, configuration summary, and panel details.

## v0.2.3 - 2026-06-03

- Expanded the README with a complete explanation of configuration, panel fields, build steps, and operational notes.
- Rewrote the developer guide with detailed runtime flow, configuration normalization, settings save behavior, WebView2 hosting, and extension points.
- Added detailed source comments across the configuration models, settings editor, configuration reader, main form, WebView panel control, and project file.
- Replaced non-ASCII UI shortcut/refresh symbols in source with ASCII text for easier cross-editor readability.
- Added centralized unexpected-error logging under `%LOCALAPPDATA%\NetWatchLite\WallboardLogs`.
- Hardened WinForms async event handlers, panel rendering, settings save events, and WebView2 process failure handling.
- Added settings-window import/export buttons for moving or backing up `wallboard.json`.
- Expanded README positioning with operational value, business benefits, and TSG-focused production support messaging.

## v0.2.2 - 2026-05-08

- Made **Save Changes** apply the selected panel editor values before writing `wallboard.json`.
- Renamed panel editor buttons to clearer labels: **New Panel**, **Add Panel**, and **Apply**.
- Added status text for unsaved panel/settings edits.
- Added save verification that reads the JSON back after writing before showing the success message.
- Allowed **Save Changes** to add a completed new panel when the editor has values but no selected row.
- Updated README and developer documentation for the more intuitive save flow.

## v0.2.1 - 2026-05-08

- Increased the top-bar Settings button width so the label is fully visible.
- Made the settings window resizable and maximizable like a standard Windows dialog.
- Updated the settings window to a lighter slate theme for better contrast.
- Styled the panel grid headers, rows, alternating rows, and selection colors for readable text.
- Updated README and developer documentation for the settings window polish.

## v0.2.0 - 2026-05-08

- Added a visual settings window for managing `wallboard.json` inside the app.
- Added panel CRUD actions for adding, updating, duplicating, deleting, and reordering wallboard panels.
- Added editable wallboard title, default layout, rotation toggle, and rotation interval controls.
- Added a top-bar Settings button and `Ctrl+,` keyboard shortcut.
- Added JSON save support with a local `wallboard.backup.json` backup before overwriting.
- Reloaded the wallboard automatically after saving settings.
- Updated README and developer documentation for the visual configuration workflow.

## v0.1.1 - 2026-05-07

- Removed the bundled local sample wallboard HTML.
- Replaced all example configuration values with neutral public placeholders.
- Updated README and developer documentation for public repository use.
- Added a Windows executable icon for portable builds.
- Documented MIT license usage.
- Added 1, 3, 6, and 8 panel layouts alongside the existing 2 and 4 panel layouts.
- Expanded the neutral sample configuration to eight panels for layout and rotation testing.
- Removed repository-hosted portable ZIP files to avoid incorrect small HTML downloads from GitHub.
- Added sanitized README screenshots for 1, 2, and 4 panel wallboard layouts.

## v0.1.0 - 2026-05-07

- Initial standalone Windows WebView2 wallboard release.
- Added JSON-driven panel configuration.
- Added 2-panel and 4-panel layouts.
- Added automatic page rotation.
- Added independent panel refresh timers.
- Added fullscreen and keyboard shortcuts.
- Added neutral example panel configuration.
- Added programmer documentation.
