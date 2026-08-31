# Scanner Evaluation

## Scope And Method

Evaluation ran on August 31, 2026 on Windows 11 Home build 26200, a 13th Gen Intel Core i9-13950HX (32 logical processors), and 15.7 GB RAM. Detection tests inject state through `IGuardianSystemProbe`; they do not mutate the machine. The production smoke test separately proves that the real Windows probe returns all seven checks.

Each broken fixture changes one input from a known-clean baseline. A detection passes only when the intended check returns the expected status and every unrelated check remains `good`. Six clean fixtures exercise normal values, exact healthy thresholds, Windows 10 and Windows 11 build labels, a legitimate 12-entry startup load, a large disk at 20% free, and a recently updated system with one browse-only optional update.

## Detection Matrix

| Condition | Expected status | Detected | Unrelated false positives |
| --- | --- | --- | ---: |
| Clean baseline | all good | yes | 0 |
| Clean threshold boundaries | all good | yes | 0 |
| Healthy Windows 10 22H2 laptop | all good | yes | 0 |
| Healthy Windows 11 24H2 workstation | all good | yes | 0 |
| Large disk with comfortable capacity | all good | yes | 0 |
| Recently updated with one optional update | all good | yes | 0 |
| Pending Windows updates | attention | yes | 0 |
| Windows Update API unavailable | review | yes | 0 |
| Defender antivirus off | attention | yes | 0 |
| Defender real-time protection off | attention | yes | 0 |
| Defender signatures four days old | review | yes | 0 |
| Disk 96% full | attention | yes | 0 |
| Disk 90% full | review | yes | 0 |
| Fifteen startup entries | review | yes | 0 |
| Device Manager error | review | yes | 0 |
| Pending restart flag | review | yes | 0 |
| Windows Event Log stopped | attention | yes | 0 |
| Windows Management Instrumentation stopped | attention | yes | 0 |

Result: **12/12 broken states detected** and **0 false positives across 18 fixture decisions (12 broken, 6 clean)**. The six clean fixtures yielded zero findings across 42 individual check results.

## Scan Latency

The full production scanner was launched in a fresh test process for 10 consecutive runs before and after parallelizing the seven independent I/O-bound checks. Times include probe and process startup. Percentiles use nearest-rank selection; with 10 samples, p95 is the maximum.

| Implementation | Runs | p50 | p95 | Min | Max |
| --- | ---: | ---: | ---: | ---: | ---: |
| Serial baseline | 10 | 4,757.6 ms | 10,066.2 ms | 4,527.7 ms | 10,066.2 ms |
| Parallel checks | 10 | 5,074.7 ms | 45,796.2 ms | 4,760.2 ms | 45,796.2 ms |

The parallel implementation did **not** demonstrate a full-scan latency improvement in this sample: p50 increased 6.7%, and p95 was dominated by one 45.8-second Windows Update API call. Parallelism removes additive waiting between independent checks, but total latency remains bounded by the slowest probe. These numbers are evidence from one machine, not a general performance claim.

Reproduce the current measurement with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\benchmark-scan.ps1 -Runs 10
```

## What The Tests Prove

- Decision thresholds and status mapping for the seven advertised checks.
- Isolation: one injected fault does not create unrelated findings.
- Clean behavior across six healthy variants, including threshold values, Windows build labels, a large disk, a 12-entry startup load, and a browse-only optional update.
- The real Windows probe can complete and return seven checks on the CI or developer machine.
- The WebView2 command parser rejects malformed, oversized, unknown, and over-parameterized messages in a suite with 81 hostile assertions across 10 attack classes, plus 21 valid-command assertions.

## What They Do Not Prove

- Fixtures do not prove every Windows API, WMI provider, OEM driver, or locale behaves identically.
- The scanner does not separately detect Windows Update pause policy; it reports pending updates or an unavailable Update API.
- Device checking reports active Device Manager error codes but does not diagnose their root cause.
- Core-service checking covers Windows Event Log and Windows Management Instrumentation only.
- The health score is not a malware verdict, reliability probability, benchmark, or guarantee that Windows is healthy.
