# Documentation

<!-- docs-nav -->
[Project README](../README.md) | [Current status](../CURRENT_STATUS.md) | [Changelog](../CHANGELOG.md)
<!-- /docs-nav -->

This is the documentation home for NetWatch Lite Wallboard. The project is a native Windows application, so documentation is maintained as GitHub Markdown rather than as a separate web application.

## Find Something Quickly

- Use the browser's find command to search the current page.
- Use GitHub repository search to search across all documents.
- Start with the operator path for daily use or the engineering path for code changes.

## Product

1. [Project introduction and product scope](../README.md)
2. [User guide](user-guide.md)
3. [Configuration contract](CONFIGURATION.md)
4. [Troubleshooting](TROUBLESHOOTING.md)

## Operations And Delivery

1. [Operations](OPERATIONS.md)
2. [Deployment](DEPLOYMENT.md)
3. [Release checklist](RELEASE_CHECKLIST.md)
4. [v0.4.0-rc.1 candidate validation](releases/v0.4.0-rc.1.md)

## Engineering

1. [Architecture](ARCHITECTURE.md)
2. [Junior developer code walkthrough](developer-guide.md)
3. [Development workflow](DEVELOPMENT.md)
4. [Testing strategy](TESTING.md)
5. [Security](SECURITY.md)

## Project Management

1. [Current status](../CURRENT_STATUS.md)
2. [Changelog](../CHANGELOG.md)
3. [Contributing](../CONTRIBUTING.md)
4. [Third-party licenses](../THIRD_PARTY_LICENSES.md)
5. [Architecture decisions](adr/README.md)
6. [Documentation standard](DOCUMENTATION_STANDARD.md)
7. [Codex handoff instructions](../AGENTS.md)

## Recommended Reading Paths

For an operator:

```text
User guide -> Configuration -> Troubleshooting -> Operations
```

For a new developer:

```text
Architecture -> Developer guide -> Development -> Testing -> Security
```

For a release owner:

```text
Testing -> Deployment -> Release checklist -> Current status -> Changelog
```

## Navigation Contract

Every document under `docs/` provides:

- A link back to this documentation home.
- A link to the project README and current status.
- Previous and next links following the reading order above.

The repository does not currently host a website at `/`, so web-only requirements such as `/docs/`, a mobile drawer, theme switcher, and a "back to application" URL do not apply. Introducing a hosted documentation site requires an explicit architecture decision.

---
[Start reading: User guide](user-guide.md)

## DOC-STD-20261002 — Canonical sources

Documentation standard v1.0 · reviewed 2026-10-02. Primary language: English.

Windows desktop wallboard using WinForms and WebView2.

This is a desktop application, not a hosted web service. Configuration is JSON; Node and env-file conventions do not apply. The removed scraping subsystem must stay absent. Portable packaging and device validation follow the release checklist.

| Need | Authoritative source |
| --- | --- |
| Presentation | [README.md](../README.md) |
| Current state | [CURRENT_STATUS.md](../CURRENT_STATUS.md) |
| Development | [docs/DEVELOPMENT.md](DEVELOPMENT.md) |
| Architecture | [docs/ARCHITECTURE.md](ARCHITECTURE.md) |
| Testing | [docs/TESTING.md](TESTING.md) |
| Security | [docs/SECURITY.md](SECURITY.md) |
| Deployment | [docs/DEPLOYMENT.md](DEPLOYMENT.md) |
| Operations | [docs/OPERATIONS.md](OPERATIONS.md) |
| Troubleshooting | [docs/TROUBLESHOOTING.md](TROUBLESHOOTING.md) |
| Usage | [docs/user-guide.md](user-guide.md) |
| Configuration | [docs/CONFIGURATION.md](CONFIGURATION.md) |
| Documentation rules | [docs/DOCUMENTATION_STANDARD.md](DOCUMENTATION_STANDARD.md) |
| History | [CHANGELOG.md](../CHANGELOG.md) |
| Decisions | [docs/adr/README.md](adr/README.md) |

Start with the presentation and current state, then read the user guide to try the product, development/architecture to contribute, or deployment/operations to maintain it. The existing detailed index remains valid.

### Evidence and updates

Keep current state, change history and decisions separate. Existing dated test results remain historical evidence. Adding this map does not rerun every documented command or complete pending product acceptance. Record actual checks, their environment and unresolved limits before publication.

Update the source guide whenever commands, configuration, behavior, permissions or deployment change. Keep existing links and portal routes stable. Use real screenshots with synthetic data; never publish env values, access keys, user data or operational logs. A local commit, a remote commit and a deployed artifact are separate states.
