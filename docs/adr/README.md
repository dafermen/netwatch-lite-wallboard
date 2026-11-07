# Architecture Decision Records

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

Architecture Decision Records capture important decisions that future contributors should not have to rediscover.

## When To Add An ADR

Add an ADR for:

- A product-scope change.
- A major framework or dependency change.
- A breaking configuration change.
- A new persistence or security boundary.
- A substantial WebView2 lifecycle change.
- A deployment model change.

Do not create an ADR for routine bug fixes or small UI adjustments.

## Format

Use the next four-digit number:

```text
NNNN-short-decision-title.md
```

Each record should contain:

- Status
- Context
- Decision
- Consequences
- Alternatives considered

## Decisions

- [0001 - Focused WebView2 wallboard without scraping](0001-focused-wallboard-without-scraping.md)
- [0002 - .NET 10, process memory, and bounded panel recovery](0002-dotnet10-memory-and-panel-recovery.md)
- [0003 - Organized self-contained portables](0003-organized-self-contained-portables.md)
- [0004 - Portable startup integrity](0004-portable-startup-integrity.md)

---
[Previous: Security](../SECURITY.md) | [Documentation home](../README.md) | [Next: ADR 0001](0001-focused-wallboard-without-scraping.md)
