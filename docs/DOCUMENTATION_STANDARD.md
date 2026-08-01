# Documentation Standard

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

This standard keeps project documentation predictable for operators, junior developers, release owners, and future Codex sessions.

## Delivery Surface

Documentation is rendered directly by GitHub from Markdown files. NetWatch Lite Wallboard is a Windows desktop application and does not host a web application at `/`.

Therefore:

- `README.md` is the product and repository entry point.
- `docs/README.md` is the documentation home.
- Documents use normal relative links that work on GitHub and in local editors.
- No VitePress, Node.js, `package.json`, or `/docs/` web route is added without a separate product and hosting decision.

## Information Architecture

Documents are grouped by their real audience and purpose:

| Area | Purpose |
|---|---|
| Product | Scope, user operation, configuration, and troubleshooting. |
| Operations and delivery | Long-running support, deployment, and release approval. |
| Engineering | Architecture, code onboarding, development, tests, and security. |
| Project management | Current status, history, contribution, decisions, and handoff. |

Do not add empty placeholder documents. Add a document only when it has an owner, a distinct purpose, and useful content.

## Page Navigation

Every Markdown document under `docs/` must include the `docs-nav` block immediately after its title:

```markdown
<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->
```

Documents in nested directories must adjust relative paths. Each page also ends with previous, documentation home, and next links matching the order in `docs/README.md`.

## Writing Rules

- Use one `#` title per document.
- Use short, descriptive headings in a logical order.
- Prefer plain language and define unavoidable technical terms.
- Keep procedures sequential and commands copyable.
- Link to the authoritative document instead of duplicating long explanations.
- Distinguish current behavior, planned work, and historical behavior.
- Never claim that a planned or manual test is automated.

## Links And Images

- Use relative links for repository files.
- Do not create duplicated paths such as `docs/docs/`.
- Give every image meaningful alternative text.
- Store documentation images under `docs/images/`.
- Avoid machine-specific links and private URLs.

## Validation

Run before committing documentation changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\validate-docs.ps1
git diff --check
```

CI runs the same documentation validation. It checks local links, image alternative text, navigation markers, index coverage, duplicate `docs/docs/` paths, and top-level headings.

## When A Documentation Website Is Justified

Adopt a documentation generator only when the project has a real hosting target and needs capabilities that GitHub Markdown cannot provide, such as full local search, responsive sidebar navigation, theme switching, or versioned documentation.

That change must define hosting, public routes, ownership, build maintenance, accessibility validation, and how the native desktop application links to the hosted site. Record the decision in an ADR before adding the toolchain.

---
[Previous: ADR 0001](adr/0001-focused-wallboard-without-scraping.md) | [Documentation home](README.md) | [Next: User guide](user-guide.md)
