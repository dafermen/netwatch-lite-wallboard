# Architecture

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

This document explains the technical shape of NetWatch Lite Wallboard and the boundaries contributors should preserve.

## System Context

NetWatch Lite Wallboard is a Windows desktop application. It embeds operational web pages in Microsoft Edge WebView2 controls and arranges them in configurable WinForms layouts.

The application:

- Reads a local JSON configuration.
- Creates one WebView2 control for each visible panel.
- Shares one WebView2 environment across panels.
- Refreshes, rotates, restarts, and disposes panels.
- Provides native settings and diagnostics windows.

The application does not expose a network API, host a server, scrape page content, or evaluate DOM selectors.

## Runtime Components

| Component | Responsibility |
|---|---|
| `Program` | Starts WinForms and registers global exception handlers. |
| `WallboardForm` | Owns the main window, layouts, paging, rotation, kiosk behavior, memory display, and application-level actions. |
| `WebViewPanelControl` | Owns one WebView2 control, navigation, refresh timer, panel status, and panel-level restart requests. |
| `WallboardConfigReader` | Locates, reads, normalizes, saves, backs up, and verifies JSON configuration. |
| `WallboardConfiguration` | Defines top-level configuration fields. |
| `WallboardPanel` | Defines one configured panel. |
| `SettingsForm` | Edits a cloned configuration and saves it through `WallboardConfigReader`. |
| `DiagnosticsForm` | Produces a read-only runtime snapshot for support. |
| `ThemePalette` | Centralizes native shell colors. |
| `AppErrorLog` | Writes unexpected failures to the local application log. |
| `PortablePaths` | Keeps editable JSON and local pages at the organized package root while preserving flat/development layouts. |
| Portable launcher | Uses Windows .NET Framework to forward arguments to the self-contained .NET 10 payload in `app/`. |
| `MemoryMonitor` | Counts distinct host/browser process IDs, tolerates exited processes, and retains bounded history and peak measurements. |
| `PanelRecoveryPolicy` | Keeps a bounded retry budget across WebView2 replacements and requires a stable minute before resetting it. |

## Startup Flow

1. `Program.Main` enables WinForms defaults and opens `WallboardForm`.
2. `WallboardForm.InitializeAsync` creates the shared WebView2 environment.
3. `WallboardConfigReader` selects the active environment profile.
4. Configuration is deserialized and normalized.
5. The main form applies theme and runtime options.
6. The current page of panels is created and navigated.
7. Rotation, memory, auto-restart, and top-bar timers are scheduled as configured.

In organized portables, the root launcher first opens `app/NetWatch-Lite-Wallboard.exe`. A read-only `--package-check` path exits before WinForms/WebView2 initialization for isolated packaging tests. It is not a per-file integrity validator. See [ADR 0003](adr/0003-organized-self-contained-portables.md).

## Configuration Flow

Configuration follows this path:

```text
wallboard[.environment].json
        |
        v
WallboardConfigReader
  deserialize + normalize
        |
        v
WallboardConfiguration
        |
        +--> WallboardForm
        +--> SettingsForm clone
        +--> DiagnosticsForm snapshot
```

Settings are edited on a clone. Saving performs normalization, creates a local backup when appropriate, writes the file, reads it again for verification, and then asks the main window to reload.

## WebView2 Lifecycle

All visible panels share one `CoreWebView2Environment` so cookies, authentication, and browser profile data behave like one browser session.

Each panel still owns its own WebView2 control. When layouts change, pages rotate, low-power mode activates, settings reload, or the app closes, panel controls must be disposed. Disposal stops timers, removes event handlers, and releases browser controls. This lifecycle is a critical memory-safety boundary.

## Data And Storage

| Data | Location |
|---|---|
| Source configuration | `Data/wallboard.json` |
| Environment profiles | `Data/wallboard.{environment}.json` |
| Portable configuration/local pages | Package root, outside `app/`; old flat layouts still use the executable directory |
| WebView2 profile | `%LOCALAPPDATA%\NetWatchLite\WallboardWebView2` |
| Error log | `%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log` |
| Generated package | `artifacts/portable/` and `artifacts/packages/`; historical `publish/` is preserved |

Generated output and machine-specific backups are excluded from source control.

## Trust Boundaries

- The native application trusts its local configuration file.
- Every configured URL is external content rendered by WebView2.
- Loaded pages keep their own authentication, scripts, cookies, and security model.
- The app does not inspect or transform page content.
- Local `file:///` URLs should only point to trusted files.
- Configuration and logs must not contain passwords, access tokens, or private keys.

## Extension Rules

The application and test suite target .NET 10 LTS. `global.json` selects a stable 10.0 SDK; CI restores, builds, and tests the solution on Windows. The WebView2 package remains pinned while migrating the framework to reduce simultaneous dependency changes.

Panel recovery timers belong to `WebViewPanelControl`; retry state belongs to the main form's dictionary keyed by panel runtime identity. This prevents a new control from resetting the retry budget. The main form serializes replacements with the existing render semaphore. When a shared browser process exits, its controls are disposed before recreating the environment and reconnecting the visible group. Closing cancels initialization and pending retries; low-power disposal also uses the render lock.

Memory enumeration uses `CoreWebView2Environment.GetProcessInfos()` on the UI thread. Only those process IDs and the host are sampled. History is bounded to avoid introducing a new long-session memory leak. Both memory accounting and retry decisions are independent of UI controls and covered by deterministic tests.

- Put panel-specific behavior in `WebViewPanelControl`.
- Put whole-window behavior in `WallboardForm`.
- Normalize every new configuration field in `WallboardConfigReader`.
- Keep new JSON fields backward-compatible with older files.
- Update Settings, diagnostics, examples, and documentation together.
- Add an Architecture Decision Record when changing a major boundary, dependency, persistence rule, or product direction.

## Related Documents

- [Configuration](CONFIGURATION.md)
- [Development](DEVELOPMENT.md)
- [Operations](OPERATIONS.md)
- [Security](SECURITY.md)
- [Architecture decisions](adr/README.md)

---
[Previous: Release checklist](RELEASE_CHECKLIST.md) | [Documentation home](README.md) | [Next: Developer guide](developer-guide.md)
