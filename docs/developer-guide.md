# NetWatch Lite Wallboard Developer Guide

This guide explains the project in plain language for a junior developer joining the codebase.

The current application is a Windows desktop wallboard. It displays configured web pages in WebView2 panels. It does not scrape page content, inspect DOM elements, run monitoring selectors, or generate alarms from page content.

## Mental Model

Think of the app as four layers:

```text
Program.cs
  starts WinForms

WallboardForm.cs
  owns the main window, layout, buttons, rotation, settings, and panel lifecycle

WallboardConfigReader.cs + model classes
  load, normalize, save, and verify wallboard JSON

WebViewPanelControl.cs
  owns one browser panel, its refresh timer, status text, and restart behavior
```

Runtime flow:

```text
NetWatch-Lite-Wallboard.exe
  |
  v
Program.Main
  |
  v
WallboardForm
  |
  | creates shared WebView2 environment
  | loads wallboard JSON
  v
WallboardConfigReader
  |
  v
WallboardConfiguration + WallboardPanel
  |
  v
WebViewPanelControl for each visible panel
  |
  v
Microsoft Edge WebView2
```

## Important Files

| File | What it does |
|---|---|
| `AGENTS.md` | Required instructions for future Codex sessions. Read this before editing. |
| `CURRENT_STATUS.md` | Current phase, completed work, validation notes, and next recommended steps. |
| `Program.cs` | Starts the WinForms app and installs global error handlers. |
| `WallboardConfiguration.cs` | Represents the full `wallboard.json` file. |
| `WallboardPanel.cs` | Represents one panel in the JSON file. |
| `WallboardConfigReader.cs` | Finds, reads, normalizes, saves, backs up, and verifies JSON configuration. |
| `WallboardForm.cs` | Main app window. Owns top bar, grid layout, page rotation, fullscreen, low-power mode, restart actions, settings, and diagnostics. |
| `WebViewPanelControl.cs` | One visual panel. Owns one WebView2 browser, refresh timer, status label, refresh button, and restart button. |
| `SettingsForm.cs` | Visual editor for the JSON configuration. |
| `DiagnosticsForm.cs` | Read-only troubleshooting window. |
| `AppErrorLog.cs` | Writes unexpected errors to a local log file. |
| `Data/wallboard.json` | Default development configuration. |
| `scripts/publish-portable.ps1` | Creates a portable publish folder and ZIP. |

## Configuration Model

The JSON file maps to these classes:

```text
WallboardConfiguration
  appTitle
  rotationEnabled
  rotationSeconds
  defaultLayout
  lowPowerModeEnabled
  autoRestartEnabled
  autoRestartHours
  startFullscreen
  confirmExit
  memoryMonitorEnabled
  memoryWarningMegabytes
  autoHideTopBarEnabled
  theme
  panels[]

WallboardPanel
  name
  url
  refreshSeconds
```

There are no monitoring, scraping, selector, severity, or alarm fields in the current model.

## Configuration Loading

`WallboardForm.InitializeAsync` creates the shared WebView2 environment and then loads configuration.

`WallboardConfigReader.LoadAsync` chooses the active JSON file in this order:

1. If an environment was selected, try `wallboard.{environment}.json`.
2. Otherwise use `wallboard.json`.
3. Look beside the executable first.
4. During development, fall back to the repository `Data/` folder.
5. If no valid file can be loaded, use a safe built-in default.

Environment can be selected by:

- `--environment demo`
- `--env demo`
- `--config-env demo`
- `NETWATCH_WALLBOARD_ENV=demo`

After reading JSON, the reader normalizes values so the app can keep running safely:

- Empty title becomes `NetWatch Lite Wallboard`.
- Supported layouts are only `1`, `2`, `3`, `4`, `6`, and `8`.
- Invalid layouts become `4`.
- `rotationSeconds <= 0` becomes `20`.
- Empty panel names become `Wallboard Panel`.
- Invalid panel URLs are skipped.
- `refreshSeconds <= 0` becomes `30`.
- `autoRestartHours` is clamped from `1` to `24`.
- `memoryWarningMegabytes` is clamped from `256` to `65536`.
- `theme` is normalized to `dark`, `light`, or `high-contrast`.
- If every panel is invalid, a default panel is created.

## Configuration Saving

The settings window edits a cloned configuration. That means the live wallboard does not change while the operator is still typing.

When the user clicks **Save Changes**:

1. `SettingsForm` copies top-level fields into the cloned configuration.
2. It applies the currently visible panel editor values.
3. It verifies that at least one valid panel exists.
4. `WallboardConfigReader.SaveAsync` normalizes the configuration again.
5. If an active JSON file already exists, it creates `wallboard.backup.json`.
6. It writes to a temporary `.tmp` file.
7. It replaces the active JSON file.
8. It reads the saved file back and verifies the final JSON.
9. The settings window closes with success.
10. `WallboardForm` reloads and rerenders the wallboard.

## Main Window Responsibilities

`WallboardForm` is the conductor of the app. It should own behavior that affects the whole wallboard:

- Top bar buttons.
- Current layout.
- Current page.
- Page rotation.
- Fullscreen mode.
- Settings dialog.
- Diagnostics dialog.
- Reloading configuration.
- Rebuilding visible panels.
- Restarting visible panels.
- Low-power behavior when minimized.
- Scheduled visible-panel restart.
- Memory status display.
- Top-bar auto-hide.
- Kiosk startup and close confirmation.
- Native shell theme application.

Keep global behavior here. Avoid putting whole-app behavior inside an individual panel.

## Panel Responsibilities

`WebViewPanelControl` owns behavior for one panel only:

- Panel title.
- Status label.
- Refresh button.
- Restart button.
- WebView2 browser instance.
- Panel-specific refresh timer.
- Navigation error page.
- WebView2 process failure handling.

When a panel loads:

1. It receives a `WallboardPanel`.
2. It resolves the URL.
3. It initializes WebView2 with the shared environment.
4. It applies browser settings.
5. It navigates to the URL.
6. It starts its refresh timer.

When a panel is disposed, it stops its timer and releases the WebView2 control. This matters because long-running wallboards can consume memory if browser controls are not cleaned up.

## Layout And Rotation

The app stores layout as the number of visible panels.

| Layout value | Grid |
|---|---|
| `1` | `1x1` |
| `2` | `2x1` |
| `3` | `3x1` |
| `4` | `2x2` |
| `6` | `3x2` |
| `8` | `4x2` |

Paging formula:

```text
page count = ceiling(panel count / layout)
visible panels = panels.Skip(currentPage * layout).Take(layout)
```

If rotation is enabled and there is more than one page, a timer advances `_currentPage` and rerenders the grid.

`RenderCurrentPageAsync` is guarded so rotation, reload, and layout changes do not overlap while panels are being created or destroyed.

## Low-Power Mode

Low-power mode is controlled by `lowPowerModeEnabled`.

When enabled:

- If the window is minimized, the app releases visible WebView2 panels.
- When the window is restored, the app rebuilds the visible panels.

This helps reduce memory and CPU use when the wallboard is not visible.

## Restart Behavior

There are two restart levels:

- **Restart Panel**: disposes and recreates one panel's WebView2 control.
- **Restart All Panels**: rerenders all visible panels.

Use restart when a panel gets stuck, consumes too much memory, or needs a cleaner reset than normal refresh.

## Scheduled Restart

Scheduled restart is controlled by `autoRestartEnabled` and `autoRestartHours`.

The timer lives in `WallboardForm`. When it fires, it calls the same visible-panel restart path used by **Restart All**. It does not close the application and does not modify the JSON file.

## Kiosk Behavior

`startFullscreen` enters fullscreen after configuration loads. `confirmExit` shows a close confirmation for user-triggered exits.

The confirmation is intentionally in `FormClosing` so it catches the window close button, `ESC`, and other normal user close paths.

## Memory Monitor

`memoryMonitorEnabled` starts a timer that checks the current process working set about once per minute.

If memory is above `memoryWarningMegabytes`, the top bar label recommends restart. This is intentionally advisory; it does not restart automatically.

## Auto-Hide Top Bar

`autoHideTopBarEnabled` hides the top bar after a short idle delay. Moving the mouse to the top edge shows it again.

The top bar is shown before opening Settings so the operator does not lose access to controls.

## Themes

Themes are centralized in `ThemePalette`.

Supported JSON values are:

- `dark`
- `light`
- `high-contrast`

Themes apply to the native shell and panel chrome. WebView2 page content is not restyled.

## URL Resolution

Panels support:

- `https://example.com/status`
- `http://example.com/status`
- `file:///C:/Tools/status.html`
- `pages/status.html`
- `/status/index.html`

Relative paths are resolved beside the executable first. During development, the app also checks the repository structure.

Root-relative local paths are resolved under `wwwroot`.

## Diagnostics

`DiagnosticsForm` gives support a read-only runtime snapshot:

- App version.
- Process ID.
- Active environment profile.
- Active configuration path.
- Error log path.
- WebView2 user data path.
- Current layout and page.
- Panel count.
- Visible panel count.
- Panel URLs and refresh intervals.

The **Copy** button is useful when someone needs to paste diagnostics into a support ticket.

## Error Handling

Unexpected errors are logged to:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log
```

Global handlers are registered in `Program.Main`:

- `Application.ThreadException`
- `AppDomain.CurrentDomain.UnhandledException`
- `TaskScheduler.UnobservedTaskException`

Panel navigation failures render a friendly local error page inside the affected panel.

WebView2 browser process failures are logged and shown inside the panel instead of leaving a silent blank area.

## Development Commands

Restore and build:

```powershell
cd C:\Projects\netwatch-lite-wallboard
dotnet restore
dotnet build
```

Run:

```powershell
dotnet run --project .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
```

Run with demo profile:

```powershell
dotnet run --project .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj -- --environment demo
```

Publish portable:

```powershell
.\scripts\publish-portable.ps1
```

## How To Add A Feature

Add a new top-level JSON setting:

1. Add the property to `WallboardConfiguration.cs`.
2. Normalize it in `WallboardConfigReader.cs`.
3. Add controls to `SettingsForm.cs` if the user should edit it visually.
4. Apply behavior in `WallboardForm.cs` or `WebViewPanelControl.cs`.
5. Update `README.md` and `docs/user-guide.md`.

Add a new panel setting:

1. Add the property to `WallboardPanel.cs`.
2. Normalize it in `WallboardConfigReader.cs`.
3. Add the field to `SettingsForm.cs`.
4. Use it in `WebViewPanelControl.cs`.
5. Update JSON examples and documentation.

Add a new layout:

1. Add the value to every `SupportedLayouts` list.
2. Update layout normalization.
3. Update `WallboardForm.GetGridDimensions`.
4. Update README and user guide tables.

## What Not To Reintroduce Accidentally

The scraping subsystem was removed on purpose. Do not reintroduce these concepts unless the project direction changes:

- DOM selector polling.
- Monitoring JSON rules.
- Alarm sound configuration.
- Severity colors.
- Page-content scraping.
- JavaScript injected for monitoring page content.

Normal WebView2 navigation, refresh, restart, and diagnostics are still in scope.

## Troubleshooting For Developers

If the app opens but panels are blank:

- Check the URL in `wallboard.json`.
- Open the same URL in Microsoft Edge.
- Check the diagnostics window.
- Check `%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log`.

If settings do not appear to change:

- Confirm you clicked **Save Changes**.
- Confirm the active JSON path in diagnostics.
- Confirm you are running the freshly built executable, not an older portable copy.

If memory grows over time:

- Use **Restart All Panels** as an operational reset.
- Enable low-power mode if the app is often minimized.
- Check if one loaded web page is unusually heavy in normal Edge.
