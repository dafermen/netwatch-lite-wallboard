[CmdletBinding()]
param(
    [string]$RepositoryRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepositoryRoot = Join-Path $scriptRoot ".."
}

$repoRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path.TrimEnd('\', '/')
$docsRoot = Join-Path $repoRoot "docs"
$docsIndexPath = Join-Path $docsRoot "README.md"
$adrIndexPath = Join-Path $docsRoot "adr\README.md"
$errors = New-Object System.Collections.Generic.List[string]
$localLinkCount = 0
$imageCount = 0

if (-not (Test-Path -LiteralPath $docsIndexPath)) {
    throw "Documentation index was not found: $docsIndexPath"
}

$markdownFiles = @()
$markdownFiles += Get-ChildItem -LiteralPath $repoRoot -File -Filter "*.md"

foreach ($contentDirectory in @("docs", "tests", ".github")) {
    $contentPath = Join-Path $repoRoot $contentDirectory
    if (Test-Path -LiteralPath $contentPath) {
        $markdownFiles += Get-ChildItem -LiteralPath $contentPath -Recurse -File -Filter "*.md"
    }
}

$linkPattern = '(?<!!)\[[^\]]+\]\((?<target>[^)]+)\)'
$imagePattern = '!\[(?<alt>[^\]]*)\]\((?<target>[^)]+)\)'

function Get-RelativeDisplayPath {
    param([System.IO.FileInfo]$File)

    return $File.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
}

function Get-LinkPath {
    param([string]$RawTarget)

    $target = $RawTarget.Trim()

    if ($target.StartsWith('<') -and $target.Contains('>')) {
        $target = $target.Substring(1, $target.IndexOf('>') - 1)
    }
    else {
        $target = $target -replace '\s+"[^"]*"$', ''
    }

    return $target
}

foreach ($file in $markdownFiles) {
    $displayPath = Get-RelativeDisplayPath -File $file
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $isRootDocument = $file.DirectoryName -eq $repoRoot
    $isDocsDocument = $file.FullName.StartsWith(
        $docsRoot,
        [System.StringComparison]::OrdinalIgnoreCase)

    if ($isRootDocument -or $isDocsDocument) {
        $headingCount = [regex]::Matches($content, '(?m)^#\s+\S').Count
        if ($headingCount -ne 1) {
            $errors.Add("$displayPath must contain exactly one top-level heading; found $headingCount.")
        }
    }

    if ($isDocsDocument) {
        if (-not $content.Contains('<!-- docs-nav -->') -or -not $content.Contains('<!-- /docs-nav -->')) {
            $errors.Add("$displayPath is missing the docs-nav block.")
        }

        if ($file.FullName -ne $docsIndexPath) {
            $tailStart = [Math]::Max(0, $content.Length - 700)
            $tail = $content.Substring($tailStart)

            if (-not $tail.Contains('[Documentation home]')) {
                $errors.Add("$displayPath is missing footer navigation back to the documentation home.")
            }
        }
    }

    foreach ($match in [regex]::Matches($content, $linkPattern)) {
        $target = Get-LinkPath -RawTarget $match.Groups['target'].Value

        if ($target -match '^(?i:https?://|mailto:|#)' -or $target -match '^[A-Za-z]:[\\/]') {
            continue
        }

        if ($target.StartsWith('/')) {
            $errors.Add("$displayPath uses unsupported root-relative documentation link '$target'.")
            continue
        }

        $pathPart = ($target -split '#', 2)[0]
        if ([string]::IsNullOrWhiteSpace($pathPart)) {
            continue
        }

        $normalizedTarget = $pathPart.Replace('\', '/')
        if ($normalizedTarget -match '(?i)(^|/)docs/docs(/|$)') {
            $errors.Add("$displayPath contains duplicated documentation path '$target'.")
        }

        $resolvedPath = Join-Path $file.DirectoryName ([Uri]::UnescapeDataString($pathPart))
        if (-not (Test-Path -LiteralPath $resolvedPath)) {
            $errors.Add("$displayPath contains broken local link '$target'.")
        }
        else {
            $localLinkCount++
        }
    }

    foreach ($match in [regex]::Matches($content, $imagePattern)) {
        $imageCount++
        $altText = $match.Groups['alt'].Value.Trim()
        $target = Get-LinkPath -RawTarget $match.Groups['target'].Value

        if ([string]::IsNullOrWhiteSpace($altText)) {
            $errors.Add("$displayPath contains an image without alternative text.")
        }

        if ($target -notmatch '^(?i:https?://|data:)' -and
            $target -notmatch '^[A-Za-z]:[\\/]' -and
            -not $target.StartsWith('/')) {
            $pathPart = ($target -split '#', 2)[0]
            $resolvedPath = Join-Path $file.DirectoryName ([Uri]::UnescapeDataString($pathPart))

            if (-not (Test-Path -LiteralPath $resolvedPath)) {
                $errors.Add("$displayPath contains broken local image '$target'.")
            }
        }
    }
}

$docsIndex = [System.IO.File]::ReadAllText($docsIndexPath)
$topLevelDocs = Get-ChildItem -LiteralPath $docsRoot -File -Filter "*.md" |
    Where-Object { $_.Name -ne "README.md" }

foreach ($document in $topLevelDocs) {
    if ($docsIndex.IndexOf($document.Name, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        $errors.Add("docs/README.md does not link to docs/$($document.Name).")
    }
}

if (Test-Path -LiteralPath $adrIndexPath) {
    $adrIndex = [System.IO.File]::ReadAllText($adrIndexPath)
    $adrDocuments = Get-ChildItem -LiteralPath (Split-Path -Parent $adrIndexPath) -File -Filter "*.md" |
        Where-Object { $_.Name -ne "README.md" }

    foreach ($document in $adrDocuments) {
        if ($adrIndex.IndexOf($document.Name, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            $errors.Add("docs/adr/README.md does not link to docs/adr/$($document.Name).")
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Documentation validation failed:" -ForegroundColor Red
    foreach ($validationError in $errors) {
        Write-Host "  - $validationError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Documentation validation passed." -ForegroundColor Green
Write-Host "  Markdown files: $($markdownFiles.Count)"
Write-Host "  Valid local links: $localLinkCount"
Write-Host "  Images with alt text: $imageCount"
