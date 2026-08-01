# Tests

Automated test projects belong under this directory.

Recommended structure as coverage is introduced:

```text
tests/
|-- NetWatchLite.Wallboard.UnitTests/
|-- NetWatchLite.Wallboard.IntegrationTests/
|-- NetWatchLite.Wallboard.ContractTests/
|-- NetWatchLite.Wallboard.EndToEndTests/
`-- fixtures/
```

Use .NET project names instead of generic `unit/`, `integration/`, and `e2e/` directories so test assemblies are easy to identify in Visual Studio and CI.

Do not create empty test projects to make the repository look covered. Each project should begin with meaningful tests and be added to `netwatch-lite-wallboard.slnx`.

## Planned First Tests

1. Configuration defaults and bounds.
2. Supported layout normalization.
3. Environment profile name normalization and path selection.
4. Theme normalization.
5. Configuration save and read-back.
6. Backward compatibility with a v0.3.0 fixture.

See [Testing Strategy And Release Gates](../docs/TESTING.md) for the complete quality model.
