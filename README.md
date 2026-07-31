# System Guardian

System Guardian is a Windows desktop app that checks PC health, explains findings in plain English, and opens trusted Windows tools when action is needed.

## What It Does

- Checks Windows Update, Microsoft Defender, storage, startup load, devices, restart state, and core services.
- Summarizes the result with a clear health score and practical next steps.
- Explains each finding before opening a Windows repair or settings tool.
- Keeps scan reports on the PC unless the user explicitly chooses to share them.

## Download And Run

[Download the latest package](Release/SystemGuardian-v1.3.0.zip), extract it, then open `Start-SystemGuardian.cmd`.

System Guardian uses Microsoft Edge WebView2, which is included with most Windows 10 and Windows 11 installations. Windows may request administrator permission so protected health checks and approved repair tools can run.

## Safety And Privacy

System Guardian uses built-in Windows tools and asks for approval before opening a repair action. It does not use registry cleaners, unknown debloat scripts, third-party driver updaters, or actions that disable Windows Update, Microsoft Defender, or the firewall.

## Development

The editable desktop interface is in `src/ui/`, and the native Windows host and scanner are in `src/desktop/`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

The verification script rebuilds the app, runs the native scanner smoke test, and validates the interface and troubleshooting knowledge base. See `docs/ARCHITECTURE.md` and `docs/DEVELOPMENT.md` for project details.
