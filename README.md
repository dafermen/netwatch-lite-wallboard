# NetWatch Lite Wallboard

NetWatch Lite Wallboard is a Windows NOC-style desktop wallboard built with .NET 8, WinForms, and Microsoft Edge WebView2.

It renders multiple monitoring pages as native WebView2 browser panels. This avoids the iframe restrictions that many operational pages enforce with `X-Frame-Options` or `Content-Security-Policy`.

GitHub repository: [https://github.com/dafermen/netwatch-lite-wallboard](https://github.com/dafermen/netwatch-lite-wallboard)

## Features

- Native Windows executable.
- Microsoft Edge WebView2 per panel.
- JSON-driven configuration.
- 2-panel and 4-panel layouts.
- Automatic page rotation.
- Independent refresh interval per panel.
- Manual refresh for visible panels.
- Individual refresh button per panel.
- Fullscreen mode for NOC/TV displays.
- Keyboard shortcuts:
  - `F`: toggle fullscreen.
  - `R`: refresh visible panels.
  - `ESC`: exit fullscreen.
- Root-relative local sample pages for testing.
- Friendly panel-level error display when navigation fails.

## Project Structure

```text
netwatch-lite-wallboard/
├── Data/
│   └── wallboard.json
├── docs/
│   └── developer-guide.md
├── src/
│   └── NetWatchLite.Wallboard.WebView2/
│       ├── NetWatchLite.Wallboard.WebView2.csproj
│       ├── Program.cs
│       ├── WallboardConfigReader.cs
│       ├── WallboardConfiguration.cs
│       ├── WallboardForm.cs
│       ├── WallboardPanel.cs
│       └── WebViewPanelControl.cs
├── wwwroot/
│   └── wallboard-sample.html
├── CHANGELOG.md
├── LICENSE
└── README.md
```

## Configuration

The application reads `wallboard.json` from the same folder as `NetWatch-Lite-Wallboard.exe`. During development, it falls back to `Data/wallboard.json`.

```json
{
  "appTitle": "GTSG Monitoring",
  "rotationEnabled": true,
  "rotationSeconds": 20,
  "defaultLayout": 4,
  "panels": [
    {
      "name": "NGSS NYELF",
      "url": "https://your-monitoring-page.example/health",
      "refreshSeconds": 10
    }
  ]
}
```

Panel URLs can be:

- Absolute HTTP/HTTPS URLs.
- Root-relative local sample URLs such as `/wallboard-sample.html?panel=Demo`.

## Requirements

- Windows 10 or later.
- .NET 8 SDK for development.
- Microsoft Edge WebView2 Runtime on the target machine.

Most modern Windows systems already include the WebView2 Runtime. If not, install the Evergreen WebView2 Runtime from Microsoft.

## Build And Run

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
```

## Publish Portable Build

```powershell
dotnet publish .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -o .\publish\wallboard-webview2-win-x64
```

Expected output:

```text
publish/wallboard-webview2-win-x64/
├── NetWatch-Lite-Wallboard.exe
├── wallboard.json
├── wwwroot/
└── runtime dependencies...
```

Run:

```powershell
.\NetWatch-Lite-Wallboard.exe
```

Edit `wallboard.json` beside the executable to change panels without recompiling.

## Operational Notes

- Use `defaultLayout: 4` for 2x2 NOC screens.
- Use `defaultLayout: 2` for large side-by-side panels.
- Keep `refreshSeconds` reasonable for internal monitoring pages.
- WebView2 loads pages as native browser views, so pages that fail inside iframe-based dashboards usually work here.
- Some authentication flows may still require interactive login in the WebView2 session.
