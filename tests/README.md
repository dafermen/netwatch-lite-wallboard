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

Run `dotnet test .\netwatch-lite-wallboard.slnx -c Release`. Real-browser tests are skipped unless `NETWATCH_WEBVIEW2_TESTS=1`. Run `scripts/test-soak.ps1 -Minutes 1440` for the 24-hour neutral-fixture resource test. Test output belongs under the ignored `TestResults/` directory. Layout, profile selection, broader URL tests, mutation testing, and broader fuzzing remain future work.

See [Testing Strategy And Release Gates](../docs/TESTING.md) for the complete quality model.
