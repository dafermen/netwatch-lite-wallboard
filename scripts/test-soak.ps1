[CmdletBinding()]
param(
    [ValidateRange(1, 1440)]
    [int]$Minutes = 1440
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$reportDirectory = Join-Path $repoRoot 'TestResults'
New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
$previousWebView = $env:NETWATCH_WEBVIEW2_TESTS
$previousMinutes = $env:NETWATCH_SOAK_MINUTES
$previousReport = $env:NETWATCH_SOAK_REPORT

try {
    $env:NETWATCH_WEBVIEW2_TESTS = '1'
    $env:NETWATCH_SOAK_MINUTES = $Minutes.ToString([Globalization.CultureInfo]::InvariantCulture)
    $env:NETWATCH_SOAK_REPORT = Join-Path $reportDirectory ("soak-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.csv')
    dotnet test (Join-Path $repoRoot 'tests\NetWatchLite.Wallboard.Tests\NetWatchLite.Wallboard.Tests.csproj') `
        -c Release --filter 'Category=Soak' --logger trx --results-directory $reportDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Soak test failed. Inspect TestResults.' }
    Write-Host "Resource measurements: $env:NETWATCH_SOAK_REPORT"
}
finally {
    $env:NETWATCH_WEBVIEW2_TESTS = $previousWebView
    $env:NETWATCH_SOAK_MINUTES = $previousMinutes
    $env:NETWATCH_SOAK_REPORT = $previousReport
}
