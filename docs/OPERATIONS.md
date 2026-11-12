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

The memory value sums the host working set and distinct process IDs returned by the active WebView2 environment. It does not scan unrelated browser sessions. Shared physical pages may appear in multiple working sets, so the total is approximate. `*` marks incomplete samples. Diagnostics includes the breakdown, process count, lifetime peak, and ten recent samples; history is capped at 1,440 entries.

For failed panels, automatic recovery retries after 5, 15, and 30 seconds. After the limit, use Restart manually and investigate the underlying site, network, or runtime issue. Recovery never scrapes page content or dismisses authentication prompts. Browser-process failures can affect the shared group of visible panels.

### Optional Memory Protection

The default remains warning-only. Enable **Memory protection** in Settings only after choosing a threshold appropriate to the machine and pages. **High for (min)** defaults to five minutes; **Cooldown (min)** defaults to thirty. Complete samples are taken once per minute, and short spikes/incomplete/discontinuous samples do not qualify. Settings/minimization/low-power/rendering pauses tracking. After a qualifying interval, the existing panel restart path is used, with at most two attempts per application session and no budget reset on a profile reload. Failed attempts count. Cookies and browser profiles are never deleted and no processes are force-killed. Shared-browser memory may remain allocated; this is mitigation, not proof that a leak is fixed or a guarantee against Windows resource exhaustion.

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

Use **Diagnostics > Export Report** for the safer support format. It writes a new JSON file, never overwrites an existing one, and refuses the protected portable `app/` directory. The schema allowlists build/runtime/OS versions, numeric panel counts, memory options, up to 120 recent memory samples and 100 typed runtime events. It does not export configuration files, titles, panel names/URLs, profile names, personal paths, cookies, credentials, or raw logs. Review even this report before sharing. Existing on-screen diagnostics and **Copy** remain raw, local details and can include private URLs/paths; they are not the sanitized report.

## Logs

Unexpected errors are written to:

```text
%LOCALAPPDATA%\NetWatchLite\WallboardLogs\wallboard-errors.log
```

The general log is limited to 1 MiB with three numbered backups (`.1` through `.3`), and entries are capped at 32 KiB. Large inputs are truncated before encoding. The first rotation of a legacy oversized log retains only its last 1 MiB; old history beyond the retention limit is discarded. Do not rely on this local log as an audit archive. Logging is best effort if files are locked or access is denied, and refuses redirected managed log paths. The launcher's separate `portable-startup.log` retains its own pre-existing bounds and is not included in support reports. No profile, cookie database, or local page is rotated.

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
