param([string]$Executable = '')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-package.ps1')
if (-not $Executable) { $Executable = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\NetWatchLite.Wallboard.WebView2\bin\Release\net10.0-windows10.0.17763.0\NetWatch-Lite-Wallboard.exe' }
$identity = (Get-Item -LiteralPath $Executable).VersionInfo.ProductVersion -split '\+'
$version = $identity[0]; $commit = $identity[1]; $tag = 'v' + $version
$root = Join-Path (Split-Path -Parent $PSScriptRoot) ('artifacts\verification\promotion-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $root 'fixture\app') | Out-Null
Copy-Item -LiteralPath $Executable -Destination (Join-Path $root 'fixture\app\NetWatch-Lite-Wallboard.exe')
$zip = Join-Path $root "NetWatch-Lite-Wallboard-$tag-win-x64.zip"
Compress-Archive -LiteralPath (Join-Path $root 'fixture\app') -DestinationPath $zip
$hash = (Get-FileHash -LiteralPath $zip).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zip.sha256", "$hash  $([IO.Path]::GetFileName($zip))")
$assets = Assert-ReleaseAssets -PackageDirectory $root -Version $version -SourceCommit $commit
$draft = [pscustomobject]@{
    id = 123; draft = $true; prerelease = $true; tag_name = $tag; target_commitish = $commit
    name = 'Fixture'; body = 'Not a live release'; updated_at = '2026-11-12T00:00:00Z'
    assets = @(
        [pscustomobject]@{ id = 1; name = [IO.Path]::GetFileName($zip); state = 'uploaded'; size = (Get-Item $zip).Length; digest = "sha256:$hash"; updated_at = '1' },
        [pscustomobject]@{ id = 2; name = [IO.Path]::GetFileName("$zip.sha256"); state = 'uploaded'; size = (Get-Item "$zip.sha256").Length; digest = "sha256:$($assets.ChecksumHash)"; updated_at = '1' }
    )
}
$script:checks = 0
$script:edited = $false
$script:changed = $false
$script:tagMismatch = $false
$script:readFailure = $false
function Clone-Fixture { return ($draft | ConvertTo-Json -Depth 8 | ConvertFrom-Json) }
function Assert-Rejected([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw "Unsafe promotion input accepted after $script:checks checks: $Action" }
    if ($script:edited) { throw 'Rejected promotion edited GitHub.' }
    $script:checks++
}
function gh {
    $global:LASTEXITCODE = 0
    if ($script:readFailure) { $global:LASTEXITCODE = 1; return }
    if ($args[0] -eq 'release' -and $args[1] -eq 'view') { return '{"databaseId":123}' }
    if ($args[0] -eq 'api') {
        if ($args[1] -like '*/git/commits/*') { return $commit }
        if ($args[1] -like '*/git/matching-refs/*') {
            if ($script:tagMismatch) { return ('[{"ref":"refs/tags/' + $tag + '","object":{"type":"commit","sha":"' + ('0' * 40) + '"}}]') }
            return '[]'
        }
        if ($args[1] -ne 'repos/fixture/repo/releases/123') { throw 'Unexpected API route.' }
        $result = Clone-Fixture
        if ($script:changed) { $result.assets[0].id = 999 }
        if ($script:edited) { $result.draft = $false; $result.updated_at = '2026-11-12T01:00:00Z' }
        return ($result | ConvertTo-Json -Depth 8)
    }
    if ($args[0] -eq 'release' -and $args[1] -eq 'edit') {
        if ($args -notcontains '--draft=false' -or $args -notcontains '--prerelease' -or $args -notcontains '--latest=false') { throw 'Unsafe promotion flags.' }
        $script:edited = $true; return
    }
    throw 'Mock refuses creation, uploads, deletion, builds and unexpected commands.'
}
function Complete-Fixture([bool]$Publish = $false, [string]$Approval = '') {
    Complete-ExistingPrereleasePromotion -Draft $draft -Tag $tag -SourceCommit $commit -Repository 'fixture/repo' `
        -ExpectedZipSha256 $hash -Assets $assets -Publish $Publish -Approval $Approval -VerificationDirectory $root
}
Assert-PromotionRequest -Tag $tag -SourceCommit $commit -ExpectedZipSha256 $hash -Repository 'fixture/repo' -Publish $false
$script:checks++
foreach ($bad in @('v1.0.0', 'v0.4.0-beta.1', '../rc', 'v0.4.0-rc.1;whoami')) {
    Assert-Rejected { Assert-PromotionRequest -Tag $bad -SourceCommit $commit -ExpectedZipSha256 $hash -Repository 'fixture/repo' }
}
Assert-Rejected { Complete-Fixture -Publish $true }
Assert-Rejected { Complete-Fixture -Publish $true -Approval 'PUBLISH another-tag' }
Assert-Rejected { Assert-PromotionRequest -Tag $tag -SourceCommit 'short' -ExpectedZipSha256 $hash -Repository 'fixture/repo' }
Assert-Rejected { Assert-PromotionRequest -Tag $tag -SourceCommit $commit -ExpectedZipSha256 'bad' -Repository 'fixture/repo' }
Assert-Rejected { Assert-PromotionRequest -Tag $tag -SourceCommit $commit -ExpectedZipSha256 $hash -Repository '../escape' }
foreach ($case in @('published','stable','source','tag','extra','digest','size','state','duplicate')) {
    $bad = Clone-Fixture
    switch ($case) {
        published { $bad.draft = $false }; stable { $bad.prerelease = $false }
        source { $bad.target_commitish = '0' * 40 }; tag { $bad.tag_name = 'v1.0.0-rc.1' }
        extra { $bad.assets += $bad.assets[0] }; digest { $bad.assets[0].digest = 'sha256:' + ('0' * 64) }
        size { $bad.assets[0].size = 0 }; state { $bad.assets[0].state = 'new' }
        duplicate { $bad.assets[1].name = $bad.assets[0].name }
    }
    Assert-Rejected { Assert-ExistingPrereleaseDraft -Draft $bad -Tag $tag -SourceCommit $commit -ExpectedZipSha256 $hash }
}
Complete-Fixture | Out-Null
if ($script:edited) { throw 'Verify-only published a draft.' }; $script:checks++
$script:changed = $true
Assert-Rejected { Complete-Fixture -Publish $true -Approval "PUBLISH $tag" }
$script:changed = $false; $script:tagMismatch = $true
Assert-Rejected { Complete-Fixture -Publish $true -Approval "PUBLISH $tag" }
$script:tagMismatch = $false; $script:readFailure = $true
Assert-Rejected { Complete-Fixture -Publish $true -Approval "PUBLISH $tag" }
$script:readFailure = $false
[IO.File]::AppendAllText("$zip.sha256", 'tamper')
Assert-Rejected { Complete-Fixture -Publish $true -Approval "PUBLISH $tag" }
[IO.File]::WriteAllText("$zip.sha256", "$hash  $([IO.Path]::GetFileName($zip))")
Complete-Fixture -Publish $true -Approval "PUBLISH $tag" | Out-Null
if (-not $script:edited) { throw 'Approved verified promotion did not run.' }; $script:checks++
Write-Host "Existing-draft promotion checks passed: $script:checks. Fixtures: $root"
