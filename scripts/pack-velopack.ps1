# Build a Velopack release for Huitres and optionally upload to GitHub Releases.
# Requires: dotnet tool install -g vpk  (same major version as Velopack NuGet package)
#
# To publish to GitHub (public repo):
#   $env:GITHUB_TOKEN = "ghp_..."   # optional for public repos; required for private
#   .\scripts\pack-velopack.ps1 -Upload

param(
    [switch]$Upload
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$version = "1.0.7"
$repoUrl = "https://github.com/yassin-ajanif/zwitreDakhla"
$publishDir = Join-Path $root "publish"
$releasesDir = Join-Path $root "releases"

Push-Location $root
try {
    dotnet publish GestionCommerciale.csproj -c Release -r win-x64 --self-contained -o $publishDir
    if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
        Write-Error "Install vpk: dotnet tool install -g vpk"
    }
    vpk pack --packId Huitres --packVersion $version --packDir $publishDir --mainExe Huitres.exe --outputDir $releasesDir
    Write-Host "Release ready in $releasesDir"

    if ($Upload) {
        $uploadArgs = @(
            "upload", "github",
            "--repoUrl", $repoUrl,
            "--outputDir", $releasesDir,
            "--publish",
            "--tag", "v$version",
            "--releaseName", "Huitres v$version"
        )
        if ($env:GITHUB_TOKEN) {
            $uploadArgs += @("--token", $env:GITHUB_TOKEN)
        }
        vpk @uploadArgs
        Write-Host "Uploaded to $repoUrl/releases/tag/v$version"
    }
    else {
        Write-Host "Upload skipped. Run with -Upload or: vpk upload github --repoUrl $repoUrl --outputDir $releasesDir --publish --tag v$version"
    }
}
finally {
    Pop-Location
}
