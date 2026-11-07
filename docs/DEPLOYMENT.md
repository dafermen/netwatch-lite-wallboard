# Deployment

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

NetWatch Lite Wallboard is distributed as a portable Windows folder and ZIP. It is not installed as a Windows service and does not require a web server.

At the user's explicit request, `0.4.0-rc.4` is provided as an [evaluation prerelease with ZIP and checksum](https://github.com/dafermen/netwatch-lite-wallboard/releases/tag/v0.4.0-rc.4) while long-session and full acceptance checks remain pending. Its [validation record](releases/v0.4.0-rc.4.md) is not stable-release approval. All previous packages are retained and are not moved or overwritten. Stable publication still requires the prerequisites below.

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
5. Builds a root launcher using the Windows .NET Framework C# compiler, embedding the manifest of the finalized payload and layout. It validates before forwarding arguments to the application.
6. Copies README, licenses, root reference documents, `docs/`, layout metadata, and start instructions.
7. Checks required files, runtime/version metadata, and excluded backups/logs.
8. Creates and extracts the exact ZIP, runs isolated package/startup-integrity checks including deliberately damaged disposable copies, then promotes the folder and ZIP. `-NoZip` tests a disposable folder copy instead.
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

## Portable History Policy

Every new portable delivered after a build must have both its extracted version under `artifacts/portable/` and its versioned ZIP plus `.zip.sha256` under `artifacts/packages/`, within this same project. Retain older versions without overwriting them. The default script already creates and tests the ZIP; `-NoZip` is for preliminary validation or an explicit exception, not final delivery. A development-only `dotnet build` does not create a distributable package.

The local ZIP is also the binary intended for an approved GitHub publication. Pushing source does not upload it: attach the tested ZIP and checksum to the corresponding GitHub Release or explicitly authorized evaluation prerelease, following the publication rules below. Do not commit generated archives, configurations from working installations, or browser profiles into Git history.

## Runtime And Launcher

The real application and .NET 10 Desktop Runtime are inside `app/`. The root executable is a small .NET Framework 4.x launcher, using the Windows component already available on supported machines. It starts the child with the package root as working directory and preserves arguments, quoting, and inherited environment variables. Normal startup returns after handing off; `--package-check` waits and propagates the child's exit code.

Before handing off, it validates the original embedded/external manifest and immutable bytes. `--verify-only` runs only that gate and never starts the .NET 10 child. Program files must be ordinary local files, not junctions/symlinks or cloud placeholders marked as reparse points. Validation does not access editable JSON profiles or browser data. Follow [Portable integrity](PORTABLE_INTEGRITY.md) for diagnostics, recovery, and the precise limitations; no automatic repair, signing, or updater is added.

`PortablePaths` recognizes `app/` only when its parent contains the product launcher and `portable-layout.json`. Configuration and local content then resolve from the parent, independently of the working directory. Old flat portable and development layouts are still supported. Do not rename/remove the launcher or layout marker.

Bundling .NET removes the separate .NET 10 installation requirement, not Windows or WebView2 requirements. It does not make browser sessions portable: existing LocalAppData paths are unchanged. Rebuild/redeploy with supported .NET security patches; bundled runtimes do not update through the machine's shared .NET installation. See [Microsoft deployment guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/).

## Required Package Contents

- `NetWatch-Lite-Wallboard.exe`
- `app/` containing application assemblies, WebView2 loader files, and the .NET 10 Desktop Runtime
- `portable-layout.json`, `integrity-manifest.xml`, and `START-HERE.txt`
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
.\scripts\test-portable.ps1 -ZipPath .\artifacts\packages\NetWatch-Lite-Wallboard-v0.4.0-rc.4-win-x64.zip
```

Tests run in a disposable copy under `artifacts/verification/`, never in the original deployment. They verify required files, the included Desktop Runtime, root/direct-payload startup from a different working directory, argument quoting, default/demo/production/custom profiles, environment-variable precedence and aliases, missing-profile fallback, unchanged JSON hashes, and detection of missing required files. Reports are outside the copied package. No WebView2 instance or user browser profile is opened by these checks.

The application's read-only `--package-check <absolute-new-report.json>` command exists for deployment tests. The root launcher's `--verify-only --report <absolute-new-report.txt>` checks integrity without starting the payload. Neither report command overwrites existing files. Tests verify runtime failures block the child before it creates a report, valid custom profiles remain usable, malformed JSON remains outside the integrity contract, and the original package is unchanged. Timings/diagnostics stay under `artifacts/verification/`. These checks are not full UI acceptance or proof of operation on every Windows machine.

## Rollback

Keep the previous portable folder and configuration until the new release completes acceptance and soak testing.

To roll back:

1. Close the new version.
2. Restore the previous folder.
3. Restore its compatible configuration.
4. Start it and verify the active version and configuration in Diagnostics.

## GitHub Publication

CI may create a validation artifact, but a successful workflow alone is not release approval. Stable releases require the approved release checklist and a tested extracted ZIP. An evaluation prerelease may be published only with explicit maintainer authorization, clear pending-test/usage limits, `prerelease=true`, and without marking it as Latest. This does not waive stable-release gates.

For rc.4, the existing ZIP and checksum were published without rebuilding. Tag `v0.4.0-rc.4` targets `9f30c6b19ddf97cb757c4db06bf588e357030f62`, matching the application's ProductVersion. Documentation updates continue on `main`; the binary's embedded documentation reflects its build commit. Both assets were downloaded from GitHub and compared to the local originals. Keep this separation explicit and never replace a published asset with a different build under the same filename/version.

---
[Previous: Operations](OPERATIONS.md) | [Documentation home](README.md) | [Next: Release checklist](RELEASE_CHECKLIST.md)
