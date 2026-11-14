# Deployment

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

NetWatch Lite Wallboard is distributed as a portable Windows folder and ZIP. It is not installed as a Windows service and does not require a web server.

At the user's explicit request, `0.4.0-rc.4` is provided as an [evaluation prerelease with ZIP and checksum](https://github.com/dafermen/netwatch-lite-wallboard/releases/tag/v0.4.0-rc.4) while long-session and full acceptance checks remain pending. Its [validation record](releases/v0.4.0-rc.4.md) is not stable-release approval. All previous packages are retained and are not moved or overwritten. Stable publication still requires the prerequisites below.

The current source candidate is `0.4.0-rc.7`; see its [validation record](releases/v0.4.0-rc.7.md). The user authorized a new portable/ZIP/checksum, source synchronization and a **draft only**, not public publication. Preserve rc.6/rc.5 drafts, rc.4 and stable Latest. The same tested local ZIP can be uploaded using the guarded local publisher in Draft mode; download/hash verification must then prove local/remote identity. If a hosted workflow instead builds the deliverable, retain its exact downloaded bytes locally rather than replacing its assets with a different local rebuild.

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

### Verify Or Promote An Existing Draft

Use **Actions > Verify Or Promote Existing Prerelease** from `main` for an already uploaded rc draft. Supply its exact tag, full binary-source SHA and the ZIP SHA-256 recorded during the original verified delivery, not a newly calculated hash of untrusted bytes. `publish` defaults to false. Verification downloads the two existing assets, validates checksum/ProductVersion/source, runs the exact-ZIP package tests and records a receipt. It never builds, creates a release, uploads/replaces assets or moves an existing tag.

Publication requires separate maintainer authorization, `publish=true` and approval text exactly `PUBLISH vX.Y.Z-rc.N` for that tag. The separate `prerelease` environment/write job repeats the full download/package checks after the approval wait, rechecks release/asset identities and source immediately before editing only draft/prerelease/Latest flags, and confirms the resulting identity. Required environment reviewers must be configured in GitHub settings; this task does not change them. Concurrency is shared with fresh-package publication.

Local verification (no publication):

```powershell
.\scripts\promote-prerelease.ps1 -Tag v0.4.0-rc.5 `
  -SourceCommit 62a50977f2402e3d2eced4866d0fb07493bea4c6 `
  -ExpectedZipSha256 2488c0690c61cbb4a6af4832ec330add612247467c82c581ca436479f7b03322
```

Only an explicitly authorized maintainer adds `-Publish -Approval 'PUBLISH <exact-tag>'`. The command rejects public/stable releases, changed identities, extra/incomplete assets, source/tag/hash mismatches and read failures. GitHub tokens must use normal CLI/job authentication, never arguments or committed files. Receipts and disposable downloads remain under ignored `artifacts/verification/promotion-*/`.

The previously approved ZIP hash is the trust anchor, not a signature. GitHub does not provide an atomic lock spanning verification and publication: repository writers must avoid editing drafts/assets during the operation. Our workflow serializes publication runs and rechecks identity but cannot prevent concurrent administrator edits. Errors after editing require inspecting actual remote state; never blindly retry or replace assets. This is for authorized evaluation prereleases only, not stable approval or completion of long-session acceptance. See [ADR 0006](adr/0006-dialog-consistency-and-existing-draft-promotion.md) and the [GitHub CLI edit reference](https://cli.github.com/manual/gh_release_edit).

### Manual Prerelease Workflow

Run **Actions > Prepare Portable Prerelease > Run workflow** from `main`, entering the exact project tag (currently `v0.4.0-rc.7`). The workflow never runs publication on an ordinary push or pull request. Do not dispatch creation for a version that already has a draft or assets.

| Mode | Result |
|---|---|
| `artifact-only` (default) | Build, tests, audit, exact-ZIP checks and seven-day validation artifacts; no release/tag creation. |
| `draft` | Separate limited-write job verifies the same-run artifact and ProductVersion/source SHA, refuses existing tags/releases, repeats package checks, uploads a draft and downloads/compares both assets. Stays private to authorized repository users. |
| `publish` | Same checks, plus explicit public prerelease publication after verification. Never stable or Latest. Requires separate maintainer authorization. |

The publication job uses the `prerelease` environment. Configure required reviewers in GitHub settings for an additional approval gate; the YAML does not configure reviewers or claim they exist. There is no stored personal token: only the job's scoped `GITHUB_TOKEN`. Global concurrency serializes these manual runs without canceling an active publication. Hosted CI skips opt-in desktop tests; independent local UI/browser evidence is still required and is not inferred from CI.

Official actions are pinned to reviewed commits: checkout `v7.0.1`, setup-dotnet `v6.1.0`, upload-artifact `v7.0.2`, and download-artifact `v8.0.2`. They use Node 24 on `windows-latest`; older self-hosted runners require a separate compatibility review. Download digest mismatches fail closed. See the [official action releases](https://github.com/actions/download-artifact/releases/tag/v8.0.2) and [WebView2 SDK release notes](https://learn.microsoft.com/en-us/microsoft-edge/webview2/release-notes/sdk/1-0-4314-35).

Local maintainers can use `scripts/publish-prerelease.ps1` with `-Tag`, `-SourceCommit` (full 40-character SHA), `-PackageDirectory` and `-Mode Draft`. It validates but does not build the supplied package. The ZIP's actual ProductVersion must match the tag/version and source; merely renaming an archive is refused. `scripts/test-release-package.ps1` tests guards with isolated fixtures and mock GitHub, not a live release. Existing releases/tags/assets are never overwritten or moved. On upload/download/verification failure inspect the remaining draft before retrying; do not bypass checks or use `--clobber`.

CI may create a validation artifact, but a successful workflow alone is not release approval. Stable releases require the approved release checklist and a tested extracted ZIP. An evaluation prerelease may be published only with explicit maintainer authorization, clear pending-test/usage limits, `prerelease=true`, and without marking it as Latest. This does not waive stable-release gates.

For rc.4, the existing ZIP and checksum were published without rebuilding. Tag `v0.4.0-rc.4` targets `9f30c6b19ddf97cb757c4db06bf588e357030f62`, matching the application's ProductVersion. Documentation updates continue on `main`; the binary's embedded documentation reflects its build commit. Both assets were downloaded from GitHub and compared to the local originals. Keep this separation explicit and never replace a published asset with a different build under the same filename/version.

---
[Previous: Operations](OPERATIONS.md) | [Documentation home](README.md) | [Next: Release checklist](RELEASE_CHECKLIST.md)
