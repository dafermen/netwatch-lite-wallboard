param([string]$Repository = 'dafermen/netwatch-lite-wallboard')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'release-package.ps1')
$tags = @(Get-GitHubReleaseTags -Repository $Repository)
if (@($tags | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count) { throw 'Unexpected empty release tag.' }
Write-Host "Read-only GitHub CLI query passed in PowerShell $($PSVersionTable.PSVersion): $($tags.Count) release tags. No release was created or changed."
