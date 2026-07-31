# Development

## Requirements

- Windows 10 or Windows 11 x64
- .NET Framework 4.x compiler included with Windows
- Microsoft Edge WebView2 Runtime
- Node.js for the optional UI preview and UI syntax check

No NuGet or npm install is required for the current desktop build.

## Start Here

1. Edit the UI in `src/ui/SystemGuardianUI.html`.
2. Edit the native bridge in `src/desktop/SystemGuardianWebHost.cs`.
3. Edit scans or approved repair actions in `src/desktop/SystemGuardianNative.cs`.
4. Run the full verification command before committing.

```powershell
.\scripts\verify.ps1
```

## Common Commands

```powershell
# Build app/SystemGuardian.exe and refresh the runtime UI
.\scripts\build-desktop.ps1

# Preview the editable UI at http://127.0.0.1:8765
node .\scripts\preview-ui.js

# Run the native scanner smoke test
.\scripts\test-native.ps1

# Build, verify, and package the current VERSION
.\scripts\build-release.ps1
```

## Adding A Check

1. Add a `GuardianCheck` producer in `GuardianScanner`.
2. Give it a stable lowercase ID.
3. Mark it fixable only when an approved action exists.
4. Add the action to `GuardianActions.Run` without accepting arbitrary arguments.
5. Update the report UI only if the existing generic card cannot represent the result.
6. Extend the smoke test when the expected check count changes.

## Release Process

1. Update `VERSION`.
2. Update the manifest assembly version when making a versioned release.
3. Run `scripts/build-release.ps1`.
4. Confirm `Release/` contains only the new package.
5. Commit source, generated runtime files, and the package together.

## Current Product Boundary

The desktop app works locally without the backend. The backend is an MVP for future accounts, report sync, and repair plans. Any integration must preserve the local action whitelist and require approval in the desktop host.
