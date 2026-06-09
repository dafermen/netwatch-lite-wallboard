# NetWatch Lite Wallboard

NetWatch Lite Wallboard is a Windows NOC-style desktop wallboard built with .NET 8, WinForms, and Microsoft Edge WebView2.

The application renders multiple operational web pages as native browser panels. Each panel is an independent WebView2 instance with its own refresh timer, title, status label, and optional DOM monitoring rules. This design is useful for monitoring pages that cannot be embedded in browser iframes because of `X-Frame-Options` or `Content-Security-Policy` headers.

GitHub repository: [https://github.com/dafermen/netwatch-lite-wallboard](https://github.com/dafermen/netwatch-lite-wallboard)

## Download

Download the latest Windows x64 portable ZIP from GitHub Releases:

[Download NetWatch Lite Wallboard v0.2.3 for Windows x64](https://github.com/dafermen/netwatch-lite-wallboard/releases/download/v0.2.3/NetWatch-Lite-Wallboard-WebView2-win-x64-v0.2.3.zip)

Previous portable release:

[Download NetWatch Lite Wallboard v0.2.2 for Windows x64](https://github.com/dafermen/netwatch-lite-wallboard/releases/download/v0.2.2/NetWatch-Lite-Wallboard-WebView2-win-x64-v0.2.2.zip)

## What It Does

NetWatch Lite Wallboard gives support teams, operations rooms, and TV wall displays one desktop application that can show several live web systems at the same time.

It can:

- Display 1, 2, 3, 4, 6, or 8 panels at once.
- Rotate through additional panels when more panels exist than the current layout can show.
- Refresh each panel on its own interval.
- Reload all visible panels manually.
- Edit `wallboard.json` through a built-in settings window.
- Export and import `wallboard.json` from the settings window for backup or moving a configuration to another machine.
- Monitor the rendered DOM of a panel and raise native visual/audible alarms when configured CSS selectors match.
- Create basic monitoring rules through a form-based rule builder, while still keeping the advanced JSON editor available.
- Load normal HTTP/HTTPS URLs or local pages shipped beside the executable.

The DOM monitoring feature is intentionally simple and transparent. The app does not run a separate scraper process and does not fetch HTML with a hidden HTTP client. Instead, it asks WebView2 to execute JavaScript inside the page that is already loaded, then checks visible DOM elements with CSS selectors.

## Operational Value

NetWatch Lite Wallboard is built to help Technical Support Groups, operations teams, and production support teams see problems earlier, react faster, and protect critical business processes before an issue becomes visible to a larger audience.

In a production environment, minutes matter. A delayed response can mean missed SLAs, longer downtime, more escalations, more manual investigation, and more pressure on support teams. This wallboard gives the team a single operational view where important systems, dashboards, alarms, health indicators, queues, and status pages can be watched continuously.

The goal is simple: help support notice first.

When support sees an alarm, red indicator, failed panel, or operational status change before users, managers, customers, or downstream teams report it, the organization gains time. That time can be used to investigate, communicate, recover, escalate correctly, and reduce operational impact.

## Why It Helps Operations

- **Earlier detection:** support teams can identify visual alarms, red indicators, and page-level warnings before they become widespread incidents.
- **Faster response:** multiple systems are visible in one place, reducing the need to manually switch between browser tabs, dashboards, and tools.
- **Better situational awareness:** operations can see the state of production processes at a glance from a TV, NOC display, shared workstation, or support desk.
- **Reduced downtime impact:** faster detection and triage can reduce the duration and business effect of production issues.
- **Lower operational cost:** less time spent searching for problems means more time spent solving them.
- **Fewer missed signals:** DOM monitoring can raise native visual and audible alerts from pages that were previously only passively watched.
- **Improved escalation quality:** when an issue is visible and centralized, support can provide clearer information to application, infrastructure, network, or management teams.
- **Consistent monitoring behavior:** JSON-driven configuration and import/export make it easier to standardize wallboard setups across machines or teams.
- **No heavy platform dependency:** the app runs as a native Windows desktop tool and can monitor existing web pages without requiring those systems to be redesigned.
- **Better production support:** critical processes stay visible, making the support team more proactive and less dependent on user-reported failures.

## Business Benefits

NetWatch Lite Wallboard is more than a screen with web pages. It is an operational awareness tool that can help reduce cost, improve service, and strengthen production support.

Potential benefits include:

- **Reduced mean time to detect:** teams can discover issues earlier because the wallboard continuously displays and checks key operational pages.
- **Reduced mean time to respond:** alerts and status changes are surfaced directly in the wallboard, helping the team move from detection to action faster.
- **Reduced manual monitoring effort:** support analysts spend less time opening pages one by one and more time focusing on resolution.
- **Improved service continuity:** earlier response can help prevent small issues from becoming larger service interruptions.
- **Improved accountability:** centralized visibility makes it clearer what was seen, when it changed, and which system needs attention.
- **Improved team confidence:** operators have a dedicated tool designed around the way support teams actually watch production systems.
- **Reusable configuration:** import/export allows configurations to be backed up, shared, moved, and restored quickly.

## TSG Helping The Operation

This project positions TSG as a proactive partner to the operation.

Instead of waiting for a call, email, escalation, or production complaint, TSG can use the wallboard to watch key systems continuously and react as soon as indicators change. That creates a stronger support model:

- TSG detects issues earlier.
- TSG communicates with better context.
- TSG helps production recover faster.
- TSG reduces avoidable downtime.
- TSG supports critical operational processes with a practical, low-friction tool.

For teams competing to present improvement projects, this is the core value: NetWatch Lite Wallboard turns passive monitoring into active operational awareness. It helps the support team become faster, more visible, more preventive, and more valuable to the business.

## Screenshots

NetWatch Lite Wallboard is designed for operations rooms, TVs, and support teams that need several live pages visible at the same time.

### 1 Panel Focus Mode

Use this layout when one application or dashboard needs full attention.

![NetWatch Lite Wallboard one panel layout](docs/images/wallboard-one-panel.png)

### 2 Panel Split View

Use this layout to compare two operational systems side by side.

![NetWatch Lite Wallboard two panel layout](docs/images/wallboard-two-panels.png)

### 4 Panel NOC View

Use this layout for a classic wallboard view with multiple web applications on one screen.

![NetWatch Lite Wallboard four panel layout](docs/images/wallboard-four-panels.png)

## Project Structure

```text
netwatch-lite-wallboard/
|-- Assets/
|   `-- netwatch-lite.ico
|-- Data/
|   `-- wallboard.json
|-- docs/
|   |-- developer-guide.md
|   |-- scraping-test-page.html
|   `-- images/
|       |-- wallboard-four-panels.png
|       |-- wallboard-one-panel.png
|       `-- wallboard-two-panels.png
|-- src/
|   `-- NetWatchLite.Wallboard.WebView2/
|       |-- NetWatchLite.Wallboard.WebView2.csproj
|       |-- DiagnosticsForm.cs
|       |-- Program.cs
|       |-- SettingsForm.cs
|       |-- WallboardConfigReader.cs
|       |-- WallboardConfiguration.cs
|       |-- WallboardForm.cs
|       |-- WallboardPanel.cs
|       `-- WebViewPanelControl.cs
|-- CHANGELOG.md
|-- LICENSE
|-- netwatch-lite-wallboard.slnx
`-- README.md
```

## Runtime Flow

The application starts in `Program.Main`, creates `WallboardForm`, initializes a shared WebView2 environment, loads configuration, and renders the visible panels.

```text
Program.Main
  |
  v
WallboardForm
  |
  | loads and normalizes wallboard.json
  v
WallboardConfigReader
  |
  v
WallboardConfiguration + WallboardPanel models
  |
  v
WebViewPanelControl for each visible panel
  |
  v
Microsoft Edge WebView2
  |
  | optional ExecuteScriptAsync DOM checks
  v
Native alarm banner and sound
```

The main form owns page layout, rotation, fullscreen mode, and global refresh. Each `WebViewPanelControl` owns one browser instance, its panel refresh timer, its DOM alarm polling timer, and its native alarm banner.

## Configuration File

The application reads `wallboard.json` from the same folder as `NetWatch-Lite-Wallboard.exe`. During development, it falls back to `Data/wallboard.json`.

Use the top-bar **Settings** button or press `C` to edit the wallboard visually. The settings window can change the wallboard title, default layout, alarm sound, rotation options, panel list, panel order, panel refresh intervals, and advanced monitoring JSON.

The settings window also includes:

- **Export JSON** to save the currently edited configuration to any JSON file.
- **Import JSON** to load a configuration backup into the editor. Imported settings are not written to the active `wallboard.json` until **Save Changes** is pressed.
- A basic monitoring rule builder inside the monitoring editor for creating selector, type, text, severity, details, and sound settings without writing JSON by hand.

When **Save Changes** is pressed, the application:

- Applies the currently visible panel editor values.
- Normalizes timing, layout, panel, and monitoring values.
- Creates `wallboard.backup.json` beside the active JSON file when an existing file is present.
- Writes `wallboard.json` through a temporary file.
- Reads the saved file back and verifies the write.
- Reloads the wallboard after the settings dialog closes.

## Configuration Example

```json
{
  "appTitle": "NetWatch Lite Wallboard",
  "rotationEnabled": false,
  "rotationSeconds": 20,
  "defaultLayout": 4,
  "alarmSound": "Exclamation",
  "severityColors": {
    "critical": "#CC1220",
    "warning": "#CC6700",
    "info": "#005C8A"
  },
  "panels": [
    {
      "name": "Operations Overview",
      "url": "https://monitoring.example.com/ssla/overview.php",
      "refreshSeconds": 10,
      "monitoring": {
        "enabled": true,
        "pollSeconds": 3,
        "soundEnabled": true,
        "repeatSoundSeconds": 5,
        "rules": [
          {
            "name": "PLC Alarm Modal",
            "type": "domText",
            "selector": "#divAlarm.on .alarmTitle",
            "contains": "PLC Alarm Detected",
            "severity": "critical",
            "detailsSelector": "#divAlarm.on .alarmDiv"
          },
          {
            "name": "Health Red Indicator",
            "type": "exists",
            "selector": ".overviewHealth .ledRED, #divHealth .ledRED",
            "severity": "warning",
            "detailsSelector": ".overviewHealth .ledRED, #divHealth .ledRED",
            "soundEnabled": false
          }
        ]
      }
    }
  ]
}
```

## Top-Level JSON Fields

| Field | Type | Meaning |
|---|---|---|
| `appTitle` | string | Text shown in the center of the top bar. Empty values become `NetWatch Lite Wallboard`. |
| `rotationEnabled` | boolean | Enables automatic page rotation when there are more panels than the current layout can display. |
| `rotationSeconds` | number | Seconds between page rotations. Values less than or equal to zero become `20`. |
| `defaultLayout` | number | Number of panels visible at once. Supported values are `1`, `2`, `3`, `4`, `6`, and `8`. Unsupported values become `4`. |
| `alarmSound` | string | Built-in Windows sound used for audible alarms. Supported values are `Exclamation`, `Asterisk`, `Beep`, `Hand`, and `Question`. Unsupported values become `Exclamation`. |
| `severityColors` | object | Alarm colors for `critical`, `warning`, and `info` severities, stored as `#RRGGBB` values. Invalid colors fall back to safe defaults. |
| `panels` | array | Ordered list of panels. The order controls both grid placement and rotation order. |

## Panel JSON Fields

| Field | Type | Meaning |
|---|---|---|
| `name` | string | Panel title shown in the panel title bar. Empty names become `Monitoring Panel`. |
| `url` | string | Absolute HTTP/HTTPS URL, absolute `file:///` URL, or local packaged path. |
| `refreshSeconds` | number | Independent panel refresh interval. Values less than or equal to zero become `30`. |
| `monitoring` | object or null | Optional DOM monitoring configuration for this panel. Omit it or set it to `null` to disable DOM alarms. |

Panel URLs can be:

- Absolute HTTP/HTTPS URLs, for example `https://example.com/status`.
- Absolute local file URLs, for example `file:///C:/Tools/wallboard/scraping-test-page.html`.
- Relative local URLs, for example `docs/scraping-test-page.html`, when you ship local files beside the executable or run from the development tree.
- Root-relative local URLs, for example `/status/index.html`, when you ship local static files in `wwwroot` beside the executable or in the development tree.

## DOM Monitoring JSON

The optional `monitoring` block enables native wallboard alarms based on elements found in the loaded WebView2 page.

| Field | Type | Meaning |
|---|---|---|
| `enabled` | boolean | Enables or disables DOM polling for this panel. |
| `pollSeconds` | number | Seconds between DOM checks. Runtime normalization clamps this to `1` through `300`. |
| `soundEnabled` | boolean | Master sound switch for this panel's monitoring alarms. |
| `repeatSoundSeconds` | number | Minimum seconds between repeated alarm sounds. Runtime normalization clamps this to `1` through `300`. |
| `rules` | array | List of CSS-selector-based rules evaluated against the rendered DOM. |

Each rule supports:

| Field | Type | Meaning |
|---|---|---|
| `name` | string | Friendly rule name shown in the native alarm banner. |
| `type` | string | Rule type. Supported values are `exists`, `domText`, and `domClass`. |
| `selector` | string | CSS selector used with `document.querySelectorAll`. This is required. |
| `contains` | string or null | Optional case-insensitive text that must appear in the selected element's `textContent`. |
| `severity` | string | `critical`, `warning`, or `info`. Unknown values become `warning`. |
| `detailsSelector` | string or null | Optional CSS selector used to collect detail text for the alarm banner. If omitted, matched elements provide the details. |
| `soundEnabled` | boolean or null | Optional per-rule sound override. `false` prevents that rule from contributing to sound. |

### Basic Rule Builder

Open **Settings**, select a panel, and choose **Edit JSON**. The top of the dialog contains a basic rule builder with these fields:

- `Name`: friendly alarm name.
- `Type`: `exists`, `domText`, or `domClass`.
- `Selector`: required CSS selector.
- `Text contains`: optional text match.
- `Severity`: `warning`, `critical`, or `info`.
- `Details selector`: optional selector used for banner detail text.
- `Sound`: when unchecked, the generated rule sets `soundEnabled` to `false`.

Press **Add Rule** to append the rule to the JSON. The JSON remains editable for advanced adjustments.

Press **Validate** to parse the monitoring JSON and perform a local selector screening pass. This catches common mistakes such as empty selectors, unbalanced brackets, unclosed quotes, trailing combinators, and malformed `detailsSelector` values. Runtime matching still depends on the loaded page DOM.

### How DOM Monitoring Works

1. WebView2 finishes navigating to the panel URL.
2. If `monitoring.enabled` is true, the panel starts a timer using `pollSeconds`.
3. On each tick, C# serializes the panel's rules and injects them into a JavaScript function.
4. WebView2 runs that function inside the already-rendered page with `ExecuteScriptAsync`.
5. JavaScript calls `document.querySelectorAll(selector)` for every rule.
6. Invalid selectors are ignored safely.
7. Only visible elements are considered. Elements hidden by `display: none`, `visibility: hidden`, `opacity: 0`, or without client rectangles are ignored.
8. If `contains` is present, the rule compares normalized element text case-insensitively.
9. Matching rules return alarm details to C# as JSON.
10. C# displays the native alarm banner, chooses the highest severity, pulses the border/banner color, and plays sound if enabled.

The app is checking the final page DOM, not the raw HTML response. This means it can detect content created by client-side JavaScript, opened modals, changed CSS classes, and status LEDs that appear after the page has loaded.

Panels with DOM monitoring enabled show a **Stop Scraping** button in the panel title bar. Press it to pause selector checks for that panel without changing `wallboard.json` or stopping page refreshes. Press **Start Scraping** to resume monitoring.

The panel status label shows scraping state while monitoring is enabled, including `Scraping active`, `Scraping checked`, and `Scraping stopped` messages.

### Local Scraping Test Page

The repository includes a standalone basic test dashboard at [docs/scraping-test-page.html](docs/scraping-test-page.html). Use it to validate DOM monitoring rules before pointing the wallboard at a real production page.

The page keeps the first scraping test intentionally simple:

- A large `Normal`, `Warning`, or `Critical` state.
- Three buttons that immediately change the visible DOM.
- Simple selectors for warning and critical alarms.
- Custom DOM targets with editable id, classes, `data-alarm`, and text.
- Recommended monitoring JSON shown directly in the page.

To add it as a panel during development, create a panel with the packaged local path:

```text
docs/scraping-test-page.html
```

Then open **Settings**, select that panel, choose **Edit JSON**, and paste the recommended monitoring JSON shown inside the test page. The default development configuration already includes matching rules for the bundled test page.

Changes made inside the test page update the live DOM immediately, so the wallboard scraping timer can detect them without rebuilding the application. The custom target section is meant as a small bridge toward future pages where alarms may depend on different ids, classes, attributes, or visible text.

## Error Logs

Unexpected UI, WebView2, configuration, timer, and background task errors are written to:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log
```

If the app shows an unexpected error dialog, check this file first. It includes the timestamp, operation context, exception type, message, and stack trace.

### Example Rule: PLC Alarm Modal

```json
{
  "name": "PLC Alarm Modal",
  "type": "domText",
  "selector": "#divAlarm.on .alarmTitle",
  "contains": "PLC Alarm Detected",
  "severity": "critical",
  "detailsSelector": "#divAlarm.on .alarmDiv"
}
```

This rule means:

- Find visible `.alarmTitle` elements inside `#divAlarm` when `#divAlarm` also has the `on` class.
- Require the element text to contain `PLC Alarm Detected`.
- Raise a `critical` alarm.
- Show detail text collected from `#divAlarm.on .alarmDiv`.

### Example Rule: Red Health Indicator

```json
{
  "name": "Health Red Indicator",
  "type": "exists",
  "selector": ".overviewHealth .ledRED, #divHealth .ledRED",
  "severity": "warning",
  "detailsSelector": ".overviewHealth .ledRED, #divHealth .ledRED",
  "soundEnabled": false
}
```

This rule means:

- Find any visible red LED indicator under either `.overviewHealth` or `#divHealth`.
- Raise a `warning` alarm when at least one matching element exists.
- Show details from the matching red indicator elements.
- Keep the visual alarm active, but do not play sound because this rule disables sound.

## Alert Sound

The alert sound can be selected in **Settings** with the **Alarm sound** dropdown. The selected value is saved as the top-level `alarmSound` field in `wallboard.json`.

Supported built-in Windows sounds:

- `Exclamation`
- `Asterisk`
- `Beep`
- `Hand`
- `Question`

The code maps the saved value to `SystemSounds` in [src/NetWatchLite.Wallboard.WebView2/WebViewPanelControl.cs](src/NetWatchLite.Wallboard.WebView2/WebViewPanelControl.cs):

```csharp
SystemSounds.Exclamation.Play();
```

For a custom `.wav` file, the source can be extended to use `SoundPlayer` instead:

```csharp
using var player = new SoundPlayer("C:\\Path\\To\\alarm.wav");
player.Play();
```

The JSON still uses `monitoring.soundEnabled` and per-rule `soundEnabled` to decide whether a matching alarm may play sound. `alarmSound` only chooses which built-in Windows sound is played when sound is allowed.

Settings also includes:

- **Preview** to play the selected sound without saving.
- **Test Alarm** to show a local visual alarm preview and play the selected sound.
- Severity color pickers for `Critical`, `Warning`, and `Info` alarm banners.

Severity colors are saved in `severityColors` and apply to all panels.

## Diagnostics

The top bar includes a **Diagnostics** button. It opens a read-only diagnostics page with:

- Active `wallboard.json` path.
- Error log path.
- App version and process ID.
- WebView2 user data path.
- Current layout and page.
- Configured panel count and visible panel count.
- Panels with DOM monitoring enabled.
- Alarm sound and severity colors.
- Panel URLs, refresh intervals, polling intervals, and rule counts.

Use Diagnostics when the wallboard does not appear to be reading the expected JSON, when a local page is not loading, or when support needs a quick runtime snapshot.

## Layouts

| Layout | Grid | Typical Use |
|---|---|---|
| `1` | `1x1` | One focused operational screen. |
| `2` | `2x1` | Two large side-by-side panels. |
| `3` | `3x1` | Three wide panels on large displays. |
| `4` | `2x2` | Standard NOC grid. |
| `6` | `3x2` | Dense TV dashboards. |
| `8` | `4x2` | Ultrawide or high-density wallboards. |

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `F` | Toggle fullscreen. |
| `R` | Refresh visible panels. |
| `C` | Open settings. |
| `ESC` | Exit fullscreen when fullscreen is active; otherwise close the application. |

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

The repository does not include portable ZIP files. Build the Windows x64 portable package locally with:

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
|-- NetWatch-Lite-Wallboard.exe
|-- wallboard.json
`-- runtime dependencies...
```

Run:

```powershell
.\NetWatch-Lite-Wallboard.exe
```

Use the Settings window or edit `wallboard.json` beside the executable to change panels without recompiling.

## Operational Notes

- Use `defaultLayout: 1` for one large focus panel.
- Use `defaultLayout: 2` or `3` for large side-by-side panels.
- Use `defaultLayout: 4` for a 2x2 NOC screen.
- Use `defaultLayout: 6` or `8` for dense TV or ultrawide monitoring walls.
- Keep `refreshSeconds` reasonable for internal monitoring pages.
- Keep `pollSeconds` reasonable for DOM monitoring rules. Very low values make checks more frequent.
- WebView2 loads pages as native browser views, so pages that fail inside iframe-based dashboards usually work here.
- Some authentication flows may require interactive login in the WebView2 session.
- Authentication cookies and WebView2 user data are stored under `%LOCALAPPDATA%\NetWatchLite\WallboardWebView2`.
- Unexpected errors are logged under `%LOCALAPPDATA%\NetWatchLite\WallboardLogs`.

## Developer Documentation

See [docs/developer-guide.md](docs/developer-guide.md) for a deeper explanation of code structure, configuration normalization, settings editing, WebView2 hosting, DOM monitoring, and alarm rendering.

## License

NetWatch Lite Wallboard is released under the [MIT License](LICENSE). You can use, copy, modify, merge, publish, distribute, sublicense, and sell copies of the software under the license terms.
