# Changelog

All notable changes to NetWatch Lite Wallboard are documented here.

## v0.2.3 - 2026-06-03

- Expanded the README with a complete explanation of configuration, panel fields, DOM monitoring JSON, selector rules, alarm behavior, build steps, and operational notes.
- Rewrote the developer guide with detailed runtime flow, configuration normalization, settings save behavior, WebView2 hosting, DOM polling, alarm snapshots, and extension points.
- Added detailed source comments across the configuration models, settings editor, configuration reader, main form, WebView panel control, and project file.
- Replaced non-ASCII UI shortcut/refresh symbols in source with ASCII text for easier cross-editor readability.
- Added centralized unexpected-error logging under `%LOCALAPPDATA%\NetWatchLite\WallboardLogs`.
- Hardened WinForms async event handlers, panel rendering, settings save events, DOM polling timers, and WebView2 process failure handling.
- Added settings-window import/export buttons for moving or backing up `wallboard.json`.
- Added a basic monitoring rule builder for creating selector, type, text, severity, details selector, and sound settings without hand-writing JSON.
- Expanded README positioning with operational value, business benefits, and TSG-focused production support messaging.

## v0.2.2 - 2026-05-08

- Made **Save Changes** apply the selected panel editor values before writing `wallboard.json`.
- Renamed panel editor buttons to clearer labels: **New Panel**, **Add Panel**, and **Apply**.
- Added status text for unsaved panel/settings edits.
- Added save verification that reads the JSON back after writing before showing the success message.
- Allowed **Save Changes** to add a completed new panel when the editor has values but no selected row.
- Updated README and developer documentation for the more intuitive save flow.

## v0.2.1 - 2026-05-08

- Increased the top-bar Settings button width so the label is fully visible.
- Made the settings window resizable and maximizable like a standard Windows dialog.
- Updated the settings window to a lighter slate theme for better contrast.
- Styled the panel grid headers, rows, alternating rows, and selection colors for readable text.
- Updated README and developer documentation for the settings window polish.

## v0.2.0 - 2026-05-08

- Added a visual settings window for managing `wallboard.json` inside the app.
- Added panel CRUD actions for adding, updating, duplicating, deleting, and reordering monitoring panels.
- Added editable wallboard title, default layout, rotation toggle, and rotation interval controls.
- Added a top-bar Settings button and `Ctrl+,` keyboard shortcut.
- Added JSON save support with a local `wallboard.backup.json` backup before overwriting.
- Reloaded the wallboard automatically after saving settings.
- Updated README and developer documentation for the visual configuration workflow.

## v0.1.1 - 2026-05-07

- Removed the bundled local sample wallboard HTML.
- Replaced all example configuration values with neutral public placeholders.
- Updated README and developer documentation for public repository use.
- Added a Windows executable icon for portable builds.
- Documented MIT license usage.
- Added 1, 3, 6, and 8 panel layouts alongside the existing 2 and 4 panel layouts.
- Expanded the neutral sample configuration to eight panels for layout and rotation testing.
- Removed repository-hosted portable ZIP files to avoid incorrect small HTML downloads from GitHub.
- Added sanitized README screenshots for 1, 2, and 4 panel wallboard layouts.

## v0.1.0 - 2026-05-07

- Initial standalone Windows WebView2 wallboard release.
- Added JSON-driven panel configuration.
- Added 2-panel and 4-panel layouts.
- Added automatic page rotation.
- Added independent panel refresh timers.
- Added fullscreen and keyboard shortcuts.
- Added neutral example panel configuration.
- Added programmer documentation.
