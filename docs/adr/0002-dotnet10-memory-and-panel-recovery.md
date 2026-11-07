# ADR 0002: .NET 10, Process Memory, And Bounded Panel Recovery

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

- Status: Accepted
- Date: 2026-11-06

## Context

The application targets .NET 8, whose support ends on November 10, 2026. Its memory label counts only the host process even though WebView2 uses a separate process group. Panel process failures require operator recovery. Long-running use therefore needs better resource visibility, bounded recovery, and executable regression checks.

## Decision

- Target .NET 10 LTS in the application, tests, SDK selection, CI, and deployment requirements.
- Keep the current WebView2 package pinned during this framework migration.
- Measure the host and distinct process IDs returned by the active WebView2 environment. Mark partial samples and describe combined working sets as an estimate.
- Retain a lifetime peak and at most 1,440 samples; show recent values in Diagnostics.
- Enable automatic recovery by default for old and new JSON, with an explicit Settings switch.
- Retry at 5, 15, and 30 seconds, preserve state across control recreation, and reset the budget after a healthy minute or manual intervention.
- Cancel recovery on disposal, page changes, low-power suspension, and close. Serialize replacements through the main form's render lock.
- Add deterministic xUnit tests, temporary-file integration/contract tests, and opt-in isolated WebView2 tests.

## Consequences

New builds require .NET 10 Desktop Runtime. Existing JSON profiles remain readable. Memory totals will generally be larger because browser processes are now included; shared physical pages mean the value is approximate. A crash in the shared browser process requires reconnecting all visible panels. The monitor remains advisory and never restarts panels solely because memory is high.

Resource tests produce repeatable CSV/TRX evidence, but short runs cannot establish 24-hour production stability. The existing MSB3277 WebView2/WPF warning remains tracked separately.

## Alternatives Considered

- Count all `msedgewebview2` processes: rejected because other applications use the same executable.
- Give each recreated control a fresh retry budget: rejected because persistent failures would retry indefinitely.
- Upgrade the framework and browser package together: deferred to keep migration effects easier to isolate.
- Treat a green build as release acceptance: rejected because browser lifecycle and production dashboards need separate runtime validation.

---
[Previous: ADR 0001](0001-focused-wallboard-without-scraping.md) | [Documentation home](../README.md) | [Next: Documentation standard](../DOCUMENTATION_STANDARD.md)
