$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$output = Join-Path $root "tests\bin"
$test = Join-Path $root "tests\HostCommandBoundaryTests.cs"
$command = Join-Path $root "src\desktop\HostCommand.cs"
$native = Join-Path $root "src\desktop\SystemGuardianNative.cs"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

New-Item -ItemType Directory -Path $output -Force | Out-Null

& $compiler /nologo /target:exe /platform:x64 `
    /out:"$(Join-Path $output 'HostCommandBoundaryTests.exe')" `
    /reference:System.Management.dll `
    /reference:System.ServiceProcess.dll `
    /reference:System.Web.Extensions.dll `
    $test $command $native

& (Join-Path $output "HostCommandBoundaryTests.exe")
if ($LASTEXITCODE -ne 0) {
    throw "Host command boundary tests failed with exit code $LASTEXITCODE"
}
