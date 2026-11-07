param(
    [Parameter(Mandatory = $true)][string]$PortablePath,
    [Parameter(Mandatory = $true)][string]$Version
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'The Windows .NET Framework C# compiler is required to build the portable launcher.'
}
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$') {
    throw 'Invalid launcher version.'
}

$buildDirectory = Join-Path $repoRoot ('artifacts\.staging\launcher-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null
$assemblyInfo = Join-Path $buildDirectory 'AssemblyInfo.cs'
$numericVersion = ($Version -split '-')[0] + '.0'
$attributes = @"
using System.Reflection;
[assembly: AssemblyTitle("NetWatch Lite Wallboard Portable Launcher")]
[assembly: AssemblyVersion("$numericVersion")]
[assembly: AssemblyFileVersion("$numericVersion")]
[assembly: AssemblyInformationalVersion("$Version")]
"@
[IO.File]::WriteAllText($assemblyInfo, $attributes)

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ `
    "/out:$PortablePath\NetWatch-Lite-Wallboard.exe" `
    "/win32icon:$repoRoot\Assets\netwatch-lite.ico" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll `
    (Join-Path $repoRoot 'src\NetWatchLite.Wallboard.Launcher\Launcher.cs') $assemblyInfo
if ($LASTEXITCODE -ne 0) { throw 'Portable launcher compilation failed.' }
