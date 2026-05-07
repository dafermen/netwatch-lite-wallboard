# NetWatch Lite Wallboard

NetWatch Lite Wallboard is a Windows NOC-style desktop wallboard built with .NET 8, WinForms, and Microsoft Edge WebView2.

It renders multiple monitoring pages as native WebView2 browser panels. This avoids the iframe restrictions that many operational pages enforce with `X-Frame-Options` or `Content-Security-Policy`.

GitHub repository: [https://github.com/dafermen/netwatch-lite-wallboard](https://github.com/dafermen/netwatch-lite-wallboard)

## Features

- Native Windows executable.
- Embedded Windows executable icon for portable builds.
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
- Optional root-relative local pages for teams that want to ship static wallboard assets beside the executable.
- Friendly panel-level error display when navigation fails.

## Project Structure

```text
netwatch-lite-wallboard/
├── Assets/
│   └── netwatch-lite.ico
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
├── CHANGELOG.md
├── LICENSE
└── README.md
```

## Configuration

The application reads `wallboard.json` from the same folder as `NetWatch-Lite-Wallboard.exe`. During development, it falls back to `Data/wallboard.json`.

```json
{
  "appTitle": "NetWatch Lite Wallboard",
  "rotationEnabled": true,
  "rotationSeconds": 20,
  "defaultLayout": 4,
  "panels": [
    {
      "name": "Operations Overview",
      "url": "https://example.com/",
      "refreshSeconds": 10
    }
  ]
}
```

Panel URLs can be:

- Absolute HTTP/HTTPS URLs.
- Root-relative local URLs when you add your own static files beside the executable, for example `/status/index.html`.

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
└── runtime dependencies...
```

Run:

```powershell
.\NetWatch-Lite-Wallboard.exe
```

Edit `wallboard.json` beside the executable to change panels without recompiling.

## License

NetWatch Lite Wallboard is released under the [MIT License](LICENSE). You can use, copy, modify, merge, publish, distribute, sublicense, and sell copies of the software under the license terms.

## Operational Notes

- Use `defaultLayout: 4` for 2x2 NOC screens.
- Use `defaultLayout: 2` for large side-by-side panels.
- Keep `refreshSeconds` reasonable for internal monitoring pages.
- WebView2 loads pages as native browser views, so pages that fail inside iframe-based dashboards usually work here.
- Some authentication flows may still require interactive login in the WebView2 session.
