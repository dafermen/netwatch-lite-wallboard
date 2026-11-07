[CmdletBinding(DefaultParameterSetName = 'Folder')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Folder')][string]$PortablePath,
    [Parameter(Mandatory = $true, ParameterSetName = 'Archive')][string]$ZipPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'portable-package.ps1')
$resultsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts\verification'))
foreach ($path in @((Join-Path $repoRoot 'artifacts'), $resultsRoot)) {
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Verification output cannot be a redirected directory.'
    }
}
$resultPath = Join-Path $resultsRoot ([guid]::NewGuid().ToString('N'))
$copyPath = Join-Path $resultPath 'package with spaces'
New-Item -ItemType Directory -Path $copyPath -Force | Out-Null
if ($PSCmdlet.ParameterSetName -eq 'Archive') {
    Expand-Archive -LiteralPath $ZipPath -DestinationPath $copyPath
} else {
    Get-ChildItem -LiteralPath $PortablePath | Copy-Item -Destination $copyPath -Recurse
}
Assert-PortableContents -PortablePath $copyPath
$script:checks = [System.Collections.Generic.List[string]]::new()

function Assert-Check([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Portable test failed: $Name" }
    $script:checks.Add($Name)
    Write-Host "PASS: $Name"
}

function ConvertTo-WindowsArgument([string]$Value) {
    # Windows argv quoting, including quotes and trailing backslashes in paths.
    return '"' + [regex]::Replace([regex]::Replace($Value, '(\\*)"', '$1$1\"'), '(\\+)$', '$1$1') + '"'
}

function Invoke-PackageProbe([string]$Name, [string[]]$Arguments = @(), [string]$EnvironmentName = '', [switch]$Direct, [int]$ExpectedExitCode = 0) {
    $report = Join-Path $resultPath "$Name report.json"
    $executable = Join-Path $copyPath 'NetWatch-Lite-Wallboard.exe'
    if ($Direct) { $executable = Join-Path $copyPath 'app\NetWatch-Lite-Wallboard.exe' }
    $start = [Diagnostics.ProcessStartInfo]::new($executable)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.WorkingDirectory = $resultPath
    $allArguments = @('--package-check', $report) + $Arguments
    $start.Arguments = ($allArguments | ForEach-Object { ConvertTo-WindowsArgument $_ }) -join ' '
    $start.EnvironmentVariables['NETWATCH_WALLBOARD_ENV'] = $EnvironmentName
    # A real local runtime must work even when no global runtime location is usable.
    foreach ($variable in @('DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_ROOT_X86', 'DOTNET_ROOT_ARM64')) {
        $start.EnvironmentVariables[$variable] = Join-Path $resultPath 'no installed dotnet'
    }
    $start.EnvironmentVariables['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    $process = [Diagnostics.Process]::Start($start)
    try {
        if (-not $process.WaitForExit(60000)) {
            $process.Kill()
            throw "Package probe timed out: $Name"
        }
        Assert-Check ($process.ExitCode -eq $ExpectedExitCode) "$Name returns expected exit code $ExpectedExitCode"
    } finally { $process.Dispose() }
    if ($ExpectedExitCode -ne 0) {
        Assert-Check (-not (Test-Path -LiteralPath $report)) "$Name blocks the child before a runtime report is created"
        return
    }
    $data = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    Assert-Check (([version]$data.RuntimeVersion).Major -eq 10) "$Name runs .NET 10"
    Assert-Check ($data.EmbeddedIconAvailable -eq $true) "$Name contains the embedded window icon"
    $runtimeRoot = [IO.Path]::GetFullPath((Join-Path $copyPath 'app')) + [IO.Path]::DirectorySeparatorChar
    Assert-Check ($data.RuntimeDirectory.StartsWith($runtimeRoot, [StringComparison]::OrdinalIgnoreCase)) "$Name uses the bundled runtime"
    Assert-Check ($data.ContentDirectory -eq $copyPath) "$Name resolves the portable root from another working directory"
    Assert-Check (($data.Arguments | ConvertTo-Json -Compress) -eq ($allArguments | ConvertTo-Json -Compress)) "$Name preserves every argument"
    return $data
}

$originalHashes = @{}
foreach ($file in (Get-ChildItem -LiteralPath $copyPath -File -Filter 'wallboard*.json')) {
    $originalHashes[$file.Name] = (Get-FileHash -LiteralPath $file.FullName).Hash
}
foreach ($profile in @('', 'demo', 'production')) {
    $name = if ($profile) { $profile } else { 'default' }
    [string[]]$arguments = @()
    if ($profile) { $arguments = @('--environment', $profile) }
    $data = Invoke-PackageProbe -Name $name -Arguments $arguments
    $fileName = if ($profile) { "wallboard.$profile.json" } else { 'wallboard.json' }
    Assert-Check ($data.ConfigurationPath -eq (Join-Path $copyPath $fileName)) "$name reads the correct profile"
    Assert-Check ($data.WritableConfigurationPath -eq (Join-Path $copyPath $fileName)) "$name saves to the same root profile"
    $expected = Get-Content -LiteralPath (Join-Path $copyPath $fileName) -Raw | ConvertFrom-Json
    Assert-Check ($data.AppTitle -eq $expected.appTitle) "$name loads the existing configuration"
}
$data = Invoke-PackageProbe -Name 'environment-variable' -EnvironmentName 'demo'
Assert-Check ($data.EnvironmentName -eq 'demo') 'environment variable selects the profile'
$data = Invoke-PackageProbe -Name 'cli-precedence' -EnvironmentName 'demo' -Arguments @('--environment=production', 'with spaces', 'embedded"quote', 'trailing\', '')
Assert-Check ($data.EnvironmentName -eq 'production') 'command line overrides the environment variable'
$data = Invoke-PackageProbe -Name 'env-alias' -Arguments @('--env', 'demo')
Assert-Check ($data.EnvironmentName -eq 'demo') '--env alias is preserved'
$data = Invoke-PackageProbe -Name 'config-env-alias' -Arguments @('--config-env=production')
Assert-Check ($data.EnvironmentName -eq 'production') '--config-env alias is preserved'
$data = Invoke-PackageProbe -Name 'direct-payload' -Arguments @('--environment', 'production') -Direct
Assert-Check ($data.EnvironmentName -eq 'production') 'direct payload also resolves the root profile'
foreach ($entry in $originalHashes.GetEnumerator()) {
    Assert-Check ((Get-FileHash -LiteralPath (Join-Path $copyPath $entry.Key)).Hash -eq $entry.Value) "$($entry.Key) is unchanged after startup checks"
}

# Only this disposable copy is edited; never use an operator's browser profile or live configuration.
$customPath = Join-Path $copyPath 'wallboard.night.json'
[IO.File]::WriteAllText($customPath, '{"appTitle":"Custom night profile","panels":[{"url":"https://example.org/"}]}')
$customHash = (Get-FileHash -LiteralPath $customPath).Hash
$data = Invoke-PackageProbe -Name 'custom-profile' -Arguments @('--environment', 'night')
Assert-Check ($data.AppTitle -eq 'Custom night profile') 'custom profile content is preserved'
Assert-Check ((Get-FileHash -LiteralPath $customPath).Hash -eq $customHash) 'custom profile is never rewritten by the probe'
$data = Invoke-PackageProbe -Name 'missing-profile' -Arguments @('--environment', 'new-profile')
Assert-Check ($data.ConfigurationPath -eq (Join-Path $copyPath 'wallboard.json')) 'missing profile retains the default-file fallback'
Assert-Check ($data.WritableConfigurationPath -eq (Join-Path $copyPath 'wallboard.new-profile.json')) 'missing profile saves to its own new filename'

foreach ($relative in @('app\hostfxr.dll', 'app\System.Windows.Forms.dll', 'wallboard.production.json', 'portable-layout.json')) {
    $path = Join-Path $copyPath $relative
    $backup = "$path.test-backup"
    Move-Item -LiteralPath $path -Destination $backup
    try {
        $detected = $false
        try { Assert-PortableContents -PortablePath $copyPath } catch { $detected = $_.Exception.Message.Contains($relative) }
        Assert-Check $detected "incomplete package detected: $relative"
    } finally { Move-Item -LiteralPath $backup -Destination $path }
}
Assert-PortableContents -PortablePath $copyPath
$script:integrityTimings = [System.Collections.Generic.List[object]]::new()

function Invoke-IntegrityCheck([string]$Name, [int]$ExpectedExitCode = 0, [string]$ExpectedText = 'PASS:', [string]$ReportPath = '') {
    if (-not $ReportPath) { $ReportPath = Join-Path $resultPath "integrity-$Name.txt" }
    $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $copyPath 'NetWatch-Lite-Wallboard.exe'))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.WorkingDirectory = $resultPath
    $start.Arguments = (@('--verify-only', '--report', $ReportPath) | ForEach-Object { ConvertTo-WindowsArgument $_ }) -join ' '
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $process = [Diagnostics.Process]::Start($start)
    try {
        if (-not $process.WaitForExit(60000)) { $process.Kill(); throw "Integrity check timed out: $Name" }
        Assert-Check ($process.ExitCode -eq $ExpectedExitCode) "integrity $Name returns expected exit code $ExpectedExitCode"
    } finally { $process.Dispose(); $timer.Stop() }
    $script:integrityTimings.Add([pscustomobject]@{ Case = $Name; Milliseconds = $timer.ElapsedMilliseconds })
    if ($ExpectedText) {
        $report = [IO.File]::ReadAllText($ReportPath)
        Assert-Check ($report.Contains($ExpectedText)) "integrity $Name has the expected diagnostic"
    }
}

# These paths are inside this run's newly created disposable copy, never the supplied package.
Invoke-IntegrityCheck 'complete'
foreach ($relative in @('app/hostfxr.dll', 'app/NetWatch-Lite-Wallboard.dll', 'portable-layout.json', 'integrity-manifest.xml')) {
    $path = Join-Path $copyPath $relative
    $backup = Join-Path $resultPath ('saved-' + [IO.Path]::GetFileName($relative))
    Move-Item -LiteralPath $path -Destination $backup
    try {
        Invoke-IntegrityCheck ('missing-' + [IO.Path]::GetFileName($relative)) 1 $relative
        if ($relative -eq 'app/hostfxr.dll') { Invoke-PackageProbe -Name 'blocked-missing-runtime' -ExpectedExitCode 1 }
    } finally { Move-Item -LiteralPath $backup -Destination $path }
}
foreach ($relative in @('app/coreclr.dll', 'portable-layout.json')) {
    $path = Join-Path $copyPath $relative
    $stream = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $offset = [Math]::Min(4096, $stream.Length - 1)
        $stream.Position = $offset
        $originalByte = $stream.ReadByte()
        $stream.Position = $offset
        $stream.WriteByte([byte]($originalByte -bxor 1))
    } finally { $stream.Dispose() }
    try {
        Invoke-IntegrityCheck ('corrupt-' + [IO.Path]::GetFileName($relative)) 1 $relative
        if ($relative -eq 'app/coreclr.dll') { Invoke-PackageProbe -Name 'blocked-corrupt-runtime' -ExpectedExitCode 1 }
    } finally {
        $stream = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $stream.Position = $offset; $stream.WriteByte([byte]$originalByte) } finally { $stream.Dispose() }
    }
}
$lockedPath = Join-Path $copyPath 'app\NetWatch-Lite-Wallboard.dll'
$lock = [IO.File]::Open($lockedPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
try {
    Invoke-IntegrityCheck 'unreadable-file' 1 'app/NetWatch-Lite-Wallboard.dll'
    Invoke-PackageProbe -Name 'blocked-unreadable-file' -ExpectedExitCode 1
} finally { $lock.Dispose() }

$manifestPath = Join-Path $copyPath 'integrity-manifest.xml'
$originalManifest = [IO.File]::ReadAllBytes($manifestPath)
try {
    [IO.File]::AppendAllText($manifestPath, ' ')
    Invoke-IntegrityCheck 'changed-manifest' 1 'Changed manifest:'
} finally { [IO.File]::WriteAllBytes($manifestPath, $originalManifest) }
$extra = Join-Path $copyPath 'app\stale.dll'
try {
    [IO.File]::WriteAllText($extra, 'Unlisted old binary fixture')
    Invoke-IntegrityCheck 'unexpected-file' 1 'Unexpected payload file: app/stale.dll'
    Invoke-PackageProbe -Name 'blocked-unexpected-file' -ExpectedExitCode 1
} finally { [IO.File]::Delete($extra) }

$editable = Join-Path $copyPath 'wallboard.production.json'
$originalConfiguration = [IO.File]::ReadAllBytes($editable)
try {
    $custom = [IO.File]::ReadAllText($editable) | ConvertFrom-Json
    $custom.appTitle = 'Operator customized production'
    [IO.File]::WriteAllText($editable, ($custom | ConvertTo-Json -Depth 20))
    $customHash = (Get-FileHash -LiteralPath $editable).Hash
    Invoke-IntegrityCheck 'customized-profile'
    $data = Invoke-PackageProbe -Name 'customized-production' -Arguments @('--environment', 'production')
    Assert-Check ($data.AppTitle -eq $custom.appTitle) 'integrity preserves customized production selection'
    Assert-Check ((Get-FileHash -LiteralPath $editable).Hash -eq $customHash) 'integrity and startup do not rewrite the custom profile'
    Invoke-IntegrityCheck 'report-cannot-overwrite-profile' 2 '' $editable
    Assert-Check ((Get-FileHash -LiteralPath $editable).Hash -eq $customHash) 'integrity report refuses to overwrite existing configuration'
    [IO.File]::WriteAllText($editable, '{invalid editable JSON')
    Invoke-IntegrityCheck 'malformed-json-still-outside-integrity'
    Assert-Check ([IO.File]::ReadAllText($editable) -eq '{invalid editable JSON') 'integrity leaves malformed editable JSON untouched'
} finally { [IO.File]::WriteAllBytes($editable, $originalConfiguration) }

# Test a junction without inspecting any external/operator directory. Its target is our own moved test payload.
$app = Join-Path $copyPath 'app'
$appBackup = Join-Path $resultPath 'junction-target-app'
foreach ($path in @($app, $appBackup)) {
    if (-not ([IO.Path]::GetFullPath($path)).StartsWith($resultPath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Junction test path is outside its disposable run.'
    }
}
Move-Item -LiteralPath $app -Destination $appBackup
try {
    New-Item -ItemType Junction -Path $app -Target $appBackup | Out-Null
    Invoke-IntegrityCheck 'redirected-payload' 1 'Redirected payload path:'
} finally {
    if (Test-Path -LiteralPath $app) {
        if (-not ((Get-Item -LiteralPath $app).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Refusing to remove a non-junction test path.' }
        [IO.Directory]::Delete($app)
    }
    Move-Item -LiteralPath $appBackup -Destination $app
}
Invoke-IntegrityCheck 'restored-package'
Assert-PortableContents -PortablePath $copyPath
[IO.File]::WriteAllText((Join-Path $resultPath 'integrity-timings.json'), ($script:integrityTimings | ConvertTo-Json))
[IO.File]::WriteAllText((Join-Path $resultPath 'results.json'), ($script:checks | ConvertTo-Json))
Write-Host "Portable checks passed: $($script:checks.Count). Evidence: $resultPath"
