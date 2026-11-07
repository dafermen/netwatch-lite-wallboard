# Deployment

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

NetWatch Lite Wallboard is distributed as a portable Windows folder and ZIP. It is not installed as a Windows service and does not require a web server.

At the user's explicit request, `0.4.0-rc.3` is provided as an evaluation portable while long-session and full acceptance checks remain pending. Its [validation record](releases/v0.4.0-rc.3.md) is not stable-release approval. Previous `publish/` and rc.2 packages are retained and are not moved or overwritten. Stable publication still requires the prerequisites below.

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

1. Reads the full project version, including prerelease suffixes, unless `-Version` overrides both the package name and published version.
2. Refuses existing output paths and redirected artifact directories; never deletes an existing deployment.
3. Runs self-contained `dotnet publish` for `win-x64` inside a fresh `artifacts/.staging/` folder, without trimming or single-file extraction.
4. Moves only freshly published JSON examples to the package root. DLLs and .NET 10 stay in `app/`.
5. Builds a small root launcher using the Windows .NET Framework C# compiler; the launcher forwards all arguments and does not validate per-file hashes.
6. Copies README, licenses, root reference documents, `docs/`, layout metadata, and start instructions.
7. Checks required files, runtime/version metadata, and excluded backups/logs.
8. Creates and extracts the exact ZIP, runs isolated package checks, then promotes the folder and ZIP. `-NoZip` tests a disposable folder copy instead.
9. Writes a SHA-256 sidecar for the final ZIP.

Generated output must not be committed:

```text
artifacts/
|-- portable/NetWatch-Lite-Wallboard-v{version}-win-x64/
|-- packages/NetWatch-Lite-Wallboard-v{version}-win-x64.zip
|-- packages/NetWatch-Lite-Wallboard-v{version}-win-x64.zip.sha256
|-- verification/{run-id}/
`-- .staging/{build-id}/
```

If publishing fails, no new final package is approved; staging/evidence remain for diagnosis. To rebuild an existing version, archive its generated outputs manually first or use a new full version. Do not use an output folder as an operational installation.

## Runtime And Launcher

The real application and .NET 10 Desktop Runtime are inside `app/`. The root executable is a small .NET Framework 4.x launcher, using the Windows component already available on supported machines. It starts the child with the package root as working directory and preserves arguments, quoting, and inherited environment variables. Normal startup returns after handing off; `--package-check` waits and propagates the child's exit code.

`PortablePaths` recognizes `app/` only when its parent contains the product launcher and `portable-layout.json`. Configuration and local content then resolve from the parent, independently of the working directory. Old flat portable and development layouts are still supported. Do not rename/remove the launcher or layout marker.

Bundling .NET removes the separate .NET 10 installation requirement, not Windows or WebView2 requirements. It does not make browser sessions portable: existing LocalAppData paths are unchanged. Rebuild/redeploy with supported .NET security patches; bundled runtimes do not update through the machine's shared .NET installation. See [Microsoft deployment guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/).

## Required Package Contents

- `NetWatch-Lite-Wallboard.exe`
- `app/` containing application assemblies, WebView2 loader files, and the .NET 10 Desktop Runtime
- `portable-layout.json` and `START-HERE.txt`
- `licenses/` containing original bundled .NET runtime/Desktop and WebView2 SDK license/notices
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
2. Confirm WebView2 Runtime and Windows .NET Framework 4.x are available. .NET 10 is included in this new package; rc.1 and older framework-dependent builds retain their own runtime requirements.
3. Extract the entire ZIP to a new stable local folder. Never mix DLLs or extract on top of a working deployment.
4. Back up/export the old configuration. Copy only the JSON profiles you use and your trusted local pages (`pages/`, `wwwroot/`, or other configured paths) to the new root. Do not replace the new `app/` with old binaries. For a first installation, replace the sample URLs with approved URLs.
5. Start the root executable with the intended profile, for example `--environment production`. Confirm the active path in Diagnostics before saving.
6. Verify layouts, authentication, fullscreen behavior, and memory display.
7. Configure Windows startup only after acceptance testing on that computer.

Do not overwrite a working deployment until its current configuration has been exported or backed up.

## Package Tests

```powershell
.\scripts\test-portable.ps1 -ZipPath .\artifacts\packages\NetWatch-Lite-Wallboard-v0.4.0-rc.3-win-x64.zip
```

Tests run in a disposable copy under `artifacts/verification/`, never in the original deployment. They verify required files, the included Desktop Runtime, root/direct-payload startup from a different working directory, argument quoting, default/demo/production/custom profiles, environment-variable precedence and aliases, missing-profile fallback, unchanged JSON hashes, and detection of missing required files. Reports are outside the copied package. No WebView2 instance or user browser profile is opened by these checks.

The application's read-only `--package-check <absolute-new-report.json>` command exists for these deployment tests. It writes runtime/configuration paths and normalized title/count to a new report, refuses to overwrite an existing report, and never saves configuration. Treat reports as local diagnostics and review them before sharing. These checks are not full UI acceptance, startup cryptographic validation, or proof of operation on every Windows machine.

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
