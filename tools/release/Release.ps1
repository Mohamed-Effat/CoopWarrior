<#
.SYNOPSIS
Builds a Co-op Warrior release with Velopack: CoopWarrior-win-Setup.exe plus update packages.

.EXAMPLE
.\tools\release\Release.ps1 -Version 2.0.1
Builds into releases\ only. Install releases\CoopWarrior-win-Setup.exe to try it.

.EXAMPLE
.\tools\release\Release.ps1 -Version 2.0.1 -Publish
Also uploads the release to GitHub, where installed copies find it. Uses $env:GITHUB_TOKEN when it is set
(the Release workflow sets it), otherwise the gh CLI login. Add -Draft to leave the release unpublished.
Release notes come from docs\release-notes\<version>.md when that file exists, or from -Notes.
#>
param(
    [Parameter(Mandatory)][string]$Version,
    [switch]$Publish,
    [switch]$Draft,
    [string]$Notes
)
$ErrorActionPreference = 'Stop'

$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$repoUrl = 'https://github.com/Mohamed-Effat/CoopWarrior'
$publishDir = Join-Path $repo 'artifacts\publish'
$releaseDir = Join-Path $repo 'releases'

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 2.0.1, not '$Version'." }
if (-not $Notes) {
    $defaultNotes = Join-Path $repo "docs\release-notes\$Version.md"
    if (Test-Path $defaultNotes) { $Notes = $defaultNotes }
}
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    throw 'vpk is not installed. Run: dotnet tool install -g vpk --version 1.2.161'
}

Write-Host "Publishing Co-op Warrior $Version..."
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish (Join-Path $repo 'src') -c Release "-p:Version=$Version" -o $publishDir -nologo
if ($LASTEXITCODE) { throw 'dotnet publish failed.' }

$token = $null
if ($Publish) {
    $token = $env:GITHUB_TOKEN
    if (-not $token) { $token = (gh auth token).Trim() }
    if (-not $token) { throw 'Not logged in to GitHub. Run: gh auth login' }
    # The previous release lets vpk build a delta package, so players download only what changed.
    Write-Host 'Downloading the latest published release for delta packages...'
    vpk download github --repoUrl $repoUrl --token $token --outputDir $releaseDir
    if ($LASTEXITCODE) { throw 'vpk download failed.' }
}

Write-Host 'Packing...'
$pack = @(
    'pack',
    '--packId', 'CoopWarrior',
    '--packVersion', $Version,
    '--packDir', $publishDir,
    '--mainExe', 'CoopWarrior.exe',
    '--packTitle', 'Co-op Warrior',
    '--packAuthors', 'Mohamed Effat',
    '--icon', (Join-Path $repo 'src\Assets\CoopWarrior.ico'),
    '--splashImage', (Join-Path $repo 'src\Assets\CoopWarrior.png'),
    '--outputDir', $releaseDir
)
if ($Notes) { $pack += @('--releaseNotes', (Resolve-Path $Notes).Path) }
vpk @pack
if ($LASTEXITCODE) { throw 'vpk pack failed.' }

if ($Publish) {
    Write-Host 'Uploading to GitHub...'
    $upload = @(
        'upload', 'github',
        '--repoUrl', $repoUrl,
        '--token', $token,
        '--outputDir', $releaseDir,
        '--releaseName', "Co-op Warrior $Version",
        '--tag', "v$Version",
        '--targetCommitish', (git -C $repo rev-parse HEAD)
    )
    if (-not $Draft) { $upload += '--publish' }
    vpk @upload
    if ($LASTEXITCODE) { throw 'vpk upload failed.' }
    if ($Draft) { Write-Host "Draft created; publish it at $repoUrl/releases" }
    else { Write-Host "Published: $repoUrl/releases/tag/v$Version" }
}
else {
    Write-Host "Built locally in $releaseDir. Nothing was uploaded."
}
