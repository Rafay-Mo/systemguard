$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$app = Join-Path $root "app"
$version = "1.0.2365.46"
$expectedSha256 = "1B0146D842344B8462520FB38AD8EFF46E52031C19B955376C227BEC619FAEFD"
$packageName = "microsoft.web.webview2.$version.nupkg"
$packageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$version/$packageName"
$cache = Join-Path $env:LOCALAPPDATA "SystemGuardian\BuildCache\$packageName"
$extract = Join-Path $env:TEMP "SystemGuardian-WebView2-$version"

$required = @(
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll"
)

if (($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $app $_)) }).Count -eq 0) {
    return
}

New-Item -ItemType Directory -Path (Split-Path -Parent $cache) -Force | Out-Null
if (-not (Test-Path -LiteralPath $cache)) {
    Invoke-WebRequest -UseBasicParsing -Uri $packageUrl -OutFile $cache
}

$actualSha256 = (Get-FileHash -LiteralPath $cache -Algorithm SHA256).Hash
if ($actualSha256 -ne $expectedSha256) {
    Remove-Item -LiteralPath $cache -Force
    throw "WebView2 package checksum mismatch. Expected $expectedSha256, received $actualSha256."
}

if (Test-Path -LiteralPath $extract) {
    Remove-Item -LiteralPath $extract -Recurse -Force
}

New-Item -ItemType Directory -Path $extract -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($cache, $extract)

Copy-Item -LiteralPath (Join-Path $extract "lib\net45\Microsoft.Web.WebView2.Core.dll") -Destination $app -Force
Copy-Item -LiteralPath (Join-Path $extract "lib\net45\Microsoft.Web.WebView2.WinForms.dll") -Destination $app -Force
Copy-Item -LiteralPath (Join-Path $extract "build\native\x64\WebView2Loader.dll") -Destination $app -Force
Remove-Item -LiteralPath $extract -Recurse -Force

Write-Host "Restored Microsoft.Web.WebView2 $version from the checksum-pinned NuGet package."