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

if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$' -or
    $Runtime -notmatch '^win-(x64|x86|arm64)$') {
    throw "Use a valid version and supported Windows runtime."
}

# Validate exact output targets before replacing a generated package.
$publishRoot = [IO.Path]::GetFullPath($publishRoot)
$outputPath = [IO.Path]::GetFullPath($outputPath)
$zipPath = [IO.Path]::GetFullPath($zipPath)
if (-not $outputPath.StartsWith($publishRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Portable output must remain inside the repository publish directory."
}
if ((Test-Path -LiteralPath $outputPath) -and
    ((Get-Item -LiteralPath $outputPath).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw "Refusing to replace a redirected portable output directory."
}

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

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE. No ZIP was created."
}

$readmePath = Join-Path $repoRoot "README.md"
$licensePath = Join-Path $repoRoot "LICENSE"

if (Test-Path $readmePath) {
    Copy-Item -LiteralPath $readmePath -Destination $outputPath -Force
}

if (Test-Path $licensePath) {
    Copy-Item -LiteralPath $licensePath -Destination $outputPath -Force
}

foreach ($document in @('AGENTS.md', 'CURRENT_STATUS.md', 'CHANGELOG.md', 'CONTRIBUTING.md', 'THIRD_PARTY_LICENSES.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $document) -Destination $outputPath -Force
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs') -Destination (Join-Path $outputPath 'docs') -Recurse -Force

foreach ($requiredFile in @('NetWatch-Lite-Wallboard.exe', 'wallboard.json', 'wallboard.demo.json', 'wallboard.production.json', 'README.md', 'LICENSE', 'THIRD_PARTY_LICENSES.md', 'docs\user-guide.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $outputPath $requiredFile) -PathType Leaf)) {
        throw "Portable output is missing $requiredFile."
    }
}
if (Get-ChildItem -LiteralPath $outputPath -Recurse -File | Where-Object { $_.Name -like '*.backup.json' -or $_.Extension -eq '.log' }) {
    throw "Portable output contains a configuration backup or log."
}

if (-not $NoZip) {
    Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $zipPath -Force
    $archiveHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$zipPath.sha256", "$archiveHash  $([IO.Path]::GetFileName($zipPath))" + [Environment]::NewLine)
}

Write-Host "Portable publish complete:"
Write-Host "  Folder: $outputPath"

if (-not $NoZip) {
    Write-Host "  ZIP:    $zipPath"
}
