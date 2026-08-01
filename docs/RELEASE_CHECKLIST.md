# Release Readiness Checklist

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

Copy this checklist into the release issue. Every line requires `Passed`, `Failed`, or `Not applicable` with a reason.

## Candidate

- Version:
- Commit:
- Date:
- Release owner:
- Windows test environments:
- WebView2 Runtime versions:

## Quality Results

| Area | Result | Evidence or reason |
|---|---|---|
| 1. Acceptance |  |  |
| 2. Unit |  |  |
| 3. Properties and invariants |  |  |
| 4. Mutation |  |  |
| 5. Fuzzing |  |  |
| 6. Integration |  |  |
| 7. Contract |  |  |
| 8. End-to-end |  |  |
| 9. Regression |  |  |
| 10. Security |  |  |
| 11. Concurrency and resilience |  |  |
| 12. Performance and resources |  |  |
| 13. Compatibility and deployment |  |  |

## Package Inspection

- [ ] Version matches the project and changelog.
- [ ] Release build completed.
- [ ] Portable folder and ZIP were generated from this commit.
- [ ] Executable starts from the extracted ZIP.
- [ ] Expected JSON profiles are present.
- [ ] README and LICENSE are present.
- [ ] No `wallboard.backup.json`, logs, `bin`, or `obj` files are present.
- [ ] CI completed successfully.
- [ ] Known warnings and accepted risks are recorded below.

## Decision

- [ ] All applicable areas passed.
- [ ] No failed or untested applicable area remains.
- [ ] Release approved.

Known warnings and accepted risks:

Release approver:

---
[Previous: Deployment](DEPLOYMENT.md) | [Documentation home](README.md) | [Next: Architecture](ARCHITECTURE.md)
