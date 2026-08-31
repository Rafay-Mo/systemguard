# Generated Runtime

This folder is the generated runnable output for the current release.

Do not make source edits here. Edit `src/ui/` or `src/desktop/`, then run:

```powershell
.\scripts\build-desktop.ps1
```

The build restores a pinned Microsoft WebView2 SDK package and verifies its SHA-256 checksum. The Microsoft Edge WebView2 Runtime must still be installed on the target PC.
