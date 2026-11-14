param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root ('TestResults\mutation-' + [guid]::NewGuid().ToString('N'))
$previous = $env:NETWATCH_WEBVIEW2_TESTS
try {
    $env:NETWATCH_WEBVIEW2_TESTS = '0'
    Push-Location $root
    try {
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) { throw 'Cannot restore pinned mutation tool.' }
        Push-Location (Join-Path $root 'tests\NetWatchLite.Wallboard.Tests')
        try {
            dotnet stryker --output $output --skip-version-check --break-on-initial-test-failure
            if ($LASTEXITCODE -ne 0) { throw "Mutation baseline failed. Inspect $output" }
        } finally { Pop-Location }
    } finally { Pop-Location }
} finally { $env:NETWATCH_WEBVIEW2_TESTS = $previous }
Write-Host "Mutation evidence: $output"
