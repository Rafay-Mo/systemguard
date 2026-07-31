# Architecture

System Guardian is a local-first Windows desktop application. The current release has four clear ownership areas.

## Runtime Flow

```text
src/ui/SystemGuardianUI.html
        |
        | JSON messages through WebView2
        v
src/desktop/SystemGuardianWebHost.cs
        |
        +--> GuardianScanner      read-only Windows checks
        |
        +--> GuardianActions      approved repair entry points
        |
        v
Windows APIs, Settings pages, DISM, SFC, and Defender
```

## Ownership

| Path | Responsibility |
| --- | --- |
| `src/ui/` | Editable dashboard, scan animation, reports, and repair confirmation |
| `src/desktop/SystemGuardianWebHost.cs` | Window lifecycle and WebView2 message bridge |
| `src/desktop/SystemGuardianNative.cs` | Native checks, scoring, and approved Windows actions |
| `data/` | Source-backed troubleshooting knowledge base; never executed directly |
| `backend/` | Optional cloud-service prototype; it is not required by or wired into the current desktop release |
| `app/` | Generated runnable output; do not make source edits here |
| `tests/` | Native scanner smoke tests |
| `Release/` | Current downloadable package only |

## Message Contract

The UI sends these messages to the host:

| Type | Purpose |
| --- | --- |
| `app:ready` | Requests device and permission state |
| `scan:start` | Starts the native read-only scanner |
| `fix:run` | Requests one approved action by check ID |
| `window:*` | Controls the borderless desktop window |

The host responds with `host:ready`, `scan:progress`, `scan:complete`, `scan:error`, `fix:complete`, `fix:cancelled`, or `fix:error`.

## Safety Boundary

- Scan operations are read-only.
- The UI explains and confirms repair actions before calling the host.
- Disruptive repairs receive an additional native confirmation.
- Repair IDs are mapped to code-owned actions; the UI or backend cannot submit arbitrary commands.
- System Guardian does not use registry cleaners, third-party driver updaters, unknown debloat scripts, or actions that disable Windows security.

The repair decision model and automation levels are defined in `docs/REPAIR-ENGINE.md`.

## Generated Files

`scripts/build-desktop.ps1` copies the UI source into `app/` and compiles the host. Commit source changes in `src/`; `app/` is kept in the repository so the current release can run immediately.
