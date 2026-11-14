function Get-GitHubReleaseTags {
    param([string]$Repository)
    if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'Invalid GitHub repository.' }
    $tags = gh api "repos/$Repository/releases" --paginate --jq '.[].tag_name'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect existing GitHub releases.' }
    return $tags
}

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

function Assert-PromotionRequest {
    param([string]$Tag, [string]$SourceCommit, [string]$ExpectedZipSha256, [string]$Repository, [bool]$Publish, [string]$Approval)
    if ($Tag -cnotmatch '^v\d+\.\d+\.\d+-rc\.\d+$') { throw 'Promotion is limited to an existing rc prerelease.' }
    Assert-PrereleaseIdentity -Tag $Tag -Version $Tag.Substring(1) -SourceCommit $SourceCommit
    if ($Repository -notmatch '^[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?/[A-Za-z0-9_-][A-Za-z0-9_.-]*$' -or $ExpectedZipSha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Use a valid repository and the previously verified lowercase ZIP SHA-256.'
    }
    if ($Publish -and $Approval -cne "PUBLISH $Tag") { throw 'Explicit approval must exactly match PUBLISH followed by the tag.' }
}

function Get-ExistingPrereleaseDraft {
    param([string]$Repository, [string]$Tag)
    $viewJson = gh release view $Tag --repo $Repository --json databaseId
    if ($LASTEXITCODE -ne 0) { throw 'Cannot locate the existing draft. Nothing will be created.' }
    $id = ($viewJson | ConvertFrom-Json).databaseId
    if ("$id" -notmatch '^\d+$') { throw 'Invalid existing release identity.' }
    $json = gh api "repos/$Repository/releases/$id"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect existing draft assets.' }
    return ($json | ConvertFrom-Json)
}

function Assert-ExistingPrereleaseDraft {
    param($Draft, [string]$Tag, [string]$SourceCommit, [string]$ExpectedZipSha256)
    if ("$($Draft.id)" -notmatch '^\d+$' -or $Draft.draft -ne $true -or $Draft.prerelease -ne $true -or
        $Draft.tag_name -cne $Tag -or $Draft.target_commitish -cne $SourceCommit) { throw 'Existing draft identity mismatch.' }
    $zipName = "NetWatch-Lite-Wallboard-$Tag-win-x64.zip"
    $assets = @($Draft.assets)
    if ($assets.Count -ne 2) { throw 'Expected exactly the ZIP and checksum assets.' }
    foreach ($name in @($zipName, "$zipName.sha256")) {
        $asset = @($assets | Where-Object { $_.name -ceq $name })
        if ($asset.Count -ne 1 -or $asset[0].state -cne 'uploaded' -or "$($asset[0].id)" -notmatch '^\d+$' -or
            $asset[0].size -le 0 -or $asset[0].size -gt 256MB) { throw 'Missing, duplicate, incomplete or oversized draft asset.' }
    }
    $zip = $assets | Where-Object { $_.name -ceq $zipName }
    $sidecar = $assets | Where-Object { $_.name -ceq "$zipName.sha256" }
    if ($sidecar.size -gt 1024) { throw 'Invalid remote checksum length.' }
    if ($zip.digest -and $zip.digest -cne "sha256:$ExpectedZipSha256") { throw 'Remote ZIP digest differs from the approved hash.' }
}

function Assert-PromotionSource {
    param([string]$Repository, [string]$Tag, [string]$SourceCommit)
    $commit = gh api "repos/$Repository/git/commits/$SourceCommit" --jq '.sha'
    if ($LASTEXITCODE -ne 0 -or $commit -cne $SourceCommit) { throw 'Cannot verify draft source commit.' }
    $refs = gh api "repos/$Repository/git/matching-refs/tags/$Tag"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot verify existing tag identity.' }
    $matches = @($refs | ConvertFrom-Json | Where-Object { $_.ref -ceq "refs/tags/$Tag" })
    if ($matches.Count -gt 1) { throw 'Ambiguous existing tag.' }
    if ($matches.Count -eq 0) { return } # Unpublished drafts can have no tag yet.
    $object = $matches[0].object
    for ($depth = 0; $object.type -eq 'tag' -and $depth -lt 4; $depth++) {
        if ($object.sha -cnotmatch '^[0-9a-f]{40}$') { throw 'Invalid annotated tag identity.' }
        $tagJson = gh api "repos/$Repository/git/tags/$($object.sha)"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot verify annotated tag.' }
        $object = ($tagJson | ConvertFrom-Json).object
    }
    if ($object.type -cne 'commit' -or $object.sha -cne $SourceCommit) { throw 'Existing tag points to another source. It will not be moved.' }
}

function Get-DraftFingerprint {
    param($Draft)
    return ([ordered]@{
        id = $Draft.id; tag = $Draft.tag_name; source = $Draft.target_commitish; draft = $Draft.draft
        prerelease = $Draft.prerelease; updated = $Draft.updated_at; title = $Draft.name; body = $Draft.body
        assets = @($Draft.assets | Sort-Object name | Select-Object id,name,size,state,digest,updated_at)
    } | ConvertTo-Json -Depth 6 -Compress)
}

function Complete-ExistingPrereleasePromotion {
    param($Draft, [string]$Tag, [string]$SourceCommit, [string]$Repository, [string]$ExpectedZipSha256,
        $Assets, [bool]$Publish, [string]$Approval, [string]$VerificationDirectory)
    Assert-PromotionRequest -Tag $Tag -SourceCommit $SourceCommit -Repository $Repository -ExpectedZipSha256 $ExpectedZipSha256 -Publish $Publish -Approval $Approval
    Assert-ExistingPrereleaseDraft -Draft $Draft -Tag $Tag -SourceCommit $SourceCommit -ExpectedZipSha256 $ExpectedZipSha256
    $verified = Assert-ReleaseAssets -PackageDirectory (Split-Path -Parent $Assets.Zip) -Version $Tag.Substring(1) -SourceCommit $SourceCommit
    if ($verified.Hash -cne $ExpectedZipSha256 -or $verified.ChecksumHash -cne $Assets.ChecksumHash) { throw 'Local verified assets changed.' }
    $latest = Get-ExistingPrereleaseDraft -Repository $Repository -Tag $Tag
    Assert-ExistingPrereleaseDraft -Draft $latest -Tag $Tag -SourceCommit $SourceCommit -ExpectedZipSha256 $ExpectedZipSha256
    if ((Get-DraftFingerprint $latest) -cne (Get-DraftFingerprint $Draft)) { throw 'Draft metadata/assets changed during verification; retry from the beginning.' }
    Assert-PromotionSource -Repository $Repository -Tag $Tag -SourceCommit $SourceCommit
    foreach ($asset in $latest.assets) {
        $path = Join-Path (Split-Path -Parent $Assets.Zip) $asset.name
        if ((Get-Item -LiteralPath $path).Length -ne $asset.size) { throw 'Remote/local asset length mismatch.' }
        if ($asset.digest -and $asset.digest -cne ('sha256:' + (Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant())) { throw 'Remote/local digest mismatch.' }
    }
    if ($Publish) {
        gh release edit $Tag --repo $Repository --draft=false --prerelease --latest=false
        if ($LASTEXITCODE -ne 0) { throw 'Promotion failed. Inspect remote state before retrying.' }
        $result = Get-ExistingPrereleaseDraft -Repository $Repository -Tag $Tag
        if ($result.id -ne $Draft.id -or $result.draft -ne $false -or $result.prerelease -ne $true -or
            $result.tag_name -cne $Tag -or $result.target_commitish -cne $SourceCommit) { throw 'Final publication state mismatch.' }
        if ((Get-DraftFingerprint ($result | Select-Object * -ExcludeProperty draft,updated_at)) -cne
            (Get-DraftFingerprint ($Draft | Select-Object * -ExcludeProperty draft,updated_at))) { throw 'Published asset identity changed.' }
        Assert-PromotionSource -Repository $Repository -Tag $Tag -SourceCommit $SourceCommit
    }
    $receipt = [ordered]@{ schemaVersion = 1; tag = $Tag; releaseId = $Draft.id; sourceCommit = $SourceCommit
        zipSha256 = $verified.Hash; checksumSha256 = $verified.ChecksumHash; published = $Publish; verifiedUtc = [DateTime]::UtcNow.ToString('O') }
    [IO.File]::WriteAllText((Join-Path $VerificationDirectory 'promotion-verification.json'), ($receipt | ConvertTo-Json))
    return ($receipt | ConvertTo-Json)
}
