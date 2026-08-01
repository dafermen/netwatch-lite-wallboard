# Security Policy

<!-- docs-nav -->
[Documentation home](README.md) | [Project README](../README.md) | [Current status](../CURRENT_STATUS.md)
<!-- /docs-nav -->

## Supported Versions

Security fixes are applied to the latest released version. Older portable builds should be upgraded rather than maintained indefinitely.

## Reporting A Vulnerability

Do not publish secrets, credentials, private URLs, or exploit details in a public issue.

Report a suspected vulnerability privately to the repository owner through GitHub's private vulnerability reporting feature when it is enabled. If private reporting is unavailable, open a minimal issue requesting a private contact channel without including sensitive technical details.

Include:

- Affected version and commit when known.
- Windows and WebView2 Runtime versions.
- Reproduction steps using non-sensitive data.
- Expected and actual behavior.
- Potential impact.

## Security Model

The application is a local desktop host for web content:

- It does not expose an inbound server or API.
- It does not scrape or inspect DOM content.
- Authentication is handled by each loaded website inside WebView2.
- Panels share a WebView2 profile, including cookies and sign-in state.
- Configuration controls which URLs and local files are loaded.

## Secure Configuration

- Treat `wallboard.json` as trusted local input.
- Do not store passwords, tokens, private keys, or session cookies in JSON.
- Do not put credentials in URL query strings.
- Use HTTPS for operational sites.
- Use `file:///` only for trusted local content.
- Keep production profiles out of public commits when they expose internal names or URLs.

## Dependency And Release Checks

Before release:

```powershell
dotnet list .\src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj package --vulnerable --include-transitive
```

Review:

- NuGet vulnerability output.
- WebView2 package and Runtime support.
- Third-party license changes.
- Secrets in staged files and package output.
- Configuration path validation.
- Error messages and logs for sensitive data.

Complete the security section of [TESTING.md](TESTING.md).

## WebView2 Hardening

The panel host disables developer tools, default context menus, status bar, zoom controls, and browser accelerator keys for the kiosk experience. These settings reduce accidental access; they are not a sandbox boundary.

Loaded websites remain responsible for their own content security, authentication, authorization, and session lifetime.

## Data Retention

Browser profile data persists under `%LOCALAPPDATA%\NetWatchLite\WallboardWebView2`. Anyone with access to the Windows account may be able to use existing site sessions.

When decommissioning a display computer:

1. Sign out of loaded services.
2. Close the application.
3. Remove the WebView2 profile.
4. Remove exported production configuration and logs.
5. Follow the organization's device-retirement policy.

---
[Previous: Testing](TESTING.md) | [Documentation home](README.md) | [Next: Architecture decisions](adr/README.md)
