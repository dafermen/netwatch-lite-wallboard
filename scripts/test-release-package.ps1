param([string]$Executable = '')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-package.ps1')
if (-not $Executable) {
    $Executable = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\NetWatchLite.Wallboard.WebView2\bin\Release\net10.0-windows10.0.17763.0\NetWatch-Lite-Wallboard.exe'
}
$product = (Get-Item -LiteralPath $Executable).VersionInfo.ProductVersion -split '\+'
if ($product.Count -ne 2) { throw 'Build a Release executable with source commit metadata first.' }
$version = $product[0]
$commit = $product[1]
Assert-PrereleaseIdentity -Tag ('v' + $version) -Version $version -SourceCommit $commit
$root = Join-Path (Split-Path -Parent $PSScriptRoot) ('artifacts\verification\release-tests-' + [guid]::NewGuid().ToString('N'))
$fixture = Join-Path $root 'fixture'
New-Item -ItemType Directory -Path (Join-Path $fixture 'app') | Out-Null
Copy-Item -LiteralPath $Executable -Destination (Join-Path $fixture 'app\NetWatch-Lite-Wallboard.exe')
$zip = Join-Path $root "NetWatch-Lite-Wallboard-v$version-win-x64.zip"
Compress-Archive -LiteralPath (Join-Path $fixture 'app') -DestinationPath $zip
$hash = (Get-FileHash -LiteralPath $zip).Hash.ToLowerInvariant()
$sidecar = "$hash  $([IO.Path]::GetFileName($zip))"
[IO.File]::WriteAllText("$zip.sha256", $sidecar)
$checks = 0
function Assert-Rejected([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw 'Release validation accepted an invalid fixture.' }
    $script:checks++
}
$assets = Assert-ReleaseAssets -PackageDirectory $root -Version $version -SourceCommit $commit
$checks++
foreach ($bad in @('v1.0.0', 'v0.4.0-beta.1', 'v0.4.0-rc.1;whoami', '../v0.4.0-rc.1', ('v' + $version + '9'))) {
    Assert-Rejected { Assert-PrereleaseIdentity -Tag $bad -Version $version -SourceCommit $commit }
}
Assert-Rejected { Assert-PrereleaseIdentity -Tag ('v' + $version) -Version $version -SourceCommit 'short' }
$wrongCommit = if ($commit -eq ('0' * 40)) { '1' * 40 } else { '0' * 40 }
Assert-Rejected { Assert-ReleaseAssets -PackageDirectory $root -Version $version -SourceCommit $wrongCommit }
[IO.File]::WriteAllText("$zip.sha256", ('0' * 64) + '  wrong.zip')
Assert-Rejected { Assert-ReleaseAssets -PackageDirectory $root -Version $version -SourceCommit $commit }
[IO.File]::WriteAllText("$zip.sha256", $sidecar)
[IO.File]::AppendAllText($zip, 'corruption')
Assert-Rejected { Assert-ReleaseAssets -PackageDirectory $root -Version $version -SourceCommit $commit }
# Restore from the fixture rather than trusting altered archive bytes for the publication gate tests.
$freshZip = Join-Path $root 'fresh.zip'
Compress-Archive -LiteralPath (Join-Path $fixture 'app') -DestinationPath $freshZip
Copy-Item -LiteralPath $freshZip -Destination $zip -Force
$hash = (Get-FileHash -LiteralPath $zip).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zip.sha256", "$hash  $([IO.Path]::GetFileName($zip))")
$assets = Assert-ReleaseAssets -PackageDirectory $root -Version $version -SourceCommit $commit
$script:FakePublished = $false
$script:TamperDownload = $false
$script:WrongMetadata = $false
$script:DownloadFailure = $false
function gh {
    $global:LASTEXITCODE = 0
    if ($args[0] -ne 'release') { throw 'Unexpected mock GitHub command.' }
    switch ($args[1]) {
        'download' {
            if ($script:DownloadFailure) { $global:LASTEXITCODE = 1; return }
            $destination = $args[[Array]::IndexOf($args, '--dir') + 1]
            Copy-Item -LiteralPath $assets.Zip, $assets.Checksum -Destination $destination
            if ($script:TamperDownload) { [IO.File]::AppendAllText((Join-Path $destination ([IO.Path]::GetFileName($assets.Checksum))), 'tamper') }
        }
        'edit' {
            if ($args -notcontains '--prerelease' -or $args -notcontains '--latest=false' -or $args -notcontains '--draft=false') { throw 'Unsafe publication flags.' }
            $script:FakePublished = $true
        }
        'view' {
            $target = if ($script:WrongMetadata) { '0' * 40 } else { $commit }
            return ([pscustomobject]@{ tagName=('v' + $version); isDraft=(-not $script:FakePublished); isPrerelease=$true; targetCommitish=$target; url='https://example.invalid/test-only' } | ConvertTo-Json)
        }
        default { throw 'Unexpected mock release action.' }
    }
}
Complete-PrereleaseDraft -Tag ('v' + $version) -Repository 'fixture/repo' -SourceCommit $commit -Assets $assets -VerificationDirectory (Join-Path $root 'draft') -Publish $false | Out-Null
if ($script:FakePublished) { throw 'Draft mode published unexpectedly.' }
$checks++
$script:WrongMetadata = $true
Assert-Rejected { Complete-PrereleaseDraft -Tag ('v' + $version) -Repository 'fixture/repo' -SourceCommit $commit -Assets $assets -VerificationDirectory (Join-Path $root 'wrong-draft') -Publish $true }
if ($script:FakePublished) { throw 'Wrong draft identity was published.' }
$script:WrongMetadata = $false
$script:DownloadFailure = $true
Assert-Rejected { Complete-PrereleaseDraft -Tag ('v' + $version) -Repository 'fixture/repo' -SourceCommit $commit -Assets $assets -VerificationDirectory (Join-Path $root 'failed-download') -Publish $true }
if ($script:FakePublished) { throw 'Failed download was published.' }
$script:DownloadFailure = $false
$script:TamperDownload = $true
Assert-Rejected { Complete-PrereleaseDraft -Tag ('v' + $version) -Repository 'fixture/repo' -SourceCommit $commit -Assets $assets -VerificationDirectory (Join-Path $root 'tampered-download') -Publish $true }
if ($script:FakePublished) { throw 'Corrupt download was published.' }
$script:TamperDownload = $false
Complete-PrereleaseDraft -Tag ('v' + $version) -Repository 'fixture/repo' -SourceCommit $commit -Assets $assets -VerificationDirectory (Join-Path $root 'published') -Publish $true | Out-Null
if (-not $script:FakePublished) { throw 'Explicit publish did not run after verification.' }
$checks++
Write-Host "Release validation checks passed: $checks. Fixtures: $root"
