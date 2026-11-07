param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$Version = '',
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\NetWatchLite.Wallboard.WebView2\NetWatchLite.Wallboard.WebView2.csproj'
. (Join-Path $PSScriptRoot 'portable-package.ps1')

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content -LiteralPath $projectPath
    $Version = [string]$projectXml.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$' -or
    $Runtime -notmatch '^win-(x64|x86|arm64)$') {
    throw 'Use a valid full version and supported Windows runtime.'
}

$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$outputName = "NetWatch-Lite-Wallboard-v$Version-$Runtime"
$outputPath = Join-Path $artifactsRoot "portable\$outputName"
$zipPath = Join-Path $artifactsRoot "packages\$outputName.zip"
$stagingPath = Join-Path $artifactsRoot ('.staging\' + [guid]::NewGuid().ToString('N'))

# Never replace an existing deployment, ZIP, or redirected directory. Failed builds remain in staging.
foreach ($path in @($artifactsRoot, (Join-Path $artifactsRoot 'portable'), (Join-Path $artifactsRoot 'packages'), (Join-Path $artifactsRoot '.staging'))) {
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing a redirected artifacts directory: $path"
    }
}
foreach ($path in @($outputPath, $zipPath, "$zipPath.sha256", $stagingPath)) {
    $absolutePath = [IO.Path]::GetFullPath($path)
    if (-not $absolutePath.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Generated output must remain inside artifacts/.'
    }
    if (Test-Path -LiteralPath $absolutePath) {
        throw "Output already exists; use a new version or archive it manually: $absolutePath"
    }
}

New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null
$appPath = Join-Path $stagingPath 'app'
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained true `
    -p:Version=$Version -p:PublishSingleFile=false -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false -p:DebugType=None -o $appPath
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE. No package was published." }

$assets = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $projectPath) 'obj\project.assets.json') -Raw | ConvertFrom-Json
$runtimeConfig = Get-Content -LiteralPath (Join-Path $appPath 'NetWatch-Lite-Wallboard.runtimeconfig.json') -Raw | ConvertFrom-Json
$coreRuntime = $runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App'
$desktopRuntime = $runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App'
$webViewPackage = $assets.libraries.PSObject.Properties | Where-Object Name -Like 'Microsoft.Web.WebView2/*'
function Copy-PackageNotices([string]$Package, [string]$PackageVersion, [string[]]$Files, [string]$Destination) {
    $target = Join-Path $stagingPath "licenses\$Destination"
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    foreach ($file in $Files) {
        $source = $null
        foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
            $candidate = Join-Path $folder "$Package\$PackageVersion\$file"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $source = $candidate; break }
        }
        if (-not $source) { throw "Required notice not found for $Package/$PackageVersion`: $file" }
        Copy-Item -LiteralPath $source -Destination (Join-Path $target $file)
    }
}
Copy-PackageNotices "microsoft.netcore.app.runtime.$Runtime" $coreRuntime.version @('LICENSE.TXT', 'THIRD-PARTY-NOTICES.TXT') 'dotnet-runtime'
Copy-PackageNotices "microsoft.windowsdesktop.app.runtime.$Runtime" $desktopRuntime.version @('LICENSE') 'windows-desktop'
Copy-PackageNotices 'microsoft.web.webview2' ($webViewPackage.Name -split '/')[1] @('LICENSE.txt', 'NOTICE.txt') 'webview2'

# Move only fresh generated examples, never an existing user's configuration.
Get-ChildItem -LiteralPath $appPath -File -Filter 'wallboard*.json' | ForEach-Object {
    Move-Item -LiteralPath $_.FullName -Destination (Join-Path $stagingPath $_.Name)
}
foreach ($document in @('README.md', 'LICENSE', 'AGENTS.md', 'CURRENT_STATUS.md', 'CHANGELOG.md', 'CONTRIBUTING.md', 'THIRD_PARTY_LICENSES.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $document) -Destination $stagingPath
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs') -Destination (Join-Path $stagingPath 'docs') -Recurse
$layout = [ordered]@{ schemaVersion = 1; version = $Version; runtime = $Runtime; selfContained = $true; appDirectory = 'app' }
[IO.File]::WriteAllText((Join-Path $stagingPath 'portable-layout.json'), ($layout | ConvertTo-Json))
$instructions = @(
    'NetWatch Lite Wallboard portable - extract the COMPLETE ZIP into a new folder.',
    'Open NetWatch-Lite-Wallboard.exe at the ROOT. Keep app/ intact. .NET 10 is included.',
    'Microsoft Edge WebView2 Runtime is still required. The launcher uses Windows .NET Framework 4.x.',
    'Editable configuration: wallboard.json and wallboard.{environment}.json at the ROOT.',
    'Profiles: NetWatch-Lite-Wallboard.exe --environment production',
    'Before updating, export/back up your configuration. Do not overwrite a working deployment.',
    'Copy your chosen JSON profiles and local pages into the new folder; leave app/ unchanged.',
    'Browser sessions and logs keep their existing LocalAppData locations.',
    'Read docs/user-guide.md and docs/DEPLOYMENT.md. Evaluation candidate; long-session acceptance remains pending.'
)
[IO.File]::WriteAllLines((Join-Path $stagingPath 'START-HERE.txt'), $instructions)
& (Join-Path $PSScriptRoot 'build-launcher.ps1') -PortablePath $stagingPath -Version $Version
Assert-PortableContents -PortablePath $stagingPath

New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath), (Split-Path -Parent $zipPath) -Force | Out-Null
if ($NoZip) {
    & (Join-Path $PSScriptRoot 'test-portable.ps1') -PortablePath $stagingPath
} else {
    $stagingZip = "$stagingPath.zip"
    Compress-Archive -Path (Join-Path $stagingPath '*') -DestinationPath $stagingZip
    & (Join-Path $PSScriptRoot 'test-portable.ps1') -ZipPath $stagingZip
    Move-Item -LiteralPath $stagingZip -Destination $zipPath
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText("$zipPath.sha256", "$hash  $([IO.Path]::GetFileName($zipPath))" + [Environment]::NewLine)
}
Move-Item -LiteralPath $stagingPath -Destination $outputPath
Write-Host "Portable folder: $outputPath"
if (-not $NoZip) { Write-Host "Portable ZIP: $zipPath" }
