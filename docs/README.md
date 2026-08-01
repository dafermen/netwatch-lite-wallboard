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
