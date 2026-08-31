$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$app = Join-Path $root "app"
$uiSource = Join-Path $root "src\ui\SystemGuardianUI.html"
$src = Join-Path $root "src\desktop\SystemGuardianWebHost.cs"
$command = Join-Path $root "src\desktop\HostCommand.cs"
$probe = Join-Path $root "src\desktop\GuardianSystemProbe.cs"
$native = Join-Path $root "src\desktop\SystemGuardianNative.cs"
$manifest = Join-Path $root "src\desktop\SystemGuardian.manifest"

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
    throw "C# compiler not found at $csc"
}

if (-not (Test-Path -LiteralPath $uiSource)) {
    throw "Missing UI source: $uiSource"
}

Copy-Item -LiteralPath $uiSource -Destination (Join-Path $app "SystemGuardianUI.html") -Force

foreach ($file in @(
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Web.WebView2.WinForms.dll",
    "WebView2Loader.dll",
    "SystemGuardianUI.html"
)) {
    $path = Join-Path $app $file
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing runtime file: $path"
    }
}

& $csc /nologo /target:winexe /platform:x64 `
    /out:"$(Join-Path $app 'SystemGuardian.exe')" `
    /win32manifest:$manifest `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Management.dll `
    /reference:System.ServiceProcess.dll `
    /reference:System.Web.Extensions.dll `
    /reference:"$(Join-Path $app 'Microsoft.Web.WebView2.Core.dll')" `
    /reference:"$(Join-Path $app 'Microsoft.Web.WebView2.WinForms.dll')" `
    $src $command $probe $native

Write-Host "Built app\SystemGuardian.exe and refreshed the runtime UI"
