# Contributing

Thank you for helping improve NetWatch Lite Wallboard.

## Before You Start

Read:

- `README.md`
- `AGENTS.md`
- `CURRENT_STATUS.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPMENT.md`
- `docs/TESTING.md`

For substantial changes, open an issue first so product scope and operational impact are clear.

## Pull Requests

- Keep one focused purpose per pull request.
- Preserve unrelated changes.
- Update documentation and examples with behavior changes.
- Add tests appropriate to the risk.
- Add user-visible changes under `Unreleased` in `CHANGELOG.md`.
- Complete the pull request checklist honestly.
- Never mark planned or manual validation as automated.

## Local Validation

```powershell
dotnet restore .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
dotnet build .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj -c Release --no-restore
git diff --check
```

Use .NET 10 and run `dotnet test .\netwatch-lite-wallboard.slnx -c Release`. The xUnit suite covers configuration, process-memory accounting, and retry budgets. Real-WebView2 and long resource tests are opt-in; see `docs/TESTING.md`. Build success is not evidence that tests passed.

Follow `docs/TESTING.md` for risk-based and release-level validation.

## Commit Guidance

Use concise, imperative commit messages, for example:

```text
Add configuration normalization tests
Fix panel disposal during rotation
Document portable release checks
```

Do not commit generated output, local backups, logs, credentials, or production-only configuration.

## Product Boundary

DOM scraping, selector monitoring, and page-content alarms were intentionally removed. A proposal to restore them requires an explicit product decision and a new ADR.

## Security

Follow `docs/SECURITY.md`. Do not open a public issue containing exploit details, credentials, private URLs, or sensitive logs.
