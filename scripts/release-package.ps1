function Assert-PrereleaseIdentity {
    param([string]$Tag, [string]$Version, [string]$SourceCommit)
    if ($Tag -cnotmatch '^v\d+\.\d+\.\d+-rc\.\d+$' -or $Tag -cne ('v' + $Version)) {
        throw 'Only a version-matching vX.Y.Z-rc.N prerelease tag is allowed.'
    }
    if ($SourceCommit -cnotmatch '^[0-9a-f]{40}$') { throw 'Use the exact full lowercase source commit SHA.' }
}

function Assert-ReleaseAssets {
    param([string]$PackageDirectory, [string]$Version, [string]$SourceCommit)
    Assert-PrereleaseIdentity -Tag ('v' + $Version) -Version $Version -SourceCommit $SourceCommit
    $name = "NetWatch-Lite-Wallboard-v$Version-win-x64.zip"
    $zip = Join-Path ([IO.Path]::GetFullPath($PackageDirectory)) $name
    $checksum = "$zip.sha256"
    foreach ($path in @($zip, $checksum)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing release asset: $path" }
        if ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Redirected release asset refused.' }
    }
    if ((Get-Item -LiteralPath $checksum).Length -gt 1024) { throw 'Invalid checksum sidecar length.' }
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    if ([IO.File]::ReadAllText($checksum).Trim() -cne "$hash  $name") { throw 'Release ZIP checksum mismatch.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    $temporaryExe = Join-Path ([IO.Path]::GetTempPath()) ('netwatch-release-' + [guid]::NewGuid().ToString('N') + '.exe')
    try {
        $entries = @($archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -ceq 'app/NetWatch-Lite-Wallboard.exe' })
        if ($entries.Count -ne 1 -or $entries[0].Length -gt 2MB) { throw 'Invalid portable app executable entry.' }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $temporaryExe)
        $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($temporaryExe).ProductVersion
        if ($productVersion -cne "$Version+$SourceCommit") { throw 'ZIP ProductVersion does not match the exact source commit/version.' }
    } finally {
        $archive.Dispose()
        if (Test-Path -LiteralPath $temporaryExe) { Remove-Item -LiteralPath $temporaryExe }
    }
    return [pscustomobject]@{ Zip = $zip; Checksum = $checksum; Hash = $hash;
        ChecksumHash = (Get-FileHash -LiteralPath $checksum).Hash.ToLowerInvariant(); ProductVersion = $productVersion }
}

function Complete-PrereleaseDraft {
    param([string]$Tag, [string]$Repository, [string]$SourceCommit, $Assets, [string]$VerificationDirectory, [bool]$Publish)
    $draftJson = gh release view $Tag --repo $Repository --json tagName,isDraft,isPrerelease,targetCommitish,url
    if ($LASTEXITCODE -ne 0) { throw 'Cannot confirm the draft before verification.' }
    $draft = $draftJson | ConvertFrom-Json
    if (-not $draft.isDraft -or -not $draft.isPrerelease -or $draft.tagName -cne $Tag -or $draft.targetCommitish -cne $SourceCommit) {
        throw 'Draft identity mismatch; public publication refused.'
    }
    New-Item -ItemType Directory -Path $VerificationDirectory -ErrorAction Stop | Out-Null
    gh release download $Tag --repo $Repository --dir $VerificationDirectory --pattern ([IO.Path]::GetFileName($Assets.Zip) + '*')
    if ($LASTEXITCODE -ne 0) { throw 'Download verification failed; release remains a draft.' }
    foreach ($entry in @(@($Assets.Zip, $Assets.Hash), @($Assets.Checksum, $Assets.ChecksumHash))) {
        $remote = Join-Path $VerificationDirectory ([IO.Path]::GetFileName($entry[0]))
        if ((Get-FileHash -LiteralPath $remote).Hash.ToLowerInvariant() -cne $entry[1] -or
            (Get-FileHash -LiteralPath $entry[0]).Hash.ToLowerInvariant() -cne $entry[1]) {
            throw 'Uploaded/downloaded or local asset changed; release remains a draft.'
        }
    }
    if ($Publish) {
        gh release edit $Tag --repo $Repository --draft=false --prerelease --latest=false
        if ($LASTEXITCODE -ne 0) { throw 'Publishing the verified prerelease failed; inspect the draft.' }
    }
    $resultJson = gh release view $Tag --repo $Repository --json tagName,isDraft,isPrerelease,targetCommitish,url
    if ($LASTEXITCODE -ne 0) { throw 'Could not confirm final release state.' }
    $result = $resultJson | ConvertFrom-Json
    if (-not $result.isPrerelease -or $result.tagName -cne $Tag -or $result.targetCommitish -cne $SourceCommit -or $result.isDraft -ne (-not $Publish)) {
        throw 'Final release metadata mismatch.'
    }
    [IO.File]::WriteAllText((Join-Path $VerificationDirectory 'release-verification.json'), $resultJson)
    return $resultJson
}
