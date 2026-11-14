# Configuration

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

NetWatch Lite Wallboard uses JSON files instead of `.env` files. This document is the configuration contract for operators, developers, and release validation.

## File Selection

Unreleased Settings clarifies the existing contract: **Editing profile** identifies the save target; selecting another profile produces a pending switch and disables Save until switching or reselecting the editing profile. **Save to** is the writable profile path, which can differ from the loaded fallback file. Its folder command opens the directory only. No remembered-profile file, new JSON field, migration or precedence change is introduced.

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

Settings also lists the default, existing custom profiles, and the currently selected profile. Choose a profile and click **Switch Profile**. Switching is session-only: it does not change launch arguments, environment variables, JSON, or browser data. Unsaved edits require explicit discard confirmation (default No); save first to retain them. Explicit UI selection requires an existing readable JSON object and never falls back to another profile on a missing/malformed file. Startup CLI aliases, normalization, fallback and save/backup paths are unchanged. Backup files are not offered as profiles.

Organized portables keep all `wallboard*.json` files at the root, beside the launcher, not inside `app/`. Both loading and saving use that root regardless of the working directory. Old flat packages keep their original paths; development still uses `Data/`. A missing selected profile loads the default file if available but saves to its own `wallboard.{environment}.json`, never silently into the default profile. Backups stay beside the active JSON.

Updating a portable does not migrate or overwrite existing files automatically. Extract into a new folder and copy the profiles/local pages you want to retain, as described in [Deployment](DEPLOYMENT.md). Browser cookies and authentication remain in their existing LocalAppData location.

Startup integrity validation deliberately does not read, hash, normalize, or write `wallboard*.json`, including custom profiles and backups. Existing application behavior for missing/malformed JSON remains unchanged. The JSON files inside `app/` are runtime metadata, not editable wallboard settings; they and the root `portable-layout.json` are protected. See [Portable integrity](PORTABLE_INTEGRITY.md).

## Top-Level Fields

| JSON field | Type | Default | Normalized range or values |
|---|---:|---:|---|
| `appTitle` | string | `NetWatch Lite Wallboard` | Non-empty text |
| `homePageEnabled` | boolean | `false` | Opt-in Inicio/Wallboard switching; requires a valid URL |
| `homePageUrl` | string | empty | Existing trusted web/file/local-page URL forms; trimmed, invalid becomes empty |
| `startupView` | string | `wallboard` | `wallboard` or `home`, case-insensitive; Home requires feature and URL |
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
| `memoryProtectionEnabled` | boolean | `false` | Explicit opt-in; old JSON remains advisory |
| `memoryProtectionSustainedMinutes` | integer | `5` | `1` to `60` continuous minutes above the warning threshold |
| `memoryProtectionCooldownMinutes` | integer | `30` | `5` to `1440` minutes before tracking another recovery |
| `autoHideTopBarEnabled` | boolean | `false` | `true` or `false` |
| `theme` | string | `dark` | `dark`, `light`, or `high-contrast` |
| `panels` | array | empty | List of panel objects |

## View Selection

Optional field excerpt, not a replacement for your profile:

```json
{
  "homePageEnabled": true,
  "homePageUrl": "https://example.org/",
  "startupView": "home"
}
```

Keep your existing title, panels and settings. See the complete [home-view fixture](../tests/fixtures/wallboard-home.json). Existing `Data/` profiles are deliberately unchanged to preserve bytes/behavior. Startup never migrates or saves them.

Invalid/missing URLs disable Inicio and normalize startup to `wallboard`, without substituting a sample URL. Valid URLs can be retained while disabled. The reader retains the existing panel URL contract. Settings applies the editor-only input rules below; these do not migrate existing files or silently reinterpret stored URLs at startup.

The runtime selection is session-only. Startup applies at launch or explicit profile switch; saving/reloading the same profile preserves Inicio if still valid. Disabling Inicio returns to Wallboard. Mode-only switches do not save, clear browser data, reset recovery budgets or alter the last wallboard layout/page. Configuration reload keeps its existing layout/page reset.

Inicio disables wallboard rotation/restart/refresh/recovery and automatic memory recovery. Bounded memory sampling can continue. Home has manual refresh/retry only. Low-power minimize releases the selected view; restore recreates that same mode. Returning to Wallboard starts fresh rotation/restart intervals, not catch-up ticks. Fullscreen, close confirmation and auto-hide are independent.

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

## URL Input In Settings

New or changed Home/panel input such as `www.google.com` or `example.org/status` is completed to HTTPS when validated. Explicit `http://` remains HTTP; domains, commas and other typing errors are never guessed or silently repaired. Web hosts are validated using `Uri`; `www.google,com`, unsupported schemes and malformed HTTP/HTTPS addresses block Save with a warning and focus on the field. The check does not prove network reachability or repair TLS/site errors.

Existing unchanged values retain their original interpretation after validation, including relative paths with dotted directory names. No completion is applied by the configuration reader, import cloning or startup. Saving still requires explicit operator action and uses the same active profile/backup path. Disabled Home retains its value without forcing URL completion.

File URLs, root-relative local paths, nested local paths and common local page filenames such as `home.html` remain supported. For an ambiguous new dotted filename/directory that looks like a web host, use an explicit `file:///` or documented root-relative local path. URL text is not rewritten on each typing event; accepted completion is shown during explicit validation. No new JSON field or browser profile is introduced.

## Validation

The visual Settings window is the preferred editor. It validates common values and saves through the same normalization path used at startup.

When JSON contains a zero or negative `rotationSeconds`, the reader uses `20`. When a panel contains a zero or negative `refreshSeconds`, the reader uses `30`.

Positive rotation and refresh intervals are capped at `2147483` seconds to prevent overflow when converted to a Windows timer interval. The visual editor still uses `1` through `3600`.

Recovery uses three attempts after 5, 15, and 30 seconds. The budget survives automatic control replacements and is restored after a healthy minute or a manual Refresh/Restart. Old JSON files without `automaticRecoveryEnabled` load with recovery enabled. Set it to `false` to retain manual-only recovery.

The memory warning threshold applies to estimated combined host/WebView2 working sets. It remains advisory unless `memoryProtectionEnabled` is explicitly enabled. Protection samples even when the visual monitor is hidden, ignores partial samples, resets tracking after gaps over 90 seconds/backward clock changes, and does not recover while minimized, editing Settings, rendering, closing, or in low-power mode. Complete high samples must span the sustained interval; cooldown must expire before a new interval begins. At most two recovery attempts are reserved per application session, including failed attempts; disabling/reloading/switching profiles does not reset that budget. A new application session restores it. Recovery recreates visible controls through the existing serialized render path; it never kills browser processes or deletes cookies/profiles and cannot guarantee all browser memory is freed. See [Operations](OPERATIONS.md).

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
