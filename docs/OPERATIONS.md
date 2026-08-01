# Operations

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

This document is for the person responsible for keeping a wallboard healthy during long-running use.

## Daily Checks

- Confirm every visible panel loaded successfully.
- Confirm the active layout and profile in Diagnostics.
- Check the memory indicator when enabled.
- Confirm rotation is progressing when enabled.
- Use Refresh for normal content reload and Restart for a stuck browser panel.

## Long-Running Sessions

Recommended baseline:

- Enable the memory monitor.
- Set a memory threshold appropriate for the display computer.
- Enable scheduled panel restart for wallboards that run continuously.
- Enable low-power mode if the app is regularly minimized.
- Increase refresh intervals for heavy dashboards that do not need frequent reloads.

The memory value is an approximate process working set, not a complete measurement of every WebView2 subprocess. Windows Task Manager remains the source for full process-level investigation.

## Recovery Actions

| Symptom | First action | Next action |
|---|---|---|
| One page is stale | Refresh that panel | Restart that panel |
| One page is blank or stuck | Restart that panel | Verify the URL in Edge |
| Several panels are stale | Refresh All | Restart All |
| Memory warning appears | Restart All | Identify a heavy page and review refresh intervals |
| Window is unresponsive | Wait for current navigation | Close and restart the app, then inspect logs |
| Repeated browser failure | Test the URL in Edge | Update WebView2 Runtime and collect Diagnostics |

## Diagnostics Collection

Collect:

- App version.
- Active environment and configuration path.
- Process ID and working set.
- WebView2 data path.
- Panel names and URLs.
- Current layout and page.
- Error log.
- Windows and WebView2 Runtime versions.
- Approximate time the app had been running.

Do not share credentials, session cookies, or private URL query parameters in public tickets.

## Logs

Unexpected errors are written to:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log
```

WebView2 browser data is stored under:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardWebView2
```

Close the app before archiving or removing its browser data. Removing browser data signs the wallboard out of websites and should be treated as a recovery action, not routine maintenance.

## Configuration Backup

Use Export JSON before major changes. Store backups outside the application folder if that folder may be replaced during deployment.

After importing a configuration, verify it in Settings and click Save Changes. Import alone does not overwrite the active file.

## Operational Security

- Use a dedicated Windows account for shared displays.
- Apply least privilege.
- Keep Windows, Edge WebView2 Runtime, and the application updated.
- Do not embed credentials in URLs.
- Restrict physical and remote access to kiosk computers.
- Review loaded sites as trusted external content.

---
[Previous: Troubleshooting](TROUBLESHOOTING.md) | [Documentation home](README.md) | [Next: Deployment](DEPLOYMENT.md)
