# Configuration

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

NetWatch Lite Wallboard uses JSON files instead of `.env` files. This document is the configuration contract for operators, developers, and release validation.

## File Selection

The default file is:

```text
wallboard.json
```

An environment profile uses:

```text
wallboard.{environment}.json
```

Select a profile with:

```powershell
.\NetWatch-Lite-Wallboard.exe --environment production
```

During development:

```powershell
dotnet run --project .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj -- --environment demo
```

The `NETWATCH_WALLBOARD_ENV` environment variable is also supported. A command-line value takes precedence.

## Top-Level Fields

| JSON field | Type | Default | Normalized range or values |
|---|---:|---:|---|
| `appTitle` | string | `NetWatch Lite Wallboard` | Non-empty text |
| `rotationEnabled` | boolean | `false` | `true` or `false` |
| `rotationSeconds` | integer | `20` | Positive integer; visual editor range `1` to `3600` |
| `defaultLayout` | integer | `4` | `1`, `2`, `3`, `4`, `6`, or `8` |
| `lowPowerModeEnabled` | boolean | `true` | `true` or `false` |
| `autoRestartEnabled` | boolean | `false` | `true` or `false` |
| `autoRestartHours` | integer | `6` | `1` to `24` |
| `startFullscreen` | boolean | `false` | `true` or `false` |
| `confirmExit` | boolean | `false` | `true` or `false` |
| `memoryMonitorEnabled` | boolean | `false` | `true` or `false` |
| `memoryWarningMegabytes` | integer | `1500` | `256` to `65536` |
| `autoHideTopBarEnabled` | boolean | `false` | `true` or `false` |
| `theme` | string | `dark` | `dark`, `light`, or `high-contrast` |
| `panels` | array | empty | List of panel objects |

## Panel Fields

| JSON field | Type | Meaning |
|---|---:|---|
| `name` | string | Label shown above the page. |
| `url` | string | Web URL or trusted local file/path. |
| `refreshSeconds` | integer | Positive automatic refresh interval; visual editor range `1` through `3600`. |

Supported URL forms include:

- `https://example.com/status`
- `http://example.com/status`
- `file:///C:/Tools/status.html`
- `pages/status.html`
- `/status/index.html`

## Compatibility Rules

- Missing fields receive safe defaults.
- Unknown fields are ignored by the current reader.
- New fields must be optional and have defaults.
- Existing field meaning must not change silently.
- A breaking schema change requires an ADR, migration notes, a major version decision, and contract tests.

## Validation

The visual Settings window is the preferred editor. It validates common values and saves through the same normalization path used at startup.

When JSON contains a zero or negative `rotationSeconds`, the reader uses `20`. When a panel contains a zero or negative `refreshSeconds`, the reader uses `30`.

For manual JSON edits:

1. Keep valid JSON syntax.
2. Do not place comments inside JSON.
3. Start the app and open Diagnostics.
4. Confirm the active profile and file path.
5. Verify every panel and runtime option.

## Secrets

Do not store credentials, tokens, cookies, or API keys in configuration. Authentication should happen inside the loaded website through its normal browser flow.

The project intentionally does not include `.env.example`: no `.env` file is consumed by the application.

---
[Previous: User guide](user-guide.md) | [Documentation home](README.md) | [Next: Troubleshooting](TROUBLESHOOTING.md)
