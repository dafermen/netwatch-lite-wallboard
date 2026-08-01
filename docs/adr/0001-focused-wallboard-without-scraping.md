# ADR 0001: Focused WebView2 Wallboard Without Scraping

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

- Status: Accepted
- Date: 2026-07-28

## Context

Earlier versions included DOM scraping, selector monitoring, alarms, severity colors, and a scraping test page. Those features increased configuration and runtime complexity and no longer matched the desired product.

## Decision

NetWatch Lite Wallboard is a focused page-display application.

It may:

- Navigate to configured web and trusted local pages.
- Refresh or restart browser panels.
- Arrange and rotate panels.
- Provide kiosk, memory, diagnostics, profile, and theme features.

It must not reintroduce DOM scraping, selector polling, page-content alarms, or JavaScript injection for monitoring unless a future explicit product decision supersedes this ADR.

## Consequences

- Configuration remains smaller and easier for operators.
- The app does not interpret dashboard state or trigger content-based alerts.
- Monitoring and alerting remain the responsibility of the loaded services.
- New contributors have a clear boundary when extending WebView2 behavior.

## Alternatives Considered

- Keep scraping disabled but retain its models and UI. Rejected because dormant code and configuration would remain confusing.
- Split scraping into an optional plugin. Rejected because there is no current product requirement for that subsystem.

---
[Previous: Architecture decisions](README.md) | [Documentation home](../README.md) | [Next: Documentation standard](../DOCUMENTATION_STANDARD.md)
