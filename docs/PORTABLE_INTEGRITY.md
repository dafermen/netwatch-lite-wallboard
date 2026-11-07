# Portable Startup Integrity

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

## Start And Recover

Extract the complete trusted ZIP into a new ordinary local folder and open `NetWatch-Lite-Wallboard.exe` at the root. Do not launch `app/NetWatch-Lite-Wallboard.exe` directly: that bypasses this root check.

The root launcher uses Windows .NET Framework 4.x, independently of the .NET 10 files being checked. Before starting the child, it compares the external `integrity-manifest.xml` byte-for-byte with its embedded reference, verifies protected file lengths and SHA-256, and checks for unexpected payload files. A missing, changed, locked/unreadable, or redirected payload blocks launch. Nothing is repaired, deleted, downloaded, or replaced automatically.

If blocked, preserve your JSON profiles/local pages, obtain the original ZIP again when needed, and extract it into a different local folder. Copy only the configuration/content you want to retain. Never mix DLLs from releases, modify the manifest, or rebuild it from a damaged installation. Existing browser sessions remain in their existing LocalAppData location.

## Protected And Editable Files

| Item | Startup treatment |
|---|---|
| Every file under `app/` | Must be listed with the expected size/hash; extra files are rejected. Includes executable, assemblies, native libraries, .NET runtime, satellite resources, and runtime JSON/XML files. |
| Root `portable-layout.json` | Protected package metadata, not an operator configuration file. |
| `integrity-manifest.xml` | Must match the trusted reference embedded in the launcher; the external copy is never parsed. |
| Root `wallboard*.json` and backups | Not read, hashed, normalized, or rewritten by the validator. Custom profiles are supported as before. |
| Local pages, docs, licenses, notes outside `app/` | Outside startup hashing. Keep operator content outside `app/`. The ZIP checksum separately covers the distributed archive. |
| Browser data and application logs | Not inspected or modified by validation. Normal validation failures write only the separate startup diagnostic described below. |

Malformed or missing wallboard JSON remains the application's existing load/fallback/normalization responsibility. The validator does not add a stricter configuration schema, change environment aliases/precedence, reset saved settings, or touch authentication/cookies.

## Manual Verification

The launcher's `--verify-only` mode never opens the application or WebView2. An optional report must be a new absolute path outside `app/`; an existing file is never overwritten.

```powershell
$report = Join-Path $env:TEMP ("netwatch-integrity-" + [guid]::NewGuid().ToString('N') + ".txt")
$check = Start-Process -FilePath .\NetWatch-Lite-Wallboard.exe -ArgumentList @('--verify-only', '--report', ('"' + $report + '"')) -WindowStyle Hidden -PassThru -Wait
$check.ExitCode
Get-Content -LiteralPath $report
```

Exit codes: `0` = protected bytes passed; `1` = integrity failure (including rejected reference metadata); `2` = invalid verification arguments, report failure, or an embedded reference that could not be loaded. This mode does not validate editable JSON or prove the UI/browser can start. `--verify-only` must be the first argument, with either no further arguments or exactly `--report <path>`.

The application's separate `--package-check <new-report.json>` probe is preserved. When invoked through the root launcher, integrity runs first and a failure returns `1` without starting the child or creating its JSON report. It still tests profile selection and actual bundled-runtime use on success. Verification/package-check failures do not show dialogs or write normal startup logs.

## Diagnostics And Limits

Normal launch failures display affected files (first twelve on screen) and write bounded diagnostics to `%LOCALAPPDATA%\NetWatchLite\WallboardLogs\portable-startup.log`. The log is truncated on a later write after reaching 1 MiB, with at most 32 KiB per report. Existing `wallboard-errors.log` and configurations are untouched. Logging failure does not suppress the primary error. Review local paths/details before sharing reports.

Protected paths are canonical package-relative entries; absolute paths, traversal, alternate streams, duplicate paths, invalid hashes/lengths, XML DTDs, and unsupported schema are rejected. The reference is limited to 4 MiB/4,096 files. Directory/file enumeration is bounded and does not follow reparse points. The conservative policy rejects all reparse-point payload entries, including junctions/symlinks and some cloud/virtual-storage placeholders. Copy/extract into a normal local folder instead of changing Windows security or cloud settings. See [Microsoft's reparse-point guidance](https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-point-operations).

Hashes are computed from streams, without loading the whole runtime into memory. Checking all bytes adds startup disk I/O and CPU; slow/network/cloud storage or antivirus scanning can take longer. Timings from one machine are not a general guarantee. See [.NET stream hashing](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hashalgorithm.computehash?view=netframework-4.8).

This is corruption/incomplete-copy/mixed-version detection, not publisher authentication, a digital signature, malware scanning, a sandbox, or an updater. Replacing the whole launcher/reference can bypass it. The launcher cannot diagnose its own OS-loader failure. Windows/.NET Framework, external WebView2, mutable configuration/content, and changes after checking are outside the guarantee. Opening the payload directly bypasses the gate. Continue to use trusted distribution, Windows permissions, dependency patches, backups, and normal release acceptance.

## Build And Test

`build-launcher.ps1` enumerates the finalized fresh `app/` and layout, writes a schema-1 XML manifest using structured APIs, then compiles that exact reference as a launcher resource. Never generate it from a user's working deployment. `PackageIntegrity.cs` stays compatible with the Windows Framework compiler and is linked into xUnit so the same logic is tested.

Package tests operate only on disposable copies. They exercise complete/restored packages, absent/corrupt/locked runtime files, modified manifests/layout, stale DLLs, junctions, customized/malformed editable JSON, report overwrite refusal, and blocked child startup. Unit fixtures add path/schema/hash/size/DTD bounds and mutable-data preservation. Test reports/timings remain under ignored `artifacts/verification/`. See [Testing](TESTING.md), [Deployment](DEPLOYMENT.md), and [ADR 0004](adr/0004-portable-startup-integrity.md).

---
[Previous: Deployment](DEPLOYMENT.md) | [Documentation home](README.md) | [Next: Security](SECURITY.md)
