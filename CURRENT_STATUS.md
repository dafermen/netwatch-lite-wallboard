# Current Status

Last updated: 2026-11-11

## Repository State

- Repository path: `C:\Projects\netwatch-lite-wallboard`
- GitHub repository: `https://github.com/dafermen/netwatch-lite-wallboard`
- Main branch: `main`
- Stage-2 source base / previously synchronized main: `44ec4c5` (icon and organized portable stage). The preceding push CI passed.
- Stage-2 implementation delivered and synchronized at `9f30c6b`; [push CI passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/42384610906). The [rc.4 evaluation prerelease](https://github.com/dafermen/netwatch-lite-wallboard/releases/tag/v0.4.0-rc.4) now publishes that exact binary and checksum; documentation-only updates continue on `main`.
- Current project version: `0.4.0-rc.5`; assembly/file version `0.4.0.0`. Implementation base for this stage: `fa18501`.
- Implementation base reviewed for the current stability work: `6628569`.
- The .NET 10 portable is an evaluation candidate. The old v0.3.0 portable is preserved.

## Portable Archive Policy

- Every newly delivered portable must include a versioned ZIP and SHA-256 sidecar in this project's `artifacts/packages/`, in addition to the extracted folder under `artifacts/portable/`. Preserve previous versions; `-NoZip` is not a final-delivery workflow.
- Use the tested ZIP/checksum for an approved GitHub Release or explicitly authorized evaluation prerelease. Keep generated files out of Git; report a source push separately from binary publication.
- rc.2, rc.3 and rc.4 ZIPs/checksums are already retained locally. No application rebuild or replacement of these packages is needed for this documentation-only policy update.
- On 2026-11-07, the user explicitly authorized public rc.4 prerelease distribution. Release ID `437673152` is published (`draft=false`, `prerelease=true`, not Latest) with exactly the existing ZIP and checksum. The tag targets the binary's source commit `9f30c6b`, not a later documentation commit. Both downloaded assets matched the local originals; current documentation is available on `main`, while embedded package docs remain those of its build commit.

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

### Operational Improvements 2 Through 7

- Implemented bounded general-log rotation (1 MiB plus three backups, 32 KiB entries), structured local support export without operator strings/raw logs, and bounded numeric/event history.
- Added optional memory protection (default off): sustained complete high samples, cooldown, UI/lifecycle exclusions and two reserved attempts per application session. Existing controls are recreated without process killing or profile deletion; working-set estimates are not a leak-fix guarantee.
- Settings now offers session-only profile switching, explicit unsaved-discard confirmation and strict existing-file parsing before changing selection. Startup aliases, precedence, fallback, save and backup paths remain intact. Examples add only optional protection defaults, never modify deployed profiles.
- Reviewed official stable WebView2 SDK `1.0.4314.35` and updated only that NuGet dependency. Reviewed/pinned checkout `v7.0.1`, setup-dotnet `v6.1.0`, upload-artifact `v7.0.2`, download-artifact `v8.0.2` for hosted Windows/Node 24.
- Added a main-only manual prerelease workflow with artifact-only default, draft-first upload/download verification, ProductVersion/full-SHA binding, existing-version refusal and a separate scoped-write publication job. Environment reviewers require GitHub configuration; YAML alone does not enable them.
- Both complete desktop/browser runs passed 88 tests with zero failures/skips, including the final button-size/typed-event regression (`TestResults/dafer_DESKTOP-1DUT53N_2026-11-11_23_24_30_net10.0.trx`). Release-package fixtures and mock GitHub passed 15 checks, including wrong draft identity, download failure and tampering. Actionlint 1.7.12 passed both workflows; NuGet audit found no vulnerable packages. Documentation passed for 34 Markdown files, 288 local links and three images; PowerShell syntax/whitespace checks passed. Known MSB3277 remains.
- The preliminary exact ZIP passed 132 assertions (`artifacts/verification/b53b86f309b847aebf26817562c73383/`). Isolated UI smoke switched demo to a synthetic profile without changing any JSON, displayed the new options, exported valid schema-1 JSON omitting synthetic operator strings/URLs/paths, and closed cleanly. Browser data used a separate folder. A visual oversized profile button was corrected and its height is now asserted. Generated reports remain outside source control.
- User explicitly approved pushing changes and preparing **only a draft** of rc.5 to test the workflow. Required before delivery: final suite, isolated UI smoke, audited dependencies, exact ZIP/guard tests, source synchronization and CI, real draft run and downloaded-asset hash comparison. Do not publish the draft, alter rc.4, or change stable Latest. The 24-hour production test (recommendation 1) was deliberately excluded by the user and remains pending.
- Implementation `abe4c3b` was pushed and [CI passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43155011542). The first [real draft workflow](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43155040809) passed validation and all 132 publication-job package checks, then failed closed on PowerShell 5.1 native quoting of a filtered `gh --jq` expression, before creating/uploading a draft. The fix uses fixed field extraction plus PowerShell comparison. The new `test-release-cli.ps1` passed a real read-only query and all 15 mocks passed under explicit Windows PowerShell `5.1.26100.9711`; both workflows now run that CLI check. Repeat the authorized draft run from the corrected commit.

### Portable Integrity Stage 2

- Implemented pre-runtime validation in the Windows Framework root launcher. The external XML must match its embedded reference; every `app/` file plus `portable-layout.json` is checked for expected length/SHA-256. Extra files, unreadable/redirected paths, unsafe metadata, and XML DTDs fail closed.
- Validator limits the reference to 4 MiB/4,096 files and uses bounded streaming/enumeration rather than loading runtime files into memory. Root startup failures have a separate bounded log; quiet verify/package-check modes do not write operator logs.
- Wallboard JSON/custom profiles/backups, local pages, browser sessions/cookies, existing logs, source/flat layout behavior, aliases and save/fallback semantics are unchanged. No automatic repair, updater, signing, or integrity-disable setting was added.
- Added 30 shared-source integrity tests. Both full desktop runs passed 67 tests, zero failures/skips; the final bounded-enumeration run is `TestResults/dafer_DESKTOP-1DUT53N_2026-11-07_09_52_02_net10.0.trx`.
- The preliminary folder pipeline passed 132 package assertions, including absent/corrupt/locked runtime files, altered manifests/layout, stale DLLs, junction rejection, custom/malformed editable profiles, report overwrite refusal, and blocked child startup. Evidence: `artifacts/verification/79c799d892844b3093e60f6cc33a9aa6/`.
- The preliminary package protected 281 files. Measured quiet checks were about 1.0 s for the complete package and 0.54 s after restoration on this machine; these are not a universal performance guarantee. Timings are in the evidence folder.
- The preliminary exact ZIP also passed all 132 assertions. Evidence: `artifacts/verification/c34c3fef20bd4f9c804692d9b82f3b11/`. An isolated UI smoke opened demo, saved at the root with an exact-original backup, closed, and restarted successfully from another working directory. Default/production JSON remained unchanged, with no JSON under `app/` and a separate browser-data folder. A deliberately damaged layout then showed the recovery dialog, blocked the child, wrote only the separate startup log, and left existing application logs/profiles unchanged; restoring the layout passed verification again.
- Fresh NuGet restore/audit found no vulnerable dependencies in app or tests. Documentation validation passed for 32 Markdown files, 260 local links and three images; PowerShell syntax and Git whitespace checks passed. Only the known MSB3277 warning remains.
- Final rc.4 delivery completed from `9f30c6b`: the exact ZIP passed 132 assertions (`artifacts/verification/9227bc3efdb94b5d85faa22bc171e41c/`), the checksum matched, local/remote SHA matched with a clean tree, and push CI passed. The ZIP is `artifacts/packages/NetWatch-Lite-Wallboard-v0.4.0-rc.4-win-x64.zip`; its SHA-256 is `67bd69247f9aa95827ccf481e7ff337cf2527455a1bd0df065db502629b3d53d`. Earlier packages were preserved. Before the subsequent public prerelease upload, all 132 exact-ZIP checks passed again (`artifacts/verification/f27f5bd5edc64e6bb03471247631f5b3/`); both remote assets were downloaded and verified byte-identical by SHA-256. See [integrity reference](docs/PORTABLE_INTEGRITY.md) and [rc.4 validation](docs/releases/v0.4.0-rc.4.md). Stable/24-hour production acceptance remains pending.

### Application Icon And Publication

- Added an original transparent four-panel display icon using built-in image generation. The master PNG, seven-frame ICO, conversion script, and original prompt/provenance are in `Assets/` and `scripts/build-icon.ps1`.
- The application and root launcher use the same ICO. Main, Settings, and Diagnostics load independent embedded clones and dispose them with each window; no loose icon file or configuration change is needed at runtime.
- Full suite passed 37 tests, zero failures/skips, including desktop/WebView2 regressions. TRX: `TestResults/dafer_DESKTOP-1DUT53N_2026-11-07_09_24_25_net10.0.trx`. The two icon tests passed again after removing a test-analyzer warning (`09_27_01` TRX); only the known MSB3277 remains.
- Visual source-build smoke confirmed the new title-bar icon in all three windows using demo and an isolated browser profile. Windows were closed without saving or changing source JSON.
- The user explicitly requested GitHub synchronization of this icon update and the preceding local portable stage. Publish reviewed source only, not generated packages, test reports, browser data, or backups. Final remote SHA, clean working tree, and push CI must be verified before reporting synchronization.
- Candidate packaging is `0.4.0-rc.3` and preserves rc.2 output. Package checks now include the embedded icon in all ten process probes. See [rc.3 validation](docs/releases/v0.4.0-rc.3.md). Stable acceptance and 24-hour production validation remain pending.
- The preliminary exact ZIP passed all 85 assertions; evidence: `artifacts/verification/44a4931e74504a8e82940498445d1ecd/`. NuGet reported no vulnerable dependencies. Documentation validation passed for 29 Markdown files, 219 local links, and three images; PowerShell syntax, configuration preservation, and Git whitespace checks passed. The final committed build must pass the same exact-ZIP gate.

The first four stability improvements are implemented: .NET 10 migration, combined host/WebView2 memory monitoring, bounded automatic recovery, and an executable test suite. Portable packaging stage 1 is now implemented locally; validation is tracked in [rc.2](docs/releases/v0.4.0-rc.2.md). A 24-hour production-layout run and release acceptance remain pending.

Portable stage 1 on 2026-11-07:

- Output moves to `artifacts/portable/` and `artifacts/packages/`; historical `publish/` folders and ZIPs remain untouched.
- The root executable forwards arguments to `app/`, which includes .NET 10 Desktop Runtime. The root uses Windows .NET Framework 4.x; WebView2 Runtime stays external.
- JSON profiles/local pages remain at the portable root, independently of working directory. Source `Data/`, flat packages, schema, profile aliases/precedence, save/backup semantics, browser sessions, and log locations are preserved.
- Publishing builds in unique staging and refuses existing final outputs rather than deleting them. Package tests use disposable copies and a no-window, read-only runtime/configuration probe.
- All 35 xUnit tests passed with desktop tests enabled (zero failed/skipped), including 11 new path/save/backup/local-page/report regressions. TRX: `TestResults/dafer_DESKTOP-1DUT53N_2026-11-07_09_04_28_net10.0.trx`.
- The complete preliminary folder pipeline passed 75 isolated package assertions, including bundled runtime `10.0.14`, argument quoting, profile aliases/precedence, unchanged JSON, and missing-file detection. Evidence: `artifacts/verification/dbe31993f56f44e88d788c336ec3744f/`. The final ZIP must pass the same checks before promotion.
- The rc.2 ZIP passed the same 75 assertions after extraction; evidence: `artifacts/verification/16e965859dff4170b40227b5d63dd7e1/`. Its root launcher opened the demo profile from a path with spaces and a different working directory. Settings/Diagnostics, profile/root paths, save confirmation, panel reload, and normal close were checked with isolated browser data. Saving created an exact original demo backup at the root; default/production files remained unchanged and no JSON appeared inside `app/`.
- A repeated publish of the same version was refused before building; the existing ZIP hash remained unchanged. Original runtime/Desktop/WebView2 notices are included under `licenses/`. After recording this evidence, the deliverable is regenerated through the same exact-ZIP gate so its documentation is current.
- NuGet audit found no vulnerable packages for either project. Documentation validation passed for 28 Markdown files, 210 local links, and three images. PowerShell syntax and Git whitespace checks passed. The known MSB3277 warning remains. Package smoke is a delivery check, not full release acceptance.
- Per-file startup integrity validation, signing, automatic updates, broad fuzzing/mutation baseline, and long production acceptance remain outside this stage.
- Initially kept local for rc.2; the subsequent icon update explicitly requests GitHub publication of both stages.

Candidate packaging on 2026-11-07:

- Prepared `publish/NetWatch-Lite-Wallboard-WebView2-win-x64-v0.4.0-rc.1.zip` and SHA-256 sidecar with the complete Markdown documentation and licenses.
- The extracted Windows x64 executable started from a path containing spaces with the demo profile and isolated browser data. Settings and Diagnostics opened; version, recovery setting, host/browser memory breakdown, and normal close were verified.
- The package smoke check found the lower Delete command could be clipped at small window sizes. The panel editor now scrolls vertically and uses a constrained-width column; the minimum-size regression test checks that Delete is fully reachable.
- Candidate assessment is recorded in [v0.4.0-rc.1 validation](docs/releases/v0.4.0-rc.1.md). Stable promotion remains withheld pending long-session and full acceptance checks.
- NuGet reported no vulnerable packages for app or tests. Documentation links, version metadata, required package files, excluded backups/logs, and archive checksum were checked.
- The final candidate suite passed all 24 tests on 2026-11-07 after the editor scrolling fix. Documentation validation passed for 26 Markdown files and 186 local links. Remote documentation from `d75a3cc` was integrated while preserving its text and local navigation.

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

Current test automation:

- `tests/NetWatchLite.Wallboard.Tests` targets .NET 10 and is included in the solution and CI.
- Deterministic tests cover configuration, file save/read/backup integration, legacy JSON contracts, numeric invariants, distinct-process memory accounting, bounded history, and retry policy.
- Opt-in desktop tests use isolated WebView2 profiles for renderer-crash recovery, Settings layout, and repeated panel disposal/resource measurements.
- `scripts/test-soak.ps1 -Minutes 1440` provides a repeatable neutral-fixture resource run with CSV/TRX output under ignored `TestResults/`.
- A broad fuzzing harness, mutation baseline, full application end-to-end coverage, and 24-hour production-layout evidence remain pending.

Implementation details:

- `global.json`, app, tests, and CI select .NET 10. WebView2 stayed at `1.0.3351.48` during migration and was explicitly reviewed/upgraded to `1.0.4314.35` in the operational-safety stage.
- `automaticRecoveryEnabled` defaults to true when absent from old JSON and is exposed in Settings/import/export/diagnostics and all example profiles.
- Recovery delays are 5/15/30 seconds, with three attempts retained across control recreation. A successful page followed by a healthy minute or operator intervention restores the budget.
- Memory sampling uses only the host and distinct process IDs from the active WebView2 environment. Diagnostics shows partial samples, process counts, peak, and recent history; history is capped at 1,440 samples.
- Pending recovery is canceled on page disposal, low-power suspension, and close. Initialization is canceled at close and bounded by a timeout.

Validation performed on 2026-11-06:

- .NET 10 SDK `10.0.403` and Desktop Runtime `10.0.14` were installed beside the existing .NET 8 installation. `dotnet --version` selects `10.0.403` from the repository. The Microsoft installer completed with code `3010` (Windows restart pending).
- Release build succeeded with zero errors. The known `MSB3277` WebView2/WPF warning remains; no new code warning remains.
- All 24 tests passed with desktop tests enabled: 20 deterministic/configuration tests and four isolated UI/browser tests.
- After the side-by-side installation, 23 tests excluding the already-measured soak case were rerun successfully with the normal `dotnet` command and system runtime `10.0.14`.
- Real tests covered renderer failure, two-panel shared-browser failure with a failed first environment factory call, recovery-control visibility at minimum Settings size, and two minutes of repeated disposal/recreation.
- The reviewed short resource run completed 81 cycles: approximately 392.0 MB at the start, 441.7 MB peak, 438.7 MB at the end, and at most five browser processes. These are warm-up measurements for a neutral single-panel fixture, not 24-hour production stability evidence.
- NuGet reported no vulnerable packages for either the app or test project from the configured source.
- Documentation validation passed for 25 Markdown files, 176 local links, and three images with alternative text. PowerShell script syntax and Git whitespace checks passed.
- Windows x64 publish succeeded in a separate validation folder with .NET 10 runtime requirements, all three JSON profiles, and no backup JSON. The existing portable/ZIP was not replaced.
- CSV and TRX evidence is under ignored `TestResults/`. The 24-hour neutral-fixture run and a separate long production-layout run remain pending before release.

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
4. Expand automated coverage to broader layout, malformed profile names, and URL resolution.
5. Establish property tests and a Stryker.NET mutation baseline after unit coverage is meaningful.
6. Automate selected integration and portable end-to-end checks.
7. Investigate the known `MSB3277` `WindowsBase` warning and decide whether it is worth suppressing or resolving.

## Handoff Notes For Future Codex Sessions

- Start by reading `AGENTS.md`.
- Do not assume scraping exists; it was removed.
- Do not edit generated artifacts under `publish/`, `artifacts/`, `bin/`, or `obj/`. Never overwrite an existing version folder that may contain operator configuration.
- Read `docs/TESTING.md` before release-level work.
- Start documentation work from `docs/README.md` and follow `docs/DOCUMENTATION_STANDARD.md`.
- Run `scripts/validate-docs.ps1` after documentation changes.
- Report build, standard tests, optional desktop tests, and long-session evidence separately.
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
