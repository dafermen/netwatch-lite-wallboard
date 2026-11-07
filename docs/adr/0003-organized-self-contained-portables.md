# ADR 0003: Organized Self-Contained Portables

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

- Status: Accepted
- Integrity stage implemented subsequently by [ADR 0004](0004-portable-startup-integrity.md); original stage-1 decisions below remain historical.
- Date: 2026-11-07

## Context

The flat portable mixes operator-editable profiles with DLLs and requires a separate .NET 10 Desktop Runtime installation. The user requested TagPrinter-style organization without changing settings, profiles, browser sessions, or the existing application behavior.

## Decision

- Use full-version output under `artifacts/portable/` and ZIP/checksum output under `artifacts/packages/`.
- Build in unique staging and refuse existing final outputs instead of deleting a potentially customized deployment.
- Publish self-contained .NET 10 into `app/`, without trimming, single-file packaging, or AOT.
- Keep a small root launcher compiled against Windows .NET Framework 4.x, forwarding arguments to the payload. Preserve profile aliases, precedence, and Windows quoting.
- Keep JSON and local pages at the root. A shared path helper recognizes the exact `app/` directory plus parent launcher/layout marker. Flat/development layouts retain their existing behavior.
- Keep WebView2 Evergreen external and retain all existing LocalAppData browser/log paths.
- Validate the exact ZIP using disposable, no-window processes reporting the runtime and configuration paths; test save/backup routing separately in xUnit.
- Defer per-file startup hashing, signing, automatic updating, and configuration migration to separate work.

## Consequences

The package is larger but does not need a separate .NET 10 installation. It still needs Windows .NET Framework for the root launcher and WebView2 Runtime for browser panels. Bundled .NET security patches require a rebuilt/redeployed package. Package checks do not certify arbitrary Windows machines or replace UI acceptance/long-session tests. A read-only diagnostic probe never overwrites reports, saves JSON, or starts WebView2.

The layout marker is a presence-based layout indicator, not a signature or a trusted integrity manifest. Do not rename/remove it or mix payload versions. Updating means extracting into a new folder and copying the chosen JSON/local pages, not overwriting an existing deployment.

## Alternatives Considered

- Put DLLs under `app/` without changing path resolution: rejected because settings and relative pages would move silently.
- Duplicate the full .NET 10 runtime for a separate modern launcher: rejected as unnecessary size/complexity.
- Adopt TagPrinter's full integrity validator immediately: deferred to stage 2 to keep this change focused.
- Replace existing version folders automatically: rejected because they may contain operator settings.
- Bundle a fixed WebView2 browser runtime: deferred; it increases package/patching responsibility beyond this stage.

---
[Previous: ADR 0002](0002-dotnet10-memory-and-panel-recovery.md) | [Documentation home](../README.md) | [Next: ADR 0004](0004-portable-startup-integrity.md)
