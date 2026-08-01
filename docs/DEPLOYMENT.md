# Deployment

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

NetWatch Lite Wallboard is distributed as a portable Windows folder and ZIP. It is not installed as a Windows service and does not require a web server.

## Release Prerequisites

Before publishing:

- Complete all 13 areas in [TESTING.md](TESTING.md).
- Record results in [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md).
- Resolve every applicable failure.
- Update the project version and `CHANGELOG.md`.
- Start from a clean, reviewed commit.

## Build The Portable Package

```powershell
cd C:\Projects\netwatch-lite-wallboard
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-portable.ps1
```

The script:

1. Reads the version from the project file unless overridden.
2. removes the previous output for that exact version and runtime.
3. runs `dotnet publish` for `win-x64`.
4. copies README and LICENSE.
5. creates a ZIP unless `-NoZip` is supplied.

Generated output is under `publish/` and must not be committed.

## Required Package Contents

- `NetWatch-Lite-Wallboard.exe`
- Application assemblies and WebView2 loader files
- `wallboard.json`
- Supported environment profiles
- `README.md`
- `LICENSE`

The package must not contain:

- `wallboard.backup.json`
- Logs
- Developer configuration with credentials
- `bin/` or `obj/`
- Source-control metadata

## Deployment To A Display Computer

1. Confirm Windows 10 or Windows 11 x64.
2. Confirm .NET 8 Desktop Runtime and WebView2 Runtime are available.
3. Extract the ZIP to a stable local folder.
4. Replace example URLs with approved operational URLs.
5. Start the executable with the intended profile.
6. Verify layouts, authentication, fullscreen behavior, and memory display.
7. Configure Windows startup only after acceptance testing on that computer.

Do not overwrite a working deployment until its current configuration has been exported or backed up.

## Rollback

Keep the previous portable folder and configuration until the new release completes acceptance and soak testing.

To roll back:

1. Close the new version.
2. Restore the previous folder.
3. Restore its compatible configuration.
4. Start it and verify the active version and configuration in Diagnostics.

## GitHub Publication

CI may create a validation artifact, but a successful workflow alone is not release approval. Publish a GitHub release only after the release checklist is approved and the ZIP has been tested as an extracted package.

---
[Previous: Operations](OPERATIONS.md) | [Documentation home](README.md) | [Next: Release checklist](RELEASE_CHECKLIST.md)
