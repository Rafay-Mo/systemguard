$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$release = Join-Path $root "Release"
$version = (Get-Content -LiteralPath (Join-Path $root "VERSION") -Raw).Trim()
$stageRoot = Join-Path $release "tmp"
$stage = Join-Path $stageRoot "SystemGuardian"
$package = Join-Path $release ("SystemGuardian-v" + $version + ".zip")

if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "VERSION must use semantic versioning, for example 1.1.0."
}

& (Join-Path $root "scripts\verify.ps1")

New-Item -ItemType Directory -Path $release -Force | Out-Null
$resolvedRelease = (Resolve-Path $release).Path
if (-not $stageRoot.StartsWith($resolvedRelease, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe release staging path."
}

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path (Join-Path $stage "app") -Force | Out-Null
foreach ($file in @(
    "SystemGuardian.exe",
    "SystemGuardianUI.html",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll"
)) {
    Copy-Item -LiteralPath (Join-Path $root ("app\" + $file)) -Destination (Join-Path $stage "app")
}

Copy-Item -LiteralPath (Join-Path $root "Start-SystemGuardian.cmd") -Destination $stage
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination $stage

Get-ChildItem -LiteralPath $release -Filter "SystemGuardian-v*.zip" -File | ForEach-Object {
    if (-not $_.FullName.StartsWith($resolvedRelease, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe release cleanup path."
    }
    Remove-Item -LiteralPath $_.FullName -Force
}

Compress-Archive -Path $stage -DestinationPath $package -CompressionLevel Optimal
Remove-Item -LiteralPath $stageRoot -Recurse -Force

Write-Host ("Release created: " + $package)
