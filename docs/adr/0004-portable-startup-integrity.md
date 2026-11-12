# ADR 0004: Portable Startup Integrity

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

- Status: Accepted
- Date: 2026-11-07
- Extends: [ADR 0003](0003-organized-self-contained-portables.md), implementing its deferred integrity stage.

## Context

The organized portable contains a self-contained .NET 10 payload. Incomplete copies or mixed/altered runtime libraries may fail before the real application can explain the error. The user requested TagPrinter-style startup checks without changing configuration or browser profiles.

## Decision

- Generate size/SHA-256 metadata for every finalized `app/` file plus `portable-layout.json`, then embed the exact XML reference in the independent Windows Framework launcher.
- Require an identical external manifest, required entries, safe unique paths/metadata, and exact protected bytes before starting the payload. Reject extra payload files and reparse-point entries rather than following redirections.
- Bound manifest size, entry count, directory/file enumeration, and normal failure logs. Stream file hashes; never load the entire runtime into one buffer.
- Keep all editable JSON profiles/backups, local pages, browser sessions, and existing app logs outside the integrity scope. Preserve environment arguments, fallback, save, and authentication behavior.
- Block on failure with diagnostics, never repair/delete/replace files automatically. Add no setting that disables this gate.
- Provide a quiet `--verify-only` mode with new-file-only reports; preserve `--package-check` and prove failures prevent child startup.
- Test shared Framework-compatible source in xUnit and the actual compiled launcher against disposable package copies.

## Consequences

Each root launch reads the protected bytes and adds startup work. A normal local folder is required; conservative reparse rejection can exclude cloud placeholders and linked payloads. Existing flat/source builds and direct payload starts keep their behavior but bypass this gate.

This is not digital signing, malware protection, an immutable filesystem boundary, automatic repair, or publisher authentication. The trusted launcher/reference can itself be replaced and files can change after a check. External Windows/Framework/WebView2 components and mutable data remain outside the guarantee. Stable acceptance and long-production testing are still separate gates.

## Alternatives Considered

- Validate inside the .NET 10 app only: rejected because a damaged runtime can fail before that code executes.
- Trust only an editable external manifest: rejected; it could be changed with the files.
- Hash all JSON including operator profiles: rejected because valid Settings/profile edits must remain valid.
- Recreate the manifest after a mismatch: rejected because it would approve damaged/mixed files.
- Add signing or a full updater now: deferred as separate delivery/security work, not implied by hashes.

---
[Previous: ADR 0003](0003-organized-self-contained-portables.md) | [Documentation home](../README.md) | [Next: ADR 0005](0005-operational-safety-and-manual-publication.md)
