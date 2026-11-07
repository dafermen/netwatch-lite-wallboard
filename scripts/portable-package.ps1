# Shared structural checks for publishing and tests. Per-file startup hashes are a later stage.
function Assert-PortableContents {
    param([Parameter(Mandatory = $true)][string]$PortablePath)

    $required = @(
        'NetWatch-Lite-Wallboard.exe', 'portable-layout.json', 'START-HERE.txt',
        'wallboard.json', 'wallboard.demo.json', 'wallboard.production.json',
        'README.md', 'LICENSE', 'THIRD_PARTY_LICENSES.md', 'docs\user-guide.md',
        'licenses\dotnet-runtime\LICENSE.TXT', 'licenses\dotnet-runtime\THIRD-PARTY-NOTICES.TXT',
        'licenses\windows-desktop\LICENSE', 'licenses\webview2\LICENSE.txt', 'licenses\webview2\NOTICE.txt',
        'app\NetWatch-Lite-Wallboard.exe', 'app\NetWatch-Lite-Wallboard.dll',
        'app\NetWatch-Lite-Wallboard.deps.json', 'app\NetWatch-Lite-Wallboard.runtimeconfig.json',
        'app\coreclr.dll', 'app\hostfxr.dll', 'app\hostpolicy.dll', 'app\clrjit.dll',
        'app\System.Private.CoreLib.dll', 'app\System.Windows.Forms.dll',
        'app\WebView2Loader.dll', 'app\Microsoft.Web.WebView2.Core.dll',
        'app\Microsoft.Web.WebView2.WinForms.dll'
    )
    foreach ($relative in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $PortablePath $relative) -PathType Leaf)) {
            throw "Portable output is missing $relative."
        }
    }
    if (Get-ChildItem -LiteralPath $PortablePath -File -Filter '*.dll') {
        throw 'Runtime DLLs must stay inside app/, not the portable root.'
    }
    if (Get-ChildItem -LiteralPath $PortablePath -Recurse -File | Where-Object {
        $_.Name -like '*.backup.json' -or $_.Extension -eq '.log' -or $_.Name -like '*.tmp'
    }) {
        throw 'Portable output contains a backup, log, or temporary file.'
    }
    foreach ($name in @('wallboard.json', 'wallboard.demo.json', 'wallboard.production.json')) {
        Get-Content -LiteralPath (Join-Path $PortablePath $name) -Raw | ConvertFrom-Json | Out-Null
        if (Test-Path -LiteralPath (Join-Path $PortablePath "app\$name")) {
            throw "Editable configuration must stay at the root: $name."
        }
    }
    $layout = Get-Content -LiteralPath (Join-Path $PortablePath 'portable-layout.json') -Raw | ConvertFrom-Json
    if ($layout.schemaVersion -ne 1 -or $layout.appDirectory -ne 'app' -or -not $layout.selfContained) {
        throw 'Invalid portable layout metadata.'
    }
    $runtime = Get-Content -LiteralPath (Join-Path $PortablePath 'app\NetWatch-Lite-Wallboard.runtimeconfig.json') -Raw | ConvertFrom-Json
    $desktop = @($runtime.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App')
    if ($desktop.Count -ne 1 -or ([version]$desktop[0].version).Major -ne 10) {
        throw 'The package must include the .NET 10 Desktop Runtime.'
    }
    $version = (Get-Item -LiteralPath (Join-Path $PortablePath 'app\NetWatch-Lite-Wallboard.exe')).VersionInfo.ProductVersion
    if (($version -split '\+')[0] -ne $layout.version) {
        throw 'Package metadata and executable versions do not match.'
    }
}
