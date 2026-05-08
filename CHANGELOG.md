# Changelog

All notable changes to NetWatch Lite Wallboard are documented here.

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
