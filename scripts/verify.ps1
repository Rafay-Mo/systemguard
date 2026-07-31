$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

& (Join-Path $root "scripts\build-desktop.ps1")
if ($LASTEXITCODE -ne 0) { throw "Desktop build failed." }

& (Join-Path $root "scripts\test-native.ps1")
if ($LASTEXITCODE -ne 0) { throw "Native scanner verification failed." }

& node (Join-Path $root "scripts\check-ui.js")
if ($LASTEXITCODE -ne 0) { throw "UI verification failed." }

& node (Join-Path $root "scripts\check-knowledge-base.js")
if ($LASTEXITCODE -ne 0) { throw "Knowledge-base verification failed." }

& node --check (Join-Path $root "backend\server.js")
if ($LASTEXITCODE -ne 0) { throw "Backend syntax verification failed." }

$requiredRuntimeFiles = @(
    "SystemGuardian.exe",
    "SystemGuardianUI.html",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll"
)

foreach ($file in $requiredRuntimeFiles) {
    $path = Join-Path $root ("app\" + $file)
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing runtime file: $path"
    }
}

Write-Host "Verification passed: build, native scanner, UI, knowledge base, backend syntax, and runtime files."
