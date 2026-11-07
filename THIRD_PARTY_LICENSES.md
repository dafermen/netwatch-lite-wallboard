# Third-Party Licenses

NetWatch Lite Wallboard is licensed under MIT. It also depends on third-party software with its own license terms.

## Direct NuGet Dependency

| Component | Purpose | License |
|---|---|---|
| Microsoft.Web.WebView2 | Embedded Microsoft Edge browser controls | Microsoft software license terms |

Package details and license metadata are available from the installed NuGet package and the [Microsoft.Web.WebView2 package page](https://www.nuget.org/packages/Microsoft.Web.WebView2).

## Platform Dependencies

- .NET 10 Runtime and SDK are licensed by Microsoft.
- Microsoft Edge WebView2 Runtime is licensed by Microsoft.
- Windows is required and licensed separately.

## Release Maintenance

Development tests use xUnit.net (Apache-2.0), xunit.runner.visualstudio (Apache-2.0), and Microsoft.NET.Test.Sdk (MIT). These test-only dependencies are not part of the portable app. Review exact package license metadata when upgrading them.

Before every release:

1. Review direct and transitive dependencies.
2. Confirm license metadata for newly added or updated packages.
3. Update this file when dependency or license obligations change.
4. Ensure required notices accompany distributed binaries.

This file is an inventory aid and is not legal advice.
