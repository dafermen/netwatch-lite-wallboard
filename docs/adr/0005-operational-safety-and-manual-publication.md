# ADR 0005: Bounded Operations And Manual Publication

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

## Status

Accepted 2026-11-11 for an evaluation candidate. The maintainer authorized implementation and a real workflow draft only, not public/stable release or a 24-hour test in this task.

## Context

Long-running wallboards need bounded local diagnostics and optional mitigation without resetting browser sessions or silently changing configurations. Existing raw diagnostics can contain private URLs/paths. Publication should retain exact ZIP/source identity and never replace older versions.

## Decision

- Rotate only the managed general error log with bounded entries/files and three backups; retain a bounded tail of oversized old logs. Do not touch profiles/browser data. Logging remains best effort and does not crash the app.
- Export a new structured allowlist, not redacted raw logs/configuration. Keep numeric samples and enum events bounded; exclude operator strings. Retain raw on-screen/Copy diagnostics as a separate local feature.
- Keep memory protection off by default. Require continuous complete high samples, cooldown, an eligible foreground lifecycle and at most two reserved attempts per application session. Reloads reset tracking, not the budget. Reuse serialized visible-control recreation, without forced process termination or cookie deletion.
- Switch profiles only on explicit Settings action after unsaved-edit confirmation. Parse the exact existing file before changing selection, never silently fall back for UI switching. Selection is session-only; CLI aliases/precedence and existing startup/save/fallback remain intact.
- Pin reviewed stable WebView2 and official action commits. Separate manual artifact validation from an optional limited-write draft/publication job. Bind tag, actual binary ProductVersion, ZIP checksum and full commit SHA; refuse existing tags/releases. Upload to a draft, download and compare original hashes, then publish only with explicit authorization/mode. Never mark evaluation candidates Latest.

## Consequences

Memory figures remain approximate and panel recreation may not release all shared-browser memory. A safety budget can be exhausted and requires operator investigation. General logs remain private despite bounded retention. Existing oversized history beyond retention is intentionally discarded. A draft and mock tests are not public/stable approval. Configure environment reviewers separately for additional release approvals. Hosted SDK/runtime servicing can produce different archive bytes; deliver the exact verified workflow artifact locally rather than replacing a published version.

## Alternatives Considered

- Export raw diagnostics with regular-expression scrubbing: rejected; allowlisting avoids accidental secret-bearing fields.
- Restart on every high sample or kill WebView2 processes: rejected; short spikes, shared processes and authentication state make this disruptive.
- Persist UI profile choice by rewriting launch/environment settings: deferred; session-only selection preserves existing precedence.
- Publish automatically on every push or overwrite release assets: rejected; use explicit manual modes and immutable version identity.

---
[Previous: ADR 0004](0004-portable-startup-integrity.md) | [Documentation home](../README.md) | [Next: Documentation standard](../DOCUMENTATION_STANDARD.md)
