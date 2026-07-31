# System Guardian

System Guardian is a standalone Windows desktop app for checking PC health, explaining findings in plain English, and opening trusted Windows repair tools with clear approval steps.

## Project Layout

```text
app/                         Generated runnable Windows output
src/ui/                      Editable desktop interface source
src/desktop/                 WebView2 host, native scanner, and manifest
src/legacy/                  Archived notes for the earlier WinForms prototype
backend/                     Optional service prototype, not currently wired
data/                        Source-backed troubleshooting knowledge base
tests/                       Native scanner smoke tests
docs/                        Architecture, development, and repair research
scripts/                     Build, verify, preview, release, and Git helpers
Release/                     Current downloadable package only
```

## Developer Quick Start

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

This refreshes the runtime UI, compiles the desktop host and native scanner, runs the real scanner smoke test, and validates the UI source.

Build only:

```powershell
.\scripts\build-desktop.ps1
```

Release package:

```powershell
.\scripts\build-release.ps1
```

See `docs/ARCHITECTURE.md` and `docs/DEVELOPMENT.md` before making structural changes.

The desktop build uses:

```text
src\desktop\SystemGuardianWebHost.cs
src\desktop\SystemGuardianNative.cs
src\ui\SystemGuardianUI.html
```

and produces:

```text
app\SystemGuardian.exe
```

## Run The App

Fastest:

```bat
Start-SystemGuardian.cmd
```

Or run the app directly:

```text
app\SystemGuardian.exe
```

The app uses Microsoft Edge WebView2. Most Windows 10/11 PCs already include it. Windows asks for administrator permission at launch so protected health checks and approved repair tools can work correctly.

## Current App Files

```text
app\SystemGuardian.exe
app\SystemGuardianUI.html
app\Microsoft.Web.WebView2.Core.dll
app\Microsoft.Web.WebView2.WinForms.dll
app\WebView2Loader.dll
```

## Features

- Responsive dark Windows dashboard with compact navigation
- Real native checks for Windows Update, Defender, storage, startup load, devices, restart state, and core services
- Smooth scan radar, progress stages, and persistent scan text
- Health score, category status, report filters, and plain-English issue cards
- Per-issue repair confirmations with risk levels, verification steps, and a local audit log
- Hidden in-app DISM and SFC execution without a terminal window
- Validated 39-family troubleshooting corpus for carefully expanding future coverage

## Downloadable Release

The packaged app is:

```text
Release\SystemGuardian-v1.3.0.zip
```

Users can download the zip, extract it, and open `Start-SystemGuardian.cmd`.

## Source Files

```text
src\desktop\SystemGuardianWebHost.cs
src\desktop\SystemGuardianNative.cs
src\desktop\SystemGuardian.manifest
src\ui\SystemGuardianUI.html
docs\Windows-Optimization-Repair-Guide.md
docs\REPAIR-ENGINE.md
data\windows-troubleshooting-knowledge-base.json
backend\server.js
```

## Test The Native Scanner

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-native.ps1
```

The smoke test runs the same seven Windows checks used by the desktop app and fails if the scanner cannot return a complete report.

## Backend MVP

Run it with:

```bat
backend\Start-Backend.cmd
```

Default API URL:

```text
http://127.0.0.1:4739
```

It includes auth, device registration, report upload, repair-plan generation, update checks, and crash reports. It remains an optional prototype and is not connected to the current desktop release.

## Safety Boundary

System Guardian should only use trusted Windows repair flows. Avoid registry cleaners, unknown debloat scripts, random driver updater apps, or anything that disables Windows Update, Microsoft Defender, firewall, or other security systems.

The repair research and execution policy are in:

```text
docs\Windows-Optimization-Repair-Guide.md
docs\REPAIR-ENGINE.md
```

## GitHub Push

See:

```text
docs\GITHUB_PUSH.md
```

Quick script:

```powershell
.\scripts\push-to-github.ps1 -RemoteUrl "https://github.com/YOUR-USERNAME/YOUR-REPO.git"
```

## Branding Note

This app is inspired by modern security dashboards, but it does not use Malwarebytes code, branding, icons, engine, signatures, or proprietary scanning technology.
