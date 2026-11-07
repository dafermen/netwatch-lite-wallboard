# Development

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

This is the contributor entry point. The detailed code walkthrough remains in [developer-guide.md](developer-guide.md).

## Requirements

- Windows 10 or later.
- .NET 10 SDK (stable, selected by `global.json`).
- Microsoft Edge WebView2 Runtime.
- Git.
- A Visual Studio version supporting .NET 10, Visual Studio Code, or another C# editor is optional. The CLI SDK is the authoritative build requirement.

## First Setup

```powershell
git clone https://github.com/dafermen/netwatch-lite-wallboard.git C:\Projects\netwatch-lite-wallboard
cd C:\Projects\netwatch-lite-wallboard
dotnet restore
dotnet build .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
dotnet test .\netwatch-lite-wallboard.slnx -c Release
```

Run:

```powershell
dotnet run --project .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj
```

## Before Editing

Read:

1. `AGENTS.md`
2. `CURRENT_STATUS.md`
3. `docs/ARCHITECTURE.md`
4. `docs/CONFIGURATION.md`
5. `docs/TESTING.md`
6. `docs/developer-guide.md`

Check the working tree before making changes:

```powershell
git status --short --branch
git log -3 --oneline
```

Preserve unrelated local changes.

## Change Workflow

1. Create a focused branch when working through pull requests.
2. Make the smallest change that solves the problem.
3. Update configuration examples and user-facing documentation when behavior changes.
4. Add or update tests in proportion to the risk.
5. Run the relevant checks from [TESTING.md](TESTING.md).
6. Update `CHANGELOG.md` under `Unreleased`.
7. Use the pull request template to record evidence.

## Coding Rules

- Keep nullable reference types enabled.
- Prefer existing WinForms patterns and framework APIs.
- Keep async UI handlers guarded by the existing error-handling helpers.
- Stop timers and detach events during disposal.
- Never reintroduce DOM scraping or page-content alarms without an explicit product decision.
- Never commit `bin/`, `obj/`, `publish/`, `artifacts/`, ZIP files, logs, or local backups.

Portable publishing also uses the Windows .NET Framework C# compiler for its small root launcher. The actual application remains .NET 10. Generated packages include the .NET 10 Desktop Runtime. Run `scripts/publish-portable.ps1` for a fresh version; it refuses existing output and runs `scripts/test-portable.ps1` before promoting the exact ZIP. See [Deployment](DEPLOYMENT.md).

## Configuration Changes

For a top-level setting, update:

- `WallboardConfiguration.cs`
- `WallboardConfigReader.cs`
- `SettingsForm.cs`
- `DiagnosticsForm.cs` when operationally relevant
- `Data/wallboard*.json`
- `docs/CONFIGURATION.md`
- End-user documentation
- Tests for defaults, bounds, and backward compatibility

For a panel setting, also update `WallboardPanel.cs` and `WebViewPanelControl.cs`.

## Documentation Rules

- `README.md` is the concise GitHub entry point.
- `docs/user-guide.md` is for operators.
- Uppercase documents in `docs/` are focused technical references.
- `docs/developer-guide.md` is the junior-friendly code walkthrough.
- `CURRENT_STATUS.md` records the actual current state, not long-term ideas.
- Major decisions go under `docs/adr/`.

Avoid copying the same detailed explanation into several files. Link to the authoritative document instead.

Validate documentation changes with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\validate-docs.ps1
```

See [Documentation standard](DOCUMENTATION_STANDARD.md) for navigation, writing, link, and image rules.

---
[Previous: Developer guide](developer-guide.md) | [Documentation home](README.md) | [Next: Testing](TESTING.md)
