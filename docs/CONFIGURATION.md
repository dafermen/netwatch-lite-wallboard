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

Organized portables keep all `wallboard*.json` files at the root, beside the launcher, not inside `app/`. Both loading and saving use that root regardless of the working directory. Old flat packages keep their original paths; development still uses `Data/`. A missing selected profile loads the default file if available but saves to its own `wallboard.{environment}.json`, never silently into the default profile. Backups stay beside the active JSON.

Updating a portable does not migrate or overwrite existing files automatically. Extract into a new folder and copy the profiles/local pages you want to retain, as described in [Deployment](DEPLOYMENT.md). Browser cookies and authentication remain in their existing LocalAppData location.

Startup integrity validation deliberately does not read, hash, normalize, or write `wallboard*.json`, including custom profiles and backups. Existing application behavior for missing/malformed JSON remains unchanged. The JSON files inside `app/` are runtime metadata, not editable wallboard settings; they and the root `portable-layout.json` are protected. See [Portable integrity](PORTABLE_INTEGRITY.md).

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
| `automaticRecoveryEnabled` | boolean | `true` | Retry failed panels with bounded delays |
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

Positive rotation and refresh intervals are capped at `2147483` seconds to prevent overflow when converted to a Windows timer interval. The visual editor still uses `1` through `3600`.

Recovery uses three attempts after 5, 15, and 30 seconds. The budget survives automatic control replacements and is restored after a healthy minute or a manual Refresh/Restart. Old JSON files without `automaticRecoveryEnabled` load with recovery enabled. Set it to `false` to retain manual-only recovery.

The memory warning threshold now applies to the estimated combined host and associated WebView2 working sets. It remains advisory and never triggers an automatic restart by itself.

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
