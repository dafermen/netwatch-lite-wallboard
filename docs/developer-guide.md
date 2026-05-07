# NetWatch Lite Wallboard Developer Guide

This document explains the internal structure of the Windows WebView2 wallboard.

## Runtime Overview

NetWatch Lite Wallboard is a WinForms desktop application targeting `net8.0-windows`.

```text
NetWatch-Lite-Wallboard.exe
  |
  | reads wallboard.json
  v
WallboardConfigReader
  |
  v
WallboardForm
  |
  v
WebViewPanelControl x N
  |
  v
Microsoft Edge WebView2
```

The app is intentionally separate from the ASP.NET dashboard. It exists because browser iframes cannot load many operational monitoring pages that set anti-embedding headers.

The executable icon is stored at `Assets/netwatch-lite.ico` and is referenced through the project file `ApplicationIcon` property.

## Configuration Flow

`WallboardConfigReader.LoadAsync` resolves and reads `wallboard.json`.

Lookup order:

1. `wallboard.json` beside the executable.
2. `Data/wallboard.json` when running from the development tree.
3. Built-in fallback configuration when the file is missing or invalid.

Normalization:

- Empty `appTitle` becomes `NetWatch Lite Wallboard`.
- `defaultLayout` accepts `1`, `2`, `3`, `4`, `6`, or `8`; other values become `4`.
- `rotationSeconds <= 0` becomes `20`.
- `refreshSeconds <= 0` becomes `30`.
- Invalid panels are ignored.

## Classes

### Program

Entry point. Calls `ApplicationConfiguration.Initialize` and starts `WallboardForm`.

### WallboardConfiguration

Root configuration model:

- `AppTitle`
- `RotationEnabled`
- `RotationSeconds`
- `DefaultLayout`
- `Panels`

### WallboardPanel

One panel declaration:

- `Name`
- `Url`
- `RefreshSeconds`

### WallboardConfigReader

Static configuration reader.

Important methods:

- `LoadAsync`: loads and normalizes configuration.
- `ResolveWallboardFilePath`: locates runtime or development JSON.
- `Normalize`: applies safe defaults.
- `IsValidPanel`: accepts absolute HTTP/HTTPS URLs and optional root-relative local URLs.

### WallboardForm

Main form.

Responsibilities:

- Builds the top bar.
- Builds the panel grid.
- Loads configuration.
- Creates visible `WebViewPanelControl` instances.
- Handles layout switching.
- Maps layouts to dense grids: `1x1`, `2x1`, `3x1`, `2x2`, `3x2`, and `4x2`.
- Handles automatic page rotation.
- Handles fullscreen mode.
- Handles keyboard shortcuts.

### WebViewPanelControl

One rendered panel.

Responsibilities:

- Hosts one WebView2 browser control.
- Shows panel title and status.
- Provides an individual refresh button.
- Runs an independent refresh timer.
- Resolves optional root-relative local URLs to file URIs.
- Renders a friendly error page on navigation failure.

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `F` | Toggle fullscreen. |
| `R` | Refresh visible panels. |
| `ESC` | Exit fullscreen. |

## Layouts

| Layout | Grid | Typical Use |
|---|---|---|
| `1` | `1x1` | One focused operational screen. |
| `2` | `2x1` | Two large side-by-side panels. |
| `3` | `3x1` | Three wide panels on large displays. |
| `4` | `2x2` | Standard NOC grid. |
| `6` | `3x2` | Dense TV dashboards. |
| `8` | `4x2` | Ultrawide or high-density wallboards. |

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

## Operational Notes

- Root-relative URLs are resolved from a published `wwwroot` folder if you choose to add local static pages.
- Remote HTTP/HTTPS URLs are loaded directly by WebView2.
- WebView2 user data is stored under `%LOCALAPPDATA%\NetWatchLite\WallboardWebView2`.
- Authentication cookies and sessions can persist through that WebView2 user data folder.
