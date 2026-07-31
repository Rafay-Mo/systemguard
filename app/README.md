# Generated Runtime

This folder is the generated runnable output for the current release.

Do not make source edits here. Edit `src/ui/` or `src/desktop/`, then run:

```powershell
.\scripts\build-desktop.ps1
```

The WebView2 DLLs are checked in so the repository can build without restoring a NuGet package. The Microsoft Edge WebView2 Runtime must still be installed on the target PC.
