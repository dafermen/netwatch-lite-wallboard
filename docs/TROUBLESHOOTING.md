# Troubleshooting

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

Start with the least disruptive action and collect Diagnostics before deleting data or replacing configuration.

## A Panel Is Blank

1. Click Restart on that panel.
2. Open the same URL in Microsoft Edge.
3. Confirm the site is reachable and the session is signed in.
4. Check the panel URL in Settings.
5. Review the error log.

## A Panel Does Not Refresh

- Confirm `refreshSeconds` is at least 1.
- Use Refresh to test immediate navigation.
- Confirm the site is not presenting a login or consent page.
- Restart the panel to recreate its WebView2 control.

## Memory Keeps Growing

1. Record process memory and browser-process count.
2. Click Restart All and observe whether memory improves.
3. Test each configured site in Edge to identify unusually heavy pages.
4. Increase refresh intervals.
5. Enable scheduled restart and the memory monitor.
6. Run the performance checks in `docs/TESTING.md`.

Sustained growth after repeated panel disposal is a defect candidate. Include uptime, layout, URLs or sanitized equivalents, and measurements in the report.

## Settings Do Not Apply

- Click Apply for the selected panel before saving when editing panel details.
- Click Save Changes.
- Confirm the active configuration path in Diagnostics.
- Confirm another portable copy is not still running.
- Confirm the file is writable.
- Reload JSON from disk and compare values.

## The Wrong Profile Opens

- Check the command line for `--environment`.
- Check `NETWATCH_WALLBOARD_ENV`.
- Confirm `wallboard.{environment}.json` exists beside the executable or in the development data location.
- Use Diagnostics to see the selected environment and exact path.

## Fullscreen Or Top Bar Behavior Is Unexpected

- Press `F` to toggle fullscreen.
- Move the pointer to the top edge to reveal an auto-hidden top bar.
- Open Settings with `C`.
- Verify `startFullscreen`, `confirmExit`, and `autoHideTopBarEnabled`.

## The Portable Package Does Not Start

- Extract the ZIP before running it.
- Confirm Windows is x64 and supported.
- Install or repair the .NET 10 Desktop Runtime for the updated build. The SDK is needed only for development.
- Install or repair Microsoft Edge WebView2 Runtime.
- Avoid running from a network share during diagnosis.
- Check the error log under `%LOCALAPPDATA%\NetWatchLite\WallboardLogs`.

## Build Warning MSB3277

The current project may report `MSB3277` involving `WindowsBase` and WebView2/WPF references. The v0.3.0 build was known to succeed with this warning and zero errors.

Treat any new warning or build error separately. Do not suppress the known warning without understanding the dependency conflict.

## Information For A Support Report

- App version and commit.
- Windows edition and version.
- WebView2 Runtime version.
- Active profile and sanitized configuration.
- Layout and panel count.
- Time running before the issue.
- Memory measurements.
- Diagnostics output and relevant log lines.
- Reproduction steps.

---
[Previous: Configuration](CONFIGURATION.md) | [Documentation home](README.md) | [Next: Operations](OPERATIONS.md)
