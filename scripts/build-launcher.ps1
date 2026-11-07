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

$PortablePath = [IO.Path]::GetFullPath($PortablePath)
$appPath = Join-Path $PortablePath 'app'
$layoutPath = Join-Path $PortablePath 'portable-layout.json'
if (-not (Test-Path -LiteralPath $appPath -PathType Container) -or -not (Test-Path -LiteralPath $layoutPath -PathType Leaf)) {
    throw 'Build the complete payload and portable layout before compiling the launcher.'
}
# Enumerate without following redirected directories. Hash only fresh immutable output, never editable profiles.
$files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$files.Add((Get-Item -LiteralPath $layoutPath))
$directories = [System.Collections.Generic.Queue[string]]::new()
$directories.Enqueue($appPath)
while ($directories.Count) {
    $directory = $directories.Dequeue()
    if ((Get-Item -LiteralPath $directory).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Redirected payload directory refused.' }
    foreach ($item in (Get-ChildItem -LiteralPath $directory -Force)) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Redirected payload entry refused: $($item.Name)" }
        if ($item.PSIsContainer) { $directories.Enqueue($item.FullName) } else { $files.Add($item) }
    }
}
if ((Get-Item -LiteralPath $layoutPath).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Redirected layout metadata refused.' }
if ($files | Where-Object { $_.Name -like 'wallboard*.json' -or $_.Name -like '*.backup.json' -or $_.Extension -in @('.log', '.tmp') }) {
    throw 'Mutable configuration/logs must not be in the protected payload.'
}
$manifestPath = Join-Path $PortablePath 'integrity-manifest.xml'
$settings = [Xml.XmlWriterSettings]::new()
$settings.Encoding = [Text.UTF8Encoding]::new($false)
$settings.Indent = $true
$writer = [Xml.XmlWriter]::Create($manifestPath, $settings)
try {
    $writer.WriteStartDocument()
    $writer.WriteStartElement('package')
    $writer.WriteAttributeString('schema', '1')
    $writer.WriteAttributeString('version', $Version)
    foreach ($file in ($files | Sort-Object FullName)) {
        $relative = $file.FullName.Substring($PortablePath.TrimEnd('\').Length + 1).Replace('\', '/')
        $writer.WriteStartElement('file')
        $writer.WriteAttributeString('path', $relative)
        $writer.WriteAttributeString('length', $file.Length.ToString([Globalization.CultureInfo]::InvariantCulture))
        $writer.WriteAttributeString('sha256', (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash)
        $writer.WriteEndElement()
    }
    $writer.WriteEndElement()
    $writer.WriteEndDocument()
} finally { $writer.Dispose() }

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
    "/resource:$manifestPath,NetWatchLite.Wallboard.Integrity.xml" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Windows.Forms.dll `
    (Join-Path $repoRoot 'src\NetWatchLite.Wallboard.Launcher\Launcher.cs') `
    (Join-Path $repoRoot 'src\NetWatchLite.Wallboard.Launcher\PackageIntegrity.cs') $assemblyInfo
if ($LASTEXITCODE -ne 0) { throw 'Portable launcher compilation failed.' }
