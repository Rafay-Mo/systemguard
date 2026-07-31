# Repair Engine

System Guardian uses a diagnostic decision engine. It does not treat a symptom or an AI suggestion as permission to run a command.

## Decision Flow

1. Collect read-only evidence from Windows.
2. Match the evidence to a supported issue family and rule out conflicting causes.
3. Explain the proposed action, impact, and verification step.
4. Ask for explicit consent when the action is privileged or disruptive.
5. Run one allowlisted action at a time.
6. Rescan or retest the original symptom.
7. Stop, roll back, or escalate when the expected result is not achieved.

## Automation Levels

| Level | Meaning | App behavior |
| --- | --- | --- |
| 0 | Observe | Read-only and safe by default |
| 1 | Reversible | Explain the action and keep the user in control |
| 2 | Administrator repair | Require informed consent and a post-check |
| 3 | Disruptive | Require a clear warning, backup guidance, and rollback plan |
| 4 | Manual only | Never run unattended |

## AI Boundary

AI may explain evidence, rank existing repair plans, and translate technical results into plain language. It cannot create commands or bypass the native allowlist. The desktop host remains the sole authority for system changes.

Community reports, videos, and technical media are discovery signals only. They remain `watchlist` evidence until Microsoft or the affected vendor confirms the cause and remedy.

## Knowledge Base

The implementation corpus is [`data/windows-troubleshooting-knowledge-base.json`](../data/windows-troubleshooting-knowledge-base.json). It contains 39 issue families and their symptoms, diagnostics, safe actions, stop conditions, prohibited actions, confidence, and HTTPS sources. `scripts/check-knowledge-base.js` validates it during every full verification run.

Approved and completed repair events are recorded locally in `%LOCALAPPDATA%\SystemGuardian\Logs\repair-audit.jsonl`. The log stores action IDs, safety levels, outcomes, and timestamps; it does not store documents, credentials, or recovery keys.

The corpus was researched and validated on July 20, 2026. Sources and release-specific records must be rechecked before they authorize new native actions.

## Non-Negotiable Stops

- Never collect or retain BitLocker recovery keys, product keys, access tokens, browser data, or private documents.
- Never automatically clear TPM, alter Secure Boot or firmware, edit BCD, change partitions, reset Windows, format a disk, or reinstall Windows.
- Never permanently disable Defender, SmartScreen, firewall, memory integrity, or Windows Update.
- Defer policy-owned changes on domain, Entra, MDM, WSUS, VPN, security, or activation-managed devices.
- Use only exact, signed Microsoft, OEM, or affected-vendor packages for driver or firmware work.
