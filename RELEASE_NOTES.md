# System Guardian 1.4.0

This release hardens the privileged WebView2-to-native boundary and replaces descriptive scanner claims with repeatable evidence.

## Security hardening

- Pins the WebView2 host to the canonical packaged document, rejects messages from any other source, blocks alternate navigation and new windows, denies WebView permissions, and disables host-object exposure.
- Introduces an exact, typed, deny-by-default native command table. Unknown commands, extra fields, oversized payloads, type-confused values, path-like repair IDs, and repair IDs outside the fixed allowlist are rejected.
- Requires a native approval dialog for every repair request. Remediation remains limited to opening fixed built-in Windows tools; the renderer cannot supply executable paths or arguments.

## Findings fixed

- SG-TB-001: web messages previously lacked native source-document and navigation enforcement. A malicious document that reached the WebView could have issued legitimate UI commands, although the existing fixed action switch prevented arbitrary command execution.
- SG-TB-002: most allowlisted repair requests previously trusted renderer-side confirmation. A compromised renderer could have opened fixed elevated Windows management surfaces without a trustworthy approval prompt, but could not choose an arbitrary program or directly mutate the system through those actions.

Full impact analysis and residual risk are documented in docs/threat-model.md.

## Verification evidence

- Detects 12 of 12 isolated broken-state fixtures.
- Reports 0 false positives across 18 fixture decisions (12 broken, 6 clean), with zero findings across the 42 individual results produced by the six healthy fixtures.
- Runs 81 hostile boundary assertions across 10 attack classes, plus 21 valid-command assertions.
- Preserves the measured latency result: parallel checks did not demonstrate a speedup in the recorded sample because one Windows Update API call dominated p95.

The Windows EXE and ZIP attached to this release are built and verified by GitHub Actions from the v1.4.0 tag. Generated binaries are no longer stored in Git.
