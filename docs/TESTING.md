# Testing Strategy And Release Gates

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

Every deployable release candidate must address all 13 quality areas in this document.

For each area, record one result:

- `Passed`: evidence exists and acceptance criteria were met.
- `Failed`: a release blocker exists.
- `Not applicable`: allowed only with a written reason tied to the change scope.

`Not run` is not an acceptable release result. A release must not be published while any applicable area is failed or untested.

## Current Automation Level

The .NET 10 solution includes `tests/NetWatchLite.Wallboard.Tests`, an xUnit project. CI builds both projects and executes the deterministic configuration, file integration, contract, memory-accounting, and recovery-policy tests. Property-style checks exercise numeric bounds with fixed-seed inputs.

Four optional desktop tests require an interactive Windows session: isolated renderer-crash recovery, shared-browser recovery in the main form with a deliberately failed environment factory, Settings layout at its minimum size, and repeated real-WebView2 panel creation/disposal with resource measurements. They are skipped unless `NETWATCH_WEBVIEW2_TESTS=1`. CI does not claim that skipped desktop tests passed. Mutation testing and broader fuzzing remain pending; automated tests do not replace operator acceptance or the full release checklist.

## 1. Acceptance Testing

Confirm the release solves its intended user scenarios.

Minimum checks:

- Start the application with the default configuration.
- Display one, two, four, six, and eight panel layouts where practical.
- Open Settings, change a safe value, save, and confirm the wallboard reloads it.
- Verify Refresh, Restart, Refresh All, and Restart All.
- Verify fullscreen, exit confirmation, and top-bar auto-hide when enabled.
- Confirm Diagnostics reports the active profile and configuration path.

Evidence: completed release checklist with Windows version, app version, and tester.

## 2. Unit Testing

Unit tests should cover deterministic logic without opening the UI.

Priority targets:

- Configuration defaults and normalization.
- Supported layout normalization and grid dimensions.
- Environment name and configuration path selection.
- Theme name normalization.
- URL resolution and refresh URI behavior.

Current coverage includes defaults, legacy configuration, invalid inputs, timer-safe bounds, memory process-ID deduplication, bounded history, and retry budgets. Layout, environment selection, and broader URL-resolution coverage remain next steps.

## 3. Property And Invariant Testing

Verify rules that must hold over many inputs:

- Normalized layouts are always one of `1, 2, 3, 4, 6, 8`.
- Refresh and rotation intervals are positive after normalization.
- Rotation and restart intervals are never zero or negative after normalization.
- A rendered page never contains more controls than the active layout.
- Rebuilding panels leaves no old refresh timers running.
- Configuration save/read preserves normalized values.

Property-based automation is preferred for pure normalization logic. Until available, use boundary-value tables.

## 4. Mutation Testing

Mutation testing checks whether tests detect intentional logic defects.

Use Stryker.NET after a meaningful unit test suite exists. Focus on:

- Bounds and fallback values.
- Boolean feature switches.
- Layout and page calculations.
- Save verification and error paths.

Release rule: required for changes to well-covered shared logic once the mutation baseline is established. Before that milestone, mark `Not applicable` with the reason `automated test baseline not yet established`, never `Passed`.

## 5. Fuzzing

Exercise malformed and unexpected inputs:

- Empty, truncated, deeply nested, and invalid JSON.
- Unknown configuration fields.
- Empty names and URLs.
- Extremely large and negative numbers.
- Invalid environment profile names.
- Unusual but valid URL encodings.

The application must fail with a useful message or normalize safely. It must not hang, overwrite an unrelated file, or expose sensitive data.

## 6. Integration Testing

Validate boundaries between components:

- Configuration load, normalize, save, backup, and read-back verification.
- Settings changes followed by main-window reload.
- Shared WebView2 environment with multiple panels.
- Error logging and Diagnostics paths.
- Low-power minimize/restore and panel recreation.

Use temporary, non-production configurations and neutral test URLs.

## 7. Contract Testing

The primary contract is `wallboard.json`.

Verify:

- The current release loads configuration from the previous supported release.
- Missing optional fields receive defaults.
- Unknown fields do not break startup.
- Example profile files match the documented schema.
- The portable package contains the expected JSON profiles.

If an external service becomes integrated later, add explicit HTTP/API contract tests. There is no application API today.

## 8. End-To-End Testing

Test the packaged application as an operator would use it:

1. Extract the portable ZIP to a clean folder.
2. Start `NetWatch-Lite-Wallboard.exe`.
3. Load default, demo, and production profiles.
4. Exercise settings, layouts, refresh, restart, rotation, and diagnostics.
5. Close and reopen the app to confirm persisted configuration.
6. Confirm no development-only paths are required.

At least one end-to-end pass must use the exact package intended for distribution.

## 9. Regression Testing

Retest previously fragile behavior:

- WebView2 controls are disposed on rerender, restart, minimize, and close.
- ESC works when focus is inside WebView2.
- Settings buttons remain visible at common resolutions.
- Per-panel and global actions remain clearly labeled.
- `wallboard.backup.json` is not included in portable output.
- Scraping, monitoring, and alarm options do not reappear.

Add every fixed production defect to this list or to an automated regression suite.

## 10. Security Testing

Minimum checks:

- Review direct and transitive NuGet packages for known vulnerabilities.
- Confirm no secrets exist in source, JSON profiles, logs, or package output.
- Confirm invalid profile names cannot escape configuration lookup.
- Confirm local paths resolve only as documented.
- Confirm WebView2 developer tools and browser accelerators remain disabled in kiosk panels.
- Review changed dependencies and third-party license impact.

Follow [SECURITY.md](SECURITY.md). Do not place real credentials in test fixtures.

## 11. Concurrency And Resilience Testing

Exercise overlapping runtime events:

- Rotation while a panel is still navigating.
- Restart All during page refresh.
- Minimize/restore during navigation.
- Settings reload while timers are active.
- WebView2 process failure followed by restart.
- Repeated layout changes and panel recreation.

Expected result: no unhandled exception, deadlock, frozen window, duplicate timer activity, or undisposed panel retained by the UI.

## 12. Performance And Resource Testing

For normal releases:

- Run the representative production layout for at least 8 hours.
- Record starting, peak, and ending process memory.
- Exercise scheduled restart at least once.
- Confirm the Windows desktop remains responsive.
- Confirm panel count and timers remain stable.

For changes to WebView2 lifecycle, timers, rotation, or disposal, run a 24-hour soak test when practical.

Investigate sustained unbounded growth, increasing browser-process count, UI freezes, or memory that does not improve after Restart All.

## 13. Compatibility And Deployment Testing

Validate:

- Supported Windows 10 and Windows 11 machines, based on available test devices.
- x64 portable output.
- Presence of a compatible WebView2 Runtime.
- Startup from a path containing spaces.
- Default, demo, and production profile selection.
- Clean extraction and launch of the ZIP.
- Expected executable, JSON files, README, and license are present.
- Local backup files, logs, `bin/`, and `obj/` are absent.

## Automated Commands

Run from the repository root:

```powershell
dotnet restore .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
dotnet build .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj -c Release --no-restore
dotnet list .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj package --vulnerable --include-transitive
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-portable.ps1
```

Run the solution tests:

```powershell
dotnet test .\netwatch-lite-wallboard.slnx -c Release --logger trx --results-directory TestResults
```

To include the isolated real-renderer recovery test:

```powershell
$env:NETWATCH_WEBVIEW2_TESTS = "1"
dotnet test .\netwatch-lite-wallboard.slnx -c Release --filter "Category!=Soak"
Remove-Item Env:NETWATCH_WEBVIEW2_TESTS
```

The test uses a neutral local page and its own temporary browser profile. It intentionally crashes only its renderer through Chromium's diagnostic URL, then checks that the panel reloads and associated browser memory is measured. It does not use the operator's browser session. Temporary test profiles remain under `%TEMP%\NetWatchWebViewTests` for investigation and can be removed after test processes exit.

The shared-browser test uses the actual main form and two panels with a unique profile. It verifies ownership of that profile and PID before ending its own test browser process, injects one environment-creation failure, and confirms both panels reconnect. It never selects processes by executable name alone.

Run the reproducible resource test for 24 hours:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-soak.ps1 -Minutes 1440
```

Use `-Minutes 2` for a quick infrastructure check. The test repeatedly navigates a neutral local page and disposes/recreates its panel, writing samples to `TestResults/soak-*.csv` and results to TRX. It checks responsiveness, bounded browser-process count, and a 1536 MB budget for this single neutral fixture. This is a controlled stress test, not proof that production dashboards stay within the same budget. Complete a separate long run with the intended production layout before release.

Tests use .NET 10 SDK and Desktop Runtime plus WebView2 Runtime. The pure logic tests do not create browser controls. Build success is not evidence that tests passed.

## Release Evidence

Use the GitHub release-readiness issue template or copy [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md). Record:

- Commit and version.
- Tester and date.
- Test environment.
- Result for each of the 13 areas.
- Links to CI, logs, screenshots, measurements, or defects.
- Final approval decision.

---
[Previous: Development](DEVELOPMENT.md) | [Documentation home](README.md) | [Next: Security](SECURITY.md)
