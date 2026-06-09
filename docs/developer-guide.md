# NetWatch Lite Wallboard Developer Guide

This guide explains how the Windows WebView2 wallboard is structured, how data moves through the application, and where to make changes when adding features.

## Runtime Overview

NetWatch Lite Wallboard is a WinForms desktop application targeting `net8.0-windows`.

```text
NetWatch-Lite-Wallboard.exe
  |
  | starts WinForms
  v
Program.Main
  |
  | creates the main form
  v
WallboardForm
  |
  | creates a shared WebView2 environment
  | loads wallboard.json
  v
WallboardConfigReader
  |
  | returns normalized models
  v
WallboardConfiguration
  |
  | owns ordered WallboardPanel definitions
  v
WebViewPanelControl x visible panels
  |
  | navigates each panel URL
  v
Microsoft Edge WebView2
  |
  | optional DOM polling with ExecuteScriptAsync
  v
Native alarm banner, pulse, and sound
```

The application is intentionally separate from any ASP.NET or browser-hosted dashboard. Operational pages frequently reject iframe embedding, but WebView2 loads them as real browser views inside a native Windows process.

## Design Principles

- Keep the runtime configuration JSON-driven so operators can change panels without recompiling.
- Keep each panel independent: navigation, refresh timing, and monitoring timing are owned by the panel control.
- Keep layout and page rotation centralized in `WallboardForm`.
- Keep configuration reading, normalization, backup, and write verification centralized in `WallboardConfigReader`.
- Keep DOM monitoring declarative: users configure selectors and rule metadata; the app performs a small, predictable JavaScript scan inside the page.

## Project Files

| File | Purpose |
|---|---|
| `Program.cs` | WinForms entry point. |
| `WallboardConfiguration.cs` | Root configuration model for `wallboard.json`. |
| `WallboardPanel.cs` | Panel model plus DOM monitoring model classes. |
| `WallboardConfigReader.cs` | Loads, normalizes, saves, backs up, and verifies JSON configuration. |
| `WallboardForm.cs` | Main wallboard window, layout, paging, rotation, fullscreen, settings entry point. |
| `WebViewPanelControl.cs` | One WebView2 panel, refresh timer, DOM alarm polling, alarm banner, sound. |
| `SettingsForm.cs` | Visual settings editor plus advanced monitoring JSON editor dialog. |
| `DiagnosticsForm.cs` | Read-only runtime diagnostics window for support and troubleshooting. |
| `Data/wallboard.json` | Development/default configuration copied to build and publish output. |
| `docs/scraping-test-page.html` | Local HTML test dashboard for validating DOM monitoring rules. |
| `Assets/netwatch-lite.ico` | Executable icon referenced by the project file. |

## Scraping Test Page

`docs/scraping-test-page.html` is a standalone local page used for DOM monitoring tests. It updates the rendered DOM immediately when buttons or form fields change, which means `WebViewPanelControl` can detect those changes on the next scraping poll.

The page uses browser `localStorage` to persist test state inside the WebView2 profile. This allows operators to refresh the panel and keep a custom alarm scenario without modifying the HTML file on disk. It also supports export/import of the test state as JSON for reusable scenarios.

The custom DOM target area lets operators change an element id, class list, `data-alarm` value, and text content. This is useful for validating new selectors before pointing monitoring rules at a real production page.

## Configuration Loading Flow

`WallboardForm.InitializeAsync` creates a shared `CoreWebView2Environment`, then calls `ReloadConfigurationAsync`.

`ReloadConfigurationAsync` calls `WallboardConfigReader.LoadAsync`, stores the returned configuration, applies the default layout, resets paging, renders the first page, and schedules rotation.

`WallboardConfigReader.LoadAsync` resolves the active JSON file in this order:

1. `wallboard.json` beside the executable.
2. `Data/wallboard.json` in the development tree.
3. Built-in fallback configuration when neither file exists or parsing fails.

After reading JSON, `WallboardConfigReader.Normalize` converts it into safe runtime values:

- Empty `appTitle` becomes `NetWatch Lite Wallboard`.
- `defaultLayout` accepts only `1`, `2`, `3`, `4`, `6`, or `8`; unsupported values become `4`.
- `alarmSound` accepts only `Exclamation`, `Asterisk`, `Beep`, `Hand`, or `Question`; unsupported values become `Exclamation`.
- `severityColors.critical`, `severityColors.warning`, and `severityColors.info` are normalized to `#RRGGBB` values.
- `rotationSeconds <= 0` becomes `20`.
- Invalid panel URLs are ignored.
- Empty panel names become `Monitoring Panel`.
- `refreshSeconds <= 0` becomes `30`.
- Disabled or invalid monitoring blocks become `null`.
- Monitoring `pollSeconds` and `repeatSoundSeconds` are clamped to `1` through `300`.
- Monitoring rule types are normalized to `exists`, `domText`, or `domClass`.
- Monitoring severities are normalized to `critical`, `warning`, or `info`.

If every panel is invalid, normalization falls back to a safe default panel.

## Configuration Saving Flow

Settings are edited inside a cloned copy of the runtime configuration. This is important: opening the settings window does not mutate the live wallboard until the user saves.

When `SettingsForm.SaveConfigurationAsync` runs:

1. Top-level controls are validated and copied into the cloned configuration.
2. The currently visible panel editor values are applied.
3. The app verifies that at least one panel exists.
4. `WallboardConfigReader.SaveAsync` normalizes the configuration again.
5. If an active JSON file already exists, it is copied to `wallboard.backup.json`.
6. The new JSON is written to a temporary `.tmp` file.
7. The temporary file replaces the active `wallboard.json`.
8. The saved file is read back and compared with the expected JSON.
9. The settings dialog closes with `DialogResult.OK`.
10. `WallboardForm` reloads configuration and rerenders the wallboard.

This save path makes the settings UI resilient against partially edited values and gives operators a backup of the previous JSON.

## Main Form Responsibilities

`WallboardForm` is the orchestration layer. It does not know how to parse monitoring rules and it does not inspect page DOM. Its job is to manage the shell around the panels.

Primary responsibilities:

- Build the top bar.
- Build the panel grid.
- Create the shared WebView2 environment and user data folder.
- Load and reload configuration.
- Render the current page of panels.
- Switch layouts.
- Rotate pages when auto-rotation is enabled.
- Refresh visible panels.
- Toggle fullscreen mode.
- Handle keyboard shortcuts.
- Open `SettingsForm` and reload after a successful save.

## Layout And Rotation

Layouts are stored as the number of visible panels, not as raw rows and columns. `WallboardForm.GetGridDimensions` maps the number to an actual grid:

| Layout | Grid |
|---|---|
| `1` | `1x1` |
| `2` | `2x1` |
| `3` | `3x1` |
| `4` | `2x2` |
| `6` | `3x2` |
| `8` | `4x2` |

Paging is calculated with:

```text
page count = ceiling(panel count / layout)
visible panels = panels.Skip(currentPage * layout).Take(layout)
```

When rotation is enabled and more than one page exists, `_rotationTimer` advances `_currentPage` and rerenders the panel grid.

## WebView Panel Responsibilities

`WebViewPanelControl` is one self-contained monitoring panel.

It owns:

- A title bar with the panel name, status label, and refresh button.
- A WebView2 browser control.
- A panel refresh timer.
- A DOM alarm polling timer.
- A pulse timer used only while an alarm is active.
- The alarm banner and silence button.
- The last alarm signature, used to reset sound/silence state when a different alarm appears.

When `LoadPanelAsync` runs:

1. The panel model is stored.
2. The target URL is resolved to an absolute URI.
3. DOM monitoring timing is configured.
4. WebView2 is initialized with the shared environment.
5. WebView2 kiosk-style settings are applied.
6. The panel navigates to the target URI.
7. The independent refresh timer starts.

## URL Resolution

`WebViewPanelControl.ResolvePanelUri` supports four URL shapes:

- Absolute HTTP/HTTPS URLs are returned directly.
- Absolute `file:///` URLs are returned directly for local HTML test pages and other trusted local content.
- Relative paths such as `docs/scraping-test-page.html` are resolved beside the executable.
- Root-relative paths such as `/status/index.html` are resolved under `wwwroot`.

For local paths, runtime lookup checks the published executable folder first, then the development tree. Query strings are preserved.

## DOM Monitoring Model

The monitoring model lives in `WallboardPanel.cs`:

- `PanelMonitoringOptions`
- `PanelMonitoringRule`

`PanelMonitoringOptions` controls panel-level monitoring:

- `Enabled`
- `PollSeconds`
- `SoundEnabled`
- `RepeatSoundSeconds`
- `Rules`

`PanelMonitoringRule` controls one DOM detection rule:

- `Name`
- `Type`
- `Selector`
- `Contains`
- `Severity`
- `DetailsSelector`
- `SoundEnabled`

Supported rule types:

| Type | Behavior |
|---|---|
| `exists` | Alarm when any visible element matches the selector. If `contains` is supplied, matching elements must contain that text. |
| `domText` | Alarm when a visible selected element contains the configured text. If `contains` is omitted, it behaves like `exists`. |
| `domClass` | Currently evaluated with the same text/visibility path as the other rule types. It is reserved for class-oriented rule naming and future specialization. |

## DOM Monitoring Runtime Flow

DOM monitoring starts only after navigation succeeds. This matters because WebView2 must have a loaded document before the app can execute DOM queries.

Flow:

1. `OnNavigationCompleted` receives a successful navigation event.
2. If the panel has `Monitoring.Enabled == true` and the operator has not paused scraping, `_alarmPollTimer` starts.
3. The app immediately calls `DetectConfiguredAlertsAsync` once so a visible alarm does not wait for the first timer interval.
4. `DetectConfiguredAlertsAsync` serializes the monitoring rules to JSON.
5. The method injects that JSON into a JavaScript function.
6. `CoreWebView2.ExecuteScriptAsync` runs the function inside the loaded page.
7. JavaScript evaluates every selector with `document.querySelectorAll`.
8. Invalid selectors are caught and treated as no matches.
9. JavaScript filters to visible elements.
10. JavaScript checks optional `contains` text against normalized `textContent`.
11. JavaScript collects detail text from `detailsSelector` or from the matched elements.
12. JavaScript returns a small alarm snapshot object.
13. C# deserializes the snapshot.
14. `ShowAlarmState` displays or updates the native alarm banner.
15. `ClearAlarmState` hides the banner when no rule matches.

The JavaScript intentionally returns only small structured data. It does not return full HTML, screenshots, or large page content.

Panels with monitoring rules expose a **Stop Scraping** / **Start Scraping** button in the title bar. This toggle only pauses the panel's DOM polling timer. It does not edit the saved JSON, stop the panel refresh timer, or unload the page.

## Alarm Snapshot

`AlarmSnapshot` is the C# DTO used to receive the JavaScript result:

| Field | Meaning |
|---|---|
| `Active` | Whether any rule matched. |
| `Title` | Name of the highest-severity matched rule. |
| `Severity` | Highest matched severity. |
| `SoundEnabled` | Whether any matched rule allows sound. |
| `Alarms` | Detail strings displayed in the banner. |

When multiple rules match, JavaScript sorts them by severity:

```text
critical > warning > info
```

The banner title uses the highest-severity match. The detail line includes distinct detail strings from all matched rules.

## Alarm Sound And Silence

Sound is controlled at two levels:

- Panel-level `monitoring.soundEnabled`.
- Rule-level `rule.soundEnabled`.

Sound plays only when the panel-level setting is true and at least one matched rule does not explicitly set `soundEnabled` to false.

The silence button acknowledges alarm sound but leaves the visual alarm visible. Once silenced, sound remains muted across page refreshes and changing alarm snapshots until the operator presses **Enable Sound**.

The selected sound is stored as the top-level `alarmSound` field in `WallboardConfiguration`. `WallboardConfigReader` normalizes that value to one of the supported Windows `SystemSounds` names:

- `Exclamation`
- `Asterisk`
- `Beep`
- `Hand`
- `Question`

`SettingsForm` exposes the same list in the **Alarm sound** dropdown. `WallboardForm` passes the normalized value into each `WebViewPanelControl`, and `WebViewPanelControl.PlayConfiguredSystemSound` maps the text value to the actual `SystemSounds` call.

Settings also exposes:

- **Preview** to play the selected sound immediately.
- **Test Alarm** to show the selected severity colors in a local preview dialog.
- Color picker buttons for critical, warning, and info alarm colors.

`WebViewPanelControl` receives `AlarmSeverityColors` from `WallboardForm` when each panel is loaded. The panel converts the configured hex values to `Color` values and uses them in `GetAlarmBannerColor` and `GetAlarmBorderColor` for the pulsing native alarm chrome.

For a custom `.wav` file, replace the `SystemSounds` call with `SoundPlayer`. If custom sounds should become operator-configurable, add a new configuration field such as `alarmSoundPath` or `alarmSoundName`, normalize it in `WallboardConfigReader`, document it in the README, and keep a safe built-in fallback when the file is missing.

## Settings Form

`SettingsForm` edits a cloned configuration. The clone is deep enough to include panel monitoring options and rules, which prevents accidental mutation of the live configuration while the dialog is open.

The form is organized into:

- Top-level wallboard settings.
- Alarm sound selection.
- Severity color selection.
- Read-only panel grid.
- Panel editor fields.
- Panel command buttons.
- Status row with active JSON path and unsaved-change messages.
- Footer with save/cancel actions.

Panel commands:

- Add.
- Update.
- Duplicate.
- Delete.
- Move up.
- Move down.
- Edit Monitoring JSON.
- Export JSON.
- Import JSON.

The monitoring editor keeps a JSON text editor for full control because monitoring rules are CSS-selector based and operational pages vary widely. It also includes a basic rule builder that appends common selector rules to the JSON for operators who do not want to hand-write every property.

Import/export behavior:

- Export applies the currently visible panel editor values, validates top-level settings, and writes the edited configuration to a user-selected JSON file.
- Import reads a selected JSON file, applies editor-safe normalization, replaces the current settings editor contents, and marks the window as unsaved.
- Import does not write to the active `wallboard.json`; the operator must still press **Save Changes**.

## Monitoring JSON Editor

`MonitoringJsonEditorForm` is a modal editor for one panel's monitoring block.

Operators can:

- Paste or edit a monitoring JSON object.
- Leave the text empty to disable monitoring.
- Insert a starter PLC alarm template.
- Add a basic rule through form fields for name, type, selector, text match, severity, details selector, and sound.
- Apply changes and let `SettingsForm.TryParseMonitoringJson` validate them.

The basic rule builder parses the current JSON editor content, creates a default enabled monitoring block when the editor is empty, appends a `PanelMonitoringRule`, and serializes the result back into the editor. This keeps the generated output visible and editable.

The editor's **Validate** button parses the JSON and performs lightweight selector screening. It catches common local mistakes such as empty selectors, unbalanced brackets, unclosed quotes, trailing selector combinators, and malformed `detailsSelector` values. WebView2 remains the authoritative runtime selector evaluator because only the loaded page DOM can prove whether a selector actually matches.

Validation rules:

- Empty or `null` disables monitoring.
- Invalid JSON shows a parse error.
- Disabled monitoring becomes `null`.
- Enabled monitoring must include at least one rule.
- Every enabled rule must include a non-empty selector.
- Timing values are clamped.
- Rule type and severity names are normalized.

## Diagnostics Window

`DiagnosticsForm` is opened from the main top bar. It is intentionally read-only and gives support a fast snapshot of runtime state without requiring file browsing.

Diagnostics includes:

- Current app version, process ID, and generated timestamp.
- Active configuration path from `WallboardConfigReader.GetConfigurationFilePath`.
- Error log path from `AppErrorLog.LogFilePath`.
- WebView2 user data folder.
- Current layout, page, visible panel count, and configured panel count.
- Alarm sound and severity colors.
- Per-panel URL, refresh interval, monitoring state, poll interval, and rule count.

The diagnostics window has **Refresh** and **Copy** actions so the support team can capture the state in a ticket or troubleshooting note.

## WebView2 Environment

`WallboardForm.InitializeAsync` creates a shared WebView2 environment with this user data folder:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardWebView2
```

Because all panels share the same environment, cookies and authentication state can persist between app launches and across panels, similar to a normal browser profile.

## Error Handling

Configuration load failures fall back to default configuration so the app can still start.

Unexpected errors are logged by `AppErrorLog` to:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log
```

The log records timestamp, context, exception type, message, and stack trace. Logging is intentionally defensive: logging failures are swallowed so diagnostics never become the cause of a crash.

Global handlers are installed in `Program.Main`:

- `Application.ThreadException`
- `AppDomain.CurrentDomain.UnhandledException`
- `TaskScheduler.UnobservedTaskException`

These handlers are a last line of defense. Normal UI paths should still catch exceptions near the operation that failed so the app can keep running.

Navigation failures render a small friendly HTML page inside the panel with the WebView2 error status. The panel also stops DOM polling while navigation has failed.

DOM monitoring catches:

- `InvalidOperationException`
- `JsonException`
- `COMException`

In those cases, alarm state is cleared. This prevents stale native alarm banners when WebView2 navigation, script execution, or JSON parsing fails.

`WallboardForm` uses safe wrappers around async UI operations such as initialization, reload, settings, layout changes, and rotation. This avoids unobserved `async void` failures from WinForms event handlers.

`WebViewPanelControl` logs WebView2 `ProcessFailed` events. If the embedded browser process fails, the panel stops its timers and renders a local error message instead of leaving the operator with a silent blank panel.

`RenderCurrentPageAsync` is guarded by an async lock so rotation, reload, and layout changes cannot overlap while panels are still initializing.

## Publishing

Use a self-contained Windows x64 publish:

```powershell
dotnet publish .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -o .\publish\wallboard-webview2-win-x64
```

The publish output includes:

- `NetWatch-Lite-Wallboard.exe`
- `wallboard.json`
- WebView2 loader/runtime support assemblies
- .NET runtime dependencies

## Extension Points

Common places to extend the application:

- Add a new layout: update `SupportedLayouts`, `NormalizeLayout`, and `GetGridDimensions`.
- Add a new panel field: update `WallboardPanel`, `SettingsForm`, `WallboardConfigReader.Normalize`, and the README schema.
- Add a new monitoring rule type: update `NormalizeRuleType`, `NormalizeMonitoringRuleType`, and the JavaScript matcher in `DetectConfiguredAlertsAsync`.
- Change alarm visuals: update `GetAlarmBannerColor`, `GetAlarmBorderColor`, and banner construction in `WebViewPanelControl`.
- Change save behavior: update `WallboardConfigReader.SaveAsync`.

## Operational Notes

- Keep panel refresh intervals appropriate for the target systems.
- Keep DOM polling intervals reasonable. Polling every second is supported but should be used only when the page and machine can handle it.
- WebView2 user data is persistent. Clear `%LOCALAPPDATA%\NetWatchLite\WallboardWebView2` if you need to reset browser sessions.
- Relative local pages must be copied beside the executable. Root-relative local pages require a `wwwroot` folder in the publish output or development tree.
- Selector accuracy matters. Prefer stable IDs/classes from the monitored application over visual-only selectors that may change with styling updates.
