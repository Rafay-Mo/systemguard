$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$output = Join-Path $root "tests\bin"
$test = Join-Path $root "tests\NativeScannerSmoke.cs"
$native = Join-Path $root "src\desktop\SystemGuardianNative.cs"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

New-Item -ItemType Directory -Path $output -Force | Out-Null

& $compiler /nologo /target:exe /platform:x64 `
    /out:"$(Join-Path $output 'NativeScannerSmoke.exe')" `
    /reference:System.Management.dll `
    /reference:System.ServiceProcess.dll `
    $test $native

& (Join-Path $output "NativeScannerSmoke.exe")
if ($LASTEXITCODE -ne 0) {
    throw "Native scanner smoke test failed with exit code $LASTEXITCODE"
}
