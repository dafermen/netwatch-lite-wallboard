param(
    [Parameter(Mandatory = $true)][string]$Tag,
    [Parameter(Mandatory = $true)][string]$SourceCommit,
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [string]$Repository = 'dafermen/netwatch-lite-wallboard',
    [ValidateSet('Draft', 'Publish')][string]$Mode = 'Draft'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-package.ps1')
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'Invalid GitHub repository.' }
$version = $Tag.Substring(1)
Assert-PrereleaseIdentity -Tag $Tag -Version $version -SourceCommit $SourceCommit
$assets = Assert-ReleaseAssets -PackageDirectory $PackageDirectory -Version $version -SourceCommit $SourceCommit
& (Join-Path $PSScriptRoot 'test-portable.ps1') -ZipPath $assets.Zip

# All remote reads must succeed. Never treat an authentication/network error as an absent release.
$existing = gh api "repos/$Repository/releases" --paginate --jq ".[] | select(.tag_name == `"$Tag`") | .id"
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect existing GitHub releases.' }
if ($existing) { throw 'Release already exists; it will not be replaced.' }
$refs = gh api "repos/$Repository/git/matching-refs/tags/$Tag"
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect existing tags.' }
if (@($refs | ConvertFrom-Json | Where-Object { $_.ref -ceq "refs/tags/$Tag" }).Count) { throw 'Tag already exists; it will not be moved.' }
$commit = gh api "repos/$Repository/git/commits/$SourceCommit" --jq '.sha'
if ($LASTEXITCODE -ne 0 -or $commit -cne $SourceCommit) { throw 'Source commit is not available in the destination repository.' }

$notes = @"
Evaluation prerelease only, not stable production approval. Long production/24-hour acceptance and broader fuzzing/mutation evidence remain pending.

Windows x64 portable with .NET 10 included. WebView2 Runtime and Windows .NET Framework 4.x remain external requirements.
Extract the complete ZIP into a NEW ordinary local folder and open the root EXE. Preserve your operator JSON/local pages; never mix app/ files or overwrite a working deployment.

Source commit and binary ProductVersion: $($assets.ProductVersion)
ZIP SHA-256: $($assets.Hash)

The exact ZIP passed package/startup-integrity tests. Both assets are downloaded and hash-checked before any public publication. Checksums detect changed bytes, not publisher signing.
See https://github.com/$Repository/blob/$SourceCommit/docs/DEPLOYMENT.md and CURRENT_STATUS.md for evidence and remaining limits.
"@
gh release create $Tag $assets.Zip $assets.Checksum --repo $Repository --target $SourceCommit `
    --title "NetWatch Lite Wallboard $Tag (Pre-release)" --notes $notes --draft --prerelease --latest=false
if ($LASTEXITCODE -ne 0) { throw 'Could not create/upload the draft; inspect GitHub before retrying. No existing release was overwritten.' }
$root = Join-Path (Split-Path -Parent $PSScriptRoot) ('artifacts\verification\release-' + [guid]::NewGuid().ToString('N'))
Complete-PrereleaseDraft -Tag $Tag -Repository $Repository -SourceCommit $SourceCommit -Assets $assets `
    -VerificationDirectory $root -Publish ($Mode -eq 'Publish')
