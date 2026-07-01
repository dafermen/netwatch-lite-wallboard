# NetWatch Lite Wallboard User Guide

This guide is for operators and support users who need to run and configure the wallboard.

## What The App Does

NetWatch Lite Wallboard shows several web pages in one Windows desktop window.

Use it for:

- Cloud dashboards.
- Incident queues.
- Service status pages.
- Network maps.
- Change calendars.
- Internal operational pages.

The app displays pages and refreshes them. It does not read page content, scrape values, or create alarms from what appears inside a page.

## Start The App

From source:

```powershell
cd C:\Projects\netwatch-lite-wallboard
dotnet run --project .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
```

From a portable build:

```powershell
.\NetWatch-Lite-Wallboard.exe
```

## Main Buttons

The top bar gives quick access to the most common actions:

| Button | What it does |
|---|---|
| `Settings` | Opens the configuration window. |
| `Diagnostics` | Opens runtime information useful for support. |
| `Refresh All` | Refreshes the visible panels. |
| `Restart All` | Recreates all visible browser panels. |
| Layout buttons | Changes how many panels are visible. |

Each panel also has:

| Button | What it does |
|---|---|
| `Refresh` | Refreshes only that panel. |
| `Restart` | Recreates only that panel. |

Use **Restart** when a page gets stuck or looks heavier than usual. Use **Refresh** for a normal page reload.

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `F` | Toggle fullscreen. |
| `R` | Refresh visible panels. |
| `C` | Open settings. |
| `ESC` | Exit fullscreen, or close the app if fullscreen is not active. |

## Open Settings

Open settings with:

- The **Settings** button.
- The `C` key.

In settings you can change:

- Wallboard title.
- Auto rotation.
- Rotation interval.
- Default layout.
- Low-power mode.
- Scheduled panel restart.
- Fullscreen startup.
- Close confirmation.
- Memory monitor and warning threshold.
- Top-bar auto-hide.
- Visual theme.
- Panel list.
- Panel names.
- Panel URLs.
- Panel refresh intervals.

## Edit Panels

To add a panel:

1. Click **New Panel**.
2. Enter a name.
3. Enter the URL.
4. Enter refresh seconds.
5. Click **Add Panel**.
6. Click **Save Changes**.

To edit a panel:

1. Select the panel in the list.
2. Change the fields on the right.
3. Click **Apply**.
4. Click **Save Changes**.

To duplicate a panel:

1. Select the panel.
2. Click **Duplicate**.
3. Edit the copy if needed.
4. Click **Save Changes**.

To reorder panels:

1. Select the panel.
2. Click **Move Up** or **Move Down**.
3. Click **Save Changes**.

The order controls where panels appear and how they rotate.

## Configure URLs

Panel URLs can be:

- Web URLs: `https://example.com/status`
- Local file URLs: `file:///C:/Tools/status.html`
- Packaged local paths: `pages/status.html`
- Root-relative packaged paths: `/status/index.html`

Most users should use normal `https://` URLs.

## Layouts

| Layout | Best use |
|---|---|
| `1` | One large dashboard. |
| `2` | Two important dashboards side by side. |
| `3` | Three wide panels. |
| `4` | Standard 2x2 wallboard. |
| `6` | Dense operations screen. |
| `8` | TV wall or ultrawide display. |

## Auto Rotation

Auto rotation is useful when you have more panels than the selected layout can show.

Example:

- You have 8 panels.
- Your layout is 4.
- The app shows panels 1-4, then rotates to panels 5-8.

Set the interval with **Rotation seconds**.

## Low-Power Mode

Low-power mode releases visible browser panels when the app is minimized and rebuilds them when restored.

Enable it when:

- The app runs for many hours.
- The PC sometimes needs to free memory.
- The wallboard is minimized when not in use.

Disable it when:

- You need pages to keep running while minimized.
- A dashboard requires continuous browser activity.

## Scheduled Restart

Scheduled restart recreates visible browser panels every configured number of hours.

Use it for wallboards that run all day or overnight. It is safer than restarting the whole app because it only rebuilds the visible WebView2 panels.

## Kiosk Mode

Enable **Start fullscreen** when the wallboard is used on a TV or shared display.

Enable **Confirm exit** to prevent accidental closes from `ESC`, the window close button, or keyboard shortcuts.

## Memory Monitor

The memory monitor shows approximate process memory in the top bar.

When memory crosses the configured threshold, the label recommends restart. It does not restart automatically; the operator can use **Restart All**.

## Auto-Hide Top Bar

Auto-hide hides the top bar after a short idle delay. Move the mouse to the top edge of the window to show it again.

This is useful for TV displays where the browser panels should use almost the full screen.

## Themes

The native shell supports:

- `Dark`
- `Light`
- `High contrast`

Themes affect the wallboard controls around the web pages. The loaded web pages keep their own colors.

## Import And Export

Use **Export JSON** to save a backup of your configuration.

Use **Import JSON** to load an existing configuration into the editor.

Imported settings are not written to the active file until you click **Save Changes**.

## Environment Profiles

Profiles let you keep separate configurations.

Examples:

- Demo: `wallboard.demo.json`
- Production: `wallboard.production.json`

Run a profile:

```powershell
.\NetWatch-Lite-Wallboard.exe --environment production
```

## Diagnostics

Open diagnostics when something looks wrong.

Diagnostics shows:

- Active configuration file.
- Active profile.
- App version.
- Process ID.
- WebView2 data folder.
- Current layout.
- Panel count.
- Panel URLs.
- Refresh intervals.
- Error log path.

Use **Copy** to paste diagnostics into a support note.

## Troubleshooting

If a panel is blank:

- Click that panel's **Restart** button.
- Check that the URL opens in Microsoft Edge.
- Check if the site requires login.
- Open diagnostics and confirm the active configuration path.

If settings changes do not appear:

- Make sure you clicked **Save Changes**.
- Close old portable copies of the app.
- Run the current build again.

If the computer uses too much memory:

- Click **Restart All**.
- Enable low-power mode.
- Enable the memory monitor.
- Enable scheduled restart for long sessions.
- Increase refresh intervals for heavy dashboards.
- Check whether one specific page uses high memory in Microsoft Edge too.

## Where Files Are Stored

Browser session data:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardWebView2
```

Error logs:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log
```

Active configuration is shown in the diagnostics window.
