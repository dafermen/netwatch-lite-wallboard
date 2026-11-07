# Tests

`NetWatchLite.Wallboard.Tests` is the automated xUnit project for .NET 10 on Windows.

Current layout:

```text
tests/
|-- NetWatchLite.Wallboard.Tests/
`-- fixtures/
```

Use .NET project names instead of generic `unit/`, `integration/`, and `e2e/` directories so test assemblies are easy to identify in Visual Studio and CI.

Do not create empty test projects to make the repository look covered. Each project should begin with meaningful tests and be added to `netwatch-lite-wallboard.slnx`.

## Automated Coverage

1. Configuration defaults, numeric invariants, invalid panels, missing and unknown fields.
2. Configuration save/read/backup integration using temporary files.
3. Backward compatibility with the v0.3.0 fixture.
4. Process-memory deduplication, partial samples, peaks, and bounded history.
5. Progressive recovery delays, retry limits, duplicate events, stale generations, inactive periods, and stable/manual resets.
6. Opt-in real-WebView2 renderer recovery, main-form shared-browser recovery despite an environment-factory failure, Settings layout, and repeated disposal/resource tests.
7. Organized/flat/development content paths, default/demo/production save/read/backup, missing-profile fallback, local pages, and read-only package-report overwrite protection.
8. Embedded icon frame dimensions, transparency/nonempty content, and independent main/Settings/Diagnostics icon lifetimes. Package probes verify the icon resource is present without loose image files.

Run `dotnet test .\netwatch-lite-wallboard.slnx -c Release`. Real-browser tests are skipped unless `NETWATCH_WEBVIEW2_TESTS=1`. Run `scripts/test-soak.ps1 -Minutes 1440` for the 24-hour neutral-fixture resource test. Test output belongs under the ignored `TestResults/` directory. Broader layout/URL tests, mutation testing, and broader fuzzing remain future work.

`scripts/test-portable.ps1 -ZipPath <archive>` performs real-process package checks from a disposable extraction under `artifacts/verification/`. It verifies bundled-runtime use, launcher argument forwarding, configuration profiles, root paths, unchanged JSON, and missing-file detection. Publishing invokes it before promoting a ZIP. It never opens WebView2 or edits the original package; see [Deployment](../docs/DEPLOYMENT.md).

See [Testing Strategy And Release Gates](../docs/TESTING.md) for the complete quality model.
