# Third-Party Licenses

NetWatch Lite Wallboard is licensed under MIT. It also depends on third-party software with its own license terms.

## Direct NuGet Dependency

| Component | Purpose | License |
|---|---|---|
| Microsoft.Web.WebView2 | Embedded Microsoft Edge browser controls | Microsoft software license terms |

Package details and license metadata are available from the installed NuGet package and the [Microsoft.Web.WebView2 package page](https://www.nuget.org/packages/Microsoft.Web.WebView2).

## Platform Dependencies

- The portable now redistributes the .NET 10 Desktop Runtime inside `app/`. Exact runtime package license/notices are copied into `licenses/dotnet-runtime/` and `licenses/windows-desktop/`; WebView2 SDK license/notices are in `licenses/webview2/`. These original files are required package contents, not replaced by this inventory. See the [.NET runtime license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [.NET third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT), and [Windows Desktop license](https://github.com/dotnet/windowsdesktop/blob/main/LICENSE).
- The .NET SDK is a development dependency; Windows .NET Framework 4.x is an OS prerequisite for the root launcher, not a bundled second runtime.
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
