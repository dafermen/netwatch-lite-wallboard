# ADR 0007: Exclusive Inicio And Wallboard Views

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

## Status

Accepted for local unreleased work on 2026-11-14. No new portable or GitHub publication is implied.

## Context

Operators need to stop the divided dashboards and display a corporate page across the content area. Hiding browsers would leave refresh/recovery activity and memory consumption behind. Existing profiles, cookie data and earlier local changes must remain compatible.

## Decision

- Add optional `homePageEnabled=false`, `homePageUrl=""`, and `startupView="wallboard"`. Normalize through the existing reader; invalid/missing Home URLs cannot enable Inicio. Settings validates enabling and carries the fields through clone/import/export/save. Existing Data/deployed files are never rewritten automatically; the neutral example is a separate test fixture.
- Keep two mutually exclusive runtime modes owned by the main form. Render one chrome-free Home browser or the configured wallboard page, disposing the inactive controls and timers. Reuse `WebViewPanelControl` with a default-compatible Home presentation flag and a native retry surface. Home has no periodic refresh or automatic recovery; retry/refresh never start wallboard.
- Share the existing browser environment/profile without clearing cookies. Preserve wallboard page/layout across mode-only switches and the application memory-protection budget. Website in-page state is not retained; websites can still expire authentication independently.
- Stop rotation/scheduled restart and cancel pending initialization on transitions. Serialize through the existing render lock and reject stale render/recovery generations after acquiring it. Home is excluded from automatic memory recovery; bounded memory sampling remains available. Resume starts fresh wallboard timer intervals rather than catch-up operations.
- Preserve Home on same-profile save/reload if still enabled; apply startup preference on launch or explicit profile switch. Disabling the feature returns to Wallboard. Low-power releases/restores the selected mode; fullscreen, themes, auto-hide and close confirmation remain independent.
- Keep the Home URL local/raw in Diagnostics. Safe support JSON retains its existing allowlist and excludes that URL. Do not persist another runtime-mode state file or change profile precedence/save/backup paths.

## Consequences

Only one view remains active. Disposal releases controls but cannot guarantee an immediate working-set decrease or 24-hour stability; shared browser resources/caches may persist. Short isolated tests cover disposal/timers/queued work, cookies, bounds and UI behavior. Real company pages and long-session/device acceptance remain separate.

## Alternatives Considered

- Hide existing panels under Home: rejected because background work remains active.
- Reuse layout 1 alone: rejected because it changes dashboard selection/layout instead of representing a stopped wallboard.
- Separate environment/profile for Home: rejected because it changes authentication/storage behavior and duplicates browser resources.
- Remember every runtime toggle on disk: rejected in favor of explicit profile startup settings and non-writing view changes.

---
[Previous: ADR 0006](0006-dialog-consistency-and-existing-draft-promotion.md) | [Documentation home](../README.md) | [Next: Documentation standard](../DOCUMENTATION_STANDARD.md)
