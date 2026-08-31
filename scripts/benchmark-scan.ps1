param(
    [ValidateRange(3, 100)]
    [int]$Runs = 10
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe = Join-Path $root "tests\bin\NativeScannerSmoke.exe"

if (-not (Test-Path -LiteralPath $exe)) {
    & (Join-Path $root "scripts\test-native.ps1")
}

$times = @()
1..$Runs | ForEach-Object {
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    & $exe *> $null
    if ($LASTEXITCODE -ne 0) { throw "Scan benchmark failed on run $_." }
    $stopwatch.Stop()
    $milliseconds = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
    $times += $milliseconds
    Write-Host ("Run {0}: {1} ms" -f $_, $milliseconds)
}

$sorted = $times | Sort-Object
$p50 = $sorted[[math]::Ceiling($sorted.Count * 0.50) - 1]
$p95 = $sorted[[math]::Ceiling($sorted.Count * 0.95) - 1]
$result = [pscustomobject]@{
    Runs = $times.Count
    P50Ms = $p50
    P95Ms = $p95
    MinMs = $sorted[0]
    MaxMs = $sorted[-1]
    AllMs = $times
}

$result | Format-List
$result | ConvertTo-Json -Compress
