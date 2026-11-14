# Current Status

Last updated: 2026-11-14

## Repository State

- Repository path: `C:\Projects\netwatch-lite-wallboard`
- GitHub repository: `https://github.com/dafermen/netwatch-lite-wallboard`
- Main branch: `main`
- Stage-2 source base / previously synchronized main: `44ec4c5` (icon and organized portable stage). The preceding push CI passed.
- Stage-2 implementation delivered and synchronized at `9f30c6b`; [push CI passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/42384610906). The [rc.4 evaluation prerelease](https://github.com/dafermen/netwatch-lite-wallboard/releases/tag/v0.4.0-rc.4) now publishes that exact binary and checksum; documentation-only updates continue on `main`.
- Current project version: `0.4.0-rc.6`; assembly/file version `0.4.0.0`. The previous rc.5 draft retains its original version/source/bytes.
- rc.6 binary source: `792b85310c6eb34d9490e116fd0d6e89e55a5165`. Its verified local ZIP and GitHub draft have identical hashes; later handoff documentation does not rebuild the package.
- Operational implementation plus native-CLI fix is delivered at `62a50977f2402e3d2eced4866d0fb07493bea4c6`; [CI passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43155737818) and the [real manual draft workflow passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43155741648). The rc.5 draft targets that exact binary source; subsequent handoff documentation does not rebuild or replace its assets.
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

### rc.6 Draft Delivery (2026-11-14)

- The user explicitly authorized a fresh portable with ZIP/checksum, source synchronization and **only a GitHub draft** for `v0.4.0-rc.6`. This supersedes the earlier no-delivery scope for the local work below. No public release, rc.5 replacement, stable promotion or Latest change is authorized.
- Includes the Inicio/Wallboard feature and the prior unreleased dialog/profile/diagnostics/testing/draft-promotion improvements. Existing Data JSON and older ZIP/checksum hashes were captured before packaging and must remain unchanged.
- Private draft delivery is complete: clean identified source, full desktop suite, guarded build/audit, exact-ZIP checks, source push/CI and guarded Draft upload/download comparison. The final packaged visual pass could not be completed because Computer Use failed to initialize; it remains an explicit release gate, not a passed test. See [rc.6 validation](docs/releases/v0.4.0-rc.6.md).
- Pre-package rc.6 validation passed: full suite 126/126, zero skips (`TestResults/dafer_DESKTOP-1DUT53N_2026-11-14_12_25_58_net10.0.trx`), guarded solution build zero warnings/errors, audit with no vulnerable app/test packages, 15 publisher and 25 promotion mocks under PowerShell 5.1, script parsing and documentation/whitespace checks.
- Binary source `792b85310c6eb34d9490e116fd0d6e89e55a5165` was pushed and [CI passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43621450059). The clean-source local portable includes .NET Desktop Runtime 10.0.14. Its exact ZIP passed 132 assertions (`artifacts/verification/26d5d50a5a834b82afb3f49bbb335d7e/`) and is retained with its sidecar and extracted folder. ZIP SHA-256: `a58b0c97936e1409a288bc2a4194698f027ba916bf4a22ddbffbf3379eefeb2f`. Existing Data and all older ZIP/sidecar hashes are unchanged.
- Final extracted-package visual acceptance is pending: the isolated root launcher started the native app/window, but both Computer Use runtimes failed to initialize their kernel assets, including after reset. No visual pass is claimed; only the owned test instance was closed. Automated local UI/browser tests and exact-ZIP process tests passed independently. Keep this limitation distinct from an application defect and do not waive production/24-hour/device gates.
- Guarded local Draft publisher repeated all 132 exact-ZIP assertions (`artifacts/verification/e41ebfd1b9954379a36d0bfd8f2858fe/`), created release ID `445470528` and downloaded/compared both assets (`release-923dbd5a1e3c4dc4a8bad8dc42f3394f/`). [Owner-access rc.6 draft](https://github.com/dafermen/netwatch-lite-wallboard/releases/tag/untagged-dc73f63f3b66cf8a1ba6): `draft=true`, `prerelease=true`, original binary source above, no public publication/Latest change. ZIP: 59,243,763 bytes, SHA-256 above; sidecar: 115 bytes, SHA-256 `40ef06c5ee668390c23c880b47322e3370254f7488c8e9b228fca927b05d7063`. A draft can exist without a public Git tag; GitHub's temporary URL changed when notes were edited, while release ID, target and asset IDs/digests were rechecked unchanged.
- The final handoff is documentation-only and must be synchronized without repackaging rc.6. Current docs on main record completed delivery and remaining checks; embedded docs preserve the original build snapshot. Public rc.4, the rc.5 draft and stable v0.2.3 are not promoted or replaced.
- Production/24-hour testing, real monitor-DPI/other-device acceptance and broader lifecycle mutation/E2E coverage remain pending. A private evaluation draft does not satisfy stable/public release gates.

### Inicio And Wallboard Views (2026-11-14)

- Implemented an opt-in Inicio/Wallboard selector and Settings fields `homePageEnabled`, `homePageUrl`, and `startupView`. Legacy configurations retain the existing Wallboard startup and toolbar. Existing `Data/*.json` files remain byte-identical; the neutral new-field example is `tests/fixtures/wallboard-home.json`. See [ADR 0007](docs/adr/0007-exclusive-home-and-wallboard-views.md).
- Inicio displays one chrome-free browser. Switching views disposes inactive controls, cancels in-flight work and rejects queued work from older view generations under the render lock. Wallboard rotation, refresh, scheduled restart and automatic recovery cannot wake inactive panels. Home navigation failures offer a native Retry action without silently returning to Wallboard.
- The shared WebView2 environment, cookies, profile/save paths, layout/page selection and session memory-protection budget are preserved. Low-power suspension/restoration and closing handle both views. Ordinary Settings saves preserve the active valid view; startup selection applies at launch or explicit profile switching. Home URLs stay out of the unchanged safe support-report allowlist.
- Added 20 configuration/property/browser test cases, including legacy defaults, invalid URLs, backup preservation, safe-report privacy, repeated transitions, stale queued restart cancellation, cookies, page/layout retention, minimize/restore, close during transition and local-page retry. The existing minimum-size Settings regression exposed clipped editor commands after the new group was added; command spacing was compacted and the original reachability test now passes.
- Final complete suite passed **126 tests, zero failures/skips**, with real desktop/WebView2 tests enabled. TRX: `TestResults/dafer_DESKTOP-1DUT53N_2026-11-14_04_09_27_net10.0.trx`. The focused 22-case mode/theme/layout run also passed (`04_02_08` TRX). Final guarded Release build passed with zero warnings/errors. This is short isolated-fixture evidence, not a 24-hour production or whole-app mutation result.
- Manual UI smoke in ignored `TestResults/mode-smoke-20261114/` verified full-page Inicio, two-panel Wallboard, Settings URL/startup controls, save preserving the active Wallboard view and loading the changed URL in Inicio. Its browser data and configuration were isolated; all test windows were closed. SHA-256 checks confirmed all four existing Data JSON files, including the backup, were unchanged.
- Windows PowerShell 5.1 publisher guards passed 15 cases and draft-promotion guards passed 25 cases using fixtures/mocks only. Fresh NuGet audit found no vulnerable application/test packages. Documentation validation passed for 36 Markdown files, 314 local links and three images; Git whitespace checks passed.
- README, user/configuration/developer guides, architecture, operations, testing, changelog, documentation navigation and agent handoff instructions describe the new lifecycle and compatibility boundaries. Earlier unreleased work below is preserved. Current local HEAD is `a7445f6`; these changes remain uncommitted and no new portable, ZIP, push, hosted workflow or publication was performed. The existing rc.5 draft and package retain their original source/version/bytes.
- Pending: a long run with the actual company URL and real deployment layout, real monitor-DPI/other-device acceptance, and a separately authorized fresh-version portable/ZIP/checksum and GitHub delivery. Broader fuzz/mutation/E2E coverage remains a future improvement; the earlier scoped mutation score does not cover the new view lifecycle.

### Unreleased Improvements 2 Through 7 (2026-11-12)

- User approved dialog themes/accessibility, editing-profile clarity/folder command, effective browser diagnostics, WindowsBase warning resolution, deeper tests and promotion of existing verified drafts. This is local unreleased source work, not a request to push, create a portable or publish rc.5. Project/old binary version remains rc.5; the existing draft/ZIP must not be rebuilt or replaced under that identity.
- Implemented shared native themes with isolated Settings preview, readable accent/disabled text and DPI-aware dialogs. Reorganized Wallboard fields; display captured editing profile/save path, separate pending selection and block ambiguous Save. Folder opening is non-writing and argument-list based. No configuration fields, persisted profile selection, CLI precedence or browser lifecycle changes.
- Diagnostics refresh reads actual environment folder/Runtime version and live recovery counts/state; requested/effective/unavailable values remain distinct. Raw values never enter the unchanged safe support-report allowlist.
- Resolved MSB3277 by removing only the SDK-imported unused WPF adapter before assembly resolution in WinForms-only projects. Release build passed with zero warnings/errors; no suppression or cache edits.
- Added three FsCheck properties (1,300 generated cases/run), 1,000 seeded bounded JSON mutations with unchanged-file/sentinel assertions, dialog theme/scaling/profile/keyboard tests and a real environment-folder override test. Added precise policy boundaries/generation assertions after the first mutation run revealed coverage gaps.
- Scoped Stryker.NET 5.1.0 baseline passed at 96.92% (63/65 executable mutants detected) on retry/memory policies, above the 80% gate. Final evidence: `TestResults/mutation-448dfaf1c5434c49a215275c54c1ab8a/`. Two survivors and compilation/filter exclusions remain visible for future review; this is not a whole-app score. Manual Deep Logic Tests workflow added; no hosted run is implied.
- Existing-draft promotion defaults to verify-only and requires the previously approved hash/source. It downloads and tests exact existing bytes, rechecks release/assets/tag/source and uses explicit tag-specific approval before limited publication flags. No rebuild/upload/overwrite/delete. 25 simulated guard cases passed under Windows PowerShell 5.1.
- Real verify-only execution downloaded rc.5 and passed 132 package assertions, leaving `published=false`. Package evidence: `artifacts/verification/e205b4e0ca2c4d42afc2dfb4d91e37c8/`. The original hash `2488c0690c61cbb4a6af4832ec330add612247467c82c581ca436479f7b03322` and source `62a50977f2402e3d2eced4866d0fb07493bea4c6` were retained.
- Final complete suite passed **106 tests, zero failures/skips**, including all seven desktop/browser cases and the short neutral resource fixture. TRX: `TestResults/dafer_DESKTOP-1DUT53N_2026-11-12_06_56_47_net10.0.trx`. Four final dialog/profile/runtime regressions also passed separately (`06_56_07` TRX). The final guarded Release build (`-warnaserror:MSB3277`) passed with zero warnings/errors.
- Visual source-build smoke opened demo with isolated browser data, checked dark dropdown/disabled-command contrast, effective profile/version and multiline recovery diagnostics, opened the correct Data folder, and closed all test windows without saving. Nine three-theme/synthetic-scaling bitmaps are under ignored `TestResults/ui-final-20261112/`. Real monitor-DPI/other-device acceptance is still pending.
- Documentation validation passed: 35 Markdown files, 300 local links and three images. All PowerShell scripts parsed; Actionlint 1.7.12 accepted all four workflows. Fresh solution NuGet audit found no vulnerable application/test packages. The original 15 publisher guards and 25 promotion guards passed under Windows PowerShell 5.1. Data has no tracked changes; prior portable history was not overwritten. Final remote read confirmed rc.5 remains draft/prerelease with source `62a5097`.
- Work remains local and uncommitted; no new portable, ZIP, source push, hosted workflow execution or public promotion was performed. Future delivery must use a fresh version and the usual ZIP/checksum gates. The 24-hour production run, real device/DPI acceptance, broader fuzz/mutation/E2E coverage and optional environment-reviewer configuration remain pending.

### Operational Improvements 2 Through 7

- Implemented bounded general-log rotation (1 MiB plus three backups, 32 KiB entries), structured local support export without operator strings/raw logs, and bounded numeric/event history.
- Added optional memory protection (default off): sustained complete high samples, cooldown, UI/lifecycle exclusions and two reserved attempts per application session. Existing controls are recreated without process killing or profile deletion; working-set estimates are not a leak-fix guarantee.
- Settings now offers session-only profile switching, explicit unsaved-discard confirmation and strict existing-file parsing before changing selection. Startup aliases, precedence, fallback, save and backup paths remain intact. Examples add only optional protection defaults, never modify deployed profiles.
- Reviewed official stable WebView2 SDK `1.0.4314.35` and updated only that NuGet dependency. Reviewed/pinned checkout `v7.0.1`, setup-dotnet `v6.1.0`, upload-artifact `v7.0.2`, download-artifact `v8.0.2` for hosted Windows/Node 24.
- Added a main-only manual prerelease workflow with artifact-only default, draft-first upload/download verification, ProductVersion/full-SHA binding, existing-version refusal and a separate scoped-write publication job. Environment reviewers require GitHub configuration; YAML alone does not enable them.
- Both complete desktop/browser runs passed 88 tests with zero failures/skips, including the final button-size/typed-event regression (`TestResults/dafer_DESKTOP-1DUT53N_2026-11-11_23_24_30_net10.0.trx`). Release-package fixtures and mock GitHub passed 15 checks, including wrong draft identity, download failure and tampering. Actionlint 1.7.12 passed both workflows; NuGet audit found no vulnerable packages. Documentation passed for 34 Markdown files, 288 local links and three images; PowerShell syntax/whitespace checks passed. Known MSB3277 remains.
- The preliminary exact ZIP passed 132 assertions (`artifacts/verification/b53b86f309b847aebf26817562c73383/`). Isolated UI smoke switched demo to a synthetic profile without changing any JSON, displayed the new options, exported valid schema-1 JSON omitting synthetic operator strings/URLs/paths, and closed cleanly. Browser data used a separate folder. A visual oversized profile button was corrected and its height is now asserted. Generated reports remain outside source control.
- User explicitly approved pushing changes and preparing **only a draft** of rc.5. All implementation/delivery gates for recommendations 2 through 7 are complete: full local suite, isolated UI smoke, audit, exact-ZIP/guard tests, synchronized implementation source/CI and real draft/download verification. Do not publish the draft, alter rc.4, or change stable Latest. The 24-hour production test (recommendation 1) was deliberately excluded and remains pending, along with broad fuzzing/mutation and other-machine acceptance.
- Implementation `abe4c3b` was pushed and [CI passed](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43155011542). The first [real draft workflow](https://github.com/dafermen/netwatch-lite-wallboard/actions/runs/43155040809) passed validation and all 132 publication-job package checks, then failed closed on PowerShell 5.1 native quoting of a filtered `gh --jq` expression, before creating/uploading a draft. The fix uses fixed field extraction plus PowerShell comparison. The new `test-release-cli.ps1` passed a real read-only query and all 15 mocks passed under explicit Windows PowerShell `5.1.26100.9711`; both workflows now run that CLI check. The completed corrected run is recorded next; do not repeat creation of the existing draft.
- The corrected run completed successfully from `62a5097` without public publication. Draft release ID `442502596`, tag name `v0.4.0-rc.5`, `draft=true`, `prerelease=true`; [owner-access draft](https://github.com/dafermen/netwatch-lite-wallboard/releases/tag/untagged-c8fa836fac2894911f60). An unpublished draft need not have a public Git tag; its target commit and ProductVersion are the identity evidence. Stable Latest remains `v0.2.3`; public rc.4 and all old ZIP hashes are unchanged.
- Both draft assets were downloaded and matched their GitHub digests/sizes. ZIP: 59,286,785 bytes, SHA-256 `2488c0690c61cbb4a6af4832ec330add612247467c82c581ca436479f7b03322`; checksum asset: 115 bytes, SHA-256 `f4cfc96dddd5b1c3bf2c87e9df761003f0952da76606b577fa94b41856e98221`. That exact downloaded ZIP passed all 132 local assertions (`artifacts/verification/482fd0cd64384c3fb240e458155c76e4/`) and was promoted without overwriting to `artifacts/packages/NetWatch-Lite-Wallboard-v0.4.0-rc.5-win-x64.zip` plus sidecar and the matching extracted folder under `artifacts/portable/`.
- Final downloaded portable opened demo with isolated browser data. Settings showed the corrected fixed-height switch command and default-off protection. Closing without saving left JSON unchanged. Only a documentation handoff follows this binary build; do not regenerate or replace rc.5 just to update embedded docs.

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

1. Complete the neutral-fixture and separate real-production-layout 24-hour resource runs; record memory/process/recovery trends before stable approval.
2. Validate Windows 10/11 devices, real 125/150/200% monitor DPI changes, keyboard/screen-reader acceptance and TV fullscreen behavior.
3. Expand scoped mutation to normalization/save/report boundaries and add coverage-guided/longer fuzzing plus broader packaged UI end-to-end tests.
4. Prepare a fresh versioned portable/ZIP/checksum and synchronize source only on a separate user request; do not replace rc.5.
5. Configure GitHub environment reviewers if the maintainer authorizes it; only later promote an existing draft with explicit approval. Hosted tests of new workflows require source synchronization.
6. Consider a panel-health overview or compact About dialog; do not reintroduce page-content scraping/alarms.

Profile switching/folder access, scoped properties/fuzz/mutation and WindowsBase warning resolution are now implemented; do not recommend them again as absent. Remembering a profile across restarts remains intentionally deferred because it adds a persistence contract.

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
