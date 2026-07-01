param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "",
    [switch]$NoZip
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$projectPath = Join-Path $repoRoot "src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj"

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content $projectPath
    $Version = $projectXml.Project.PropertyGroup.Version
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "dev"
}

$publishRoot = Join-Path $repoRoot "publish"
$outputName = "NetWatch-Lite-Wallboard-WebView2-$Runtime-v$Version"
$outputPath = Join-Path $publishRoot $outputName
$zipPath = "$outputPath.zip"

if (Test-Path $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -o $outputPath

$readmePath = Join-Path $repoRoot "README.md"
$licensePath = Join-Path $repoRoot "LICENSE"

if (Test-Path $readmePath) {
    Copy-Item -LiteralPath $readmePath -Destination $outputPath -Force
}

if (Test-Path $licensePath) {
    Copy-Item -LiteralPath $licensePath -Destination $outputPath -Force
}

if (-not $NoZip) {
    Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $zipPath -Force
}

Write-Host "Portable publish complete:"
Write-Host "  Folder: $outputPath"

if (-not $NoZip) {
    Write-Host "  ZIP:    $zipPath"
}
