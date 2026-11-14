param(
    [Parameter(Mandatory = $true)][string]$Tag,
    [Parameter(Mandatory = $true)][string]$SourceCommit,
    [Parameter(Mandatory = $true)][string]$ExpectedZipSha256,
    [string]$Repository = 'dafermen/netwatch-lite-wallboard',
    [switch]$Publish,
    [string]$Approval = ''
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-package.ps1')
Assert-PromotionRequest -Tag $Tag -SourceCommit $SourceCommit -ExpectedZipSha256 $ExpectedZipSha256 `
    -Repository $Repository -Publish $Publish.IsPresent -Approval $Approval
$root = Join-Path (Split-Path -Parent $PSScriptRoot) ('artifacts\verification\promotion-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$draft = Get-ExistingPrereleaseDraft -Repository $Repository -Tag $Tag
Assert-ExistingPrereleaseDraft -Draft $draft -Tag $Tag -SourceCommit $SourceCommit -ExpectedZipSha256 $ExpectedZipSha256
Assert-PromotionSource -Repository $Repository -Tag $Tag -SourceCommit $SourceCommit
$download = Join-Path $root 'download'
New-Item -ItemType Directory -Path $download | Out-Null
$name = "NetWatch-Lite-Wallboard-$Tag-win-x64.zip"
gh release download $Tag --repo $Repository --dir $download --pattern $name --pattern "$name.sha256"
if ($LASTEXITCODE -ne 0) { throw 'Draft download failed. Nothing was published.' }
$assets = Assert-ReleaseAssets -PackageDirectory $download -Version $Tag.Substring(1) -SourceCommit $SourceCommit
if ($assets.Hash -cne $ExpectedZipSha256) { throw 'Downloaded ZIP does not match the previously approved hash.' }
& (Join-Path $PSScriptRoot 'test-portable.ps1') -ZipPath $assets.Zip
Complete-ExistingPrereleasePromotion -Draft $draft -Tag $Tag -SourceCommit $SourceCommit -Repository $Repository `
    -ExpectedZipSha256 $ExpectedZipSha256 -Assets $assets -Publish $Publish.IsPresent -Approval $Approval `
    -VerificationDirectory $root
