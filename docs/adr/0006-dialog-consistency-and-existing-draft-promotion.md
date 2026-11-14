# ADR 0006: Native Dialog Consistency And Existing-Draft Promotion

<!-- docs-nav -->
[Documentation home](../README.md) | [Project README](../../README.md) | [Current status](../../CURRENT_STATUS.md)
<!-- /docs-nav -->

## Status

Accepted for unreleased source work on 2026-11-12. No public promotion, new portable or source push is authorized by this implementation request.

## Context

Settings/Diagnostics used fixed dark colors despite the shell theme. A profile selected for switching could be confused with the profile being edited. Diagnostics displayed the requested rather than effective browser path. The SDK also imports an unused WPF adapter, causing a WindowsBase conflict in this WinForms-only solution.

The fresh-package workflow refuses existing versions. Promoting an already verified draft should not rebuild/replace its known bytes. Deeper configuration/policy tests need reproducible tools and honest scope instead of a whole-application quality claim.

## Decision

- Share `ThemePalette` through `NativeTheme`, with readable accent/disabled command text. Preview only in Settings until save. No website/OS restyling or new JSON fields.
- Capture editing profile/save path, display pending selection separately and block ambiguous saves. Preserve explicit switch/discard/startup rules. Open the directory through an argument list; never persist another profile implicitly.
- Read the actual environment's `UserDataFolder`/`BrowserVersionString` on the UI thread. Distinguish requested/effective/unavailable values and keep them out of safe support JSON.
- Remove only `Microsoft.Web.WebView2.Wpf` before assembly resolution when WinForms is enabled and WPF is not. Do not suppress MSB3277 or edit global/package-cache files.
- Use test-only FsCheck 3.6.0, bounded seeded JSON fuzzing and pinned local Stryker.NET 5.1.0. Establish an 80% gate for the two recovery/memory policies, not unrelated UI/IO code. Keep reports ignored/local; no dashboard uploads.
- Add a verify-only-by-default existing-draft command/workflow. Require the previously approved hash, exact source SHA, rc tag, two uploaded assets and unchanged identity. Download, verify ProductVersion/checksum and test the exact ZIP. Recheck assets/source before explicit promotion. Never rebuild/upload/replace/delete assets or move tags. Publication needs a tag-specific phrase, separate write job and `prerelease` environment; always prerelease and never Latest.

## Consequences

No operator JSON, cookie/authentication profile or historical ZIP changes are needed. Existing rc.5 can be verified without publication. Fresh publication and existing-draft promotion share concurrency. Environment reviewers are external GitHub configuration, not enabled by the YAML itself.

Identity checks do not create an atomic GitHub transaction; repository writers must not edit the draft during promotion. Hashes are not publisher signatures. Synthetic scaling and scoped mutation/fuzzing do not replace real-DPI, other-device, operator or 24-hour production acceptance. Public promotion remains separately authorized.

## Alternatives Considered

- Rebuild the draft: rejected because this changes tested bytes and can change the bundled runtime.
- Overwrite assets through fresh publication: rejected to preserve immutable history.
- Hide MSB3277: rejected because the unused WPF dependency can be removed at its source.
- Persist the last chosen profile: deferred because it introduces a new startup/persistence contract.

## References

- [Stryker configuration](https://stryker-mutator.io/docs/stryker-net/configuration/)
- [FsCheck quick start](https://fscheck.github.io/FsCheck/QuickStart.html)
- [GitHub release editing](https://cli.github.com/manual/gh_release_edit)

---
[Previous: ADR 0005](0005-operational-safety-and-manual-publication.md) | [Documentation home](../README.md) | [Next: ADR 0007](0007-exclusive-home-and-wallboard-views.md)
