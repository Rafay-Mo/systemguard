# Threat Model

## Scope

System Guardian is an administrator-manifested Windows desktop process containing a WebView2 renderer. The renderer displays the packaged `SystemGuardianUI.html`; the native host reads Windows health state and may open a small set of built-in Windows tools after approval.

The renderer is untrusted. Every message, string, and field crossing from WebView2 to the host is treated as attacker-controlled even though the intended document is local.

## Trust Boundaries

```text
Untrusted WebView2 renderer
        |
        | postMessage (attacker-controlled bytes and source document)
        v
Native message parser and command allowlist
        |
        | explicit native approval for every repair request
        v
Fixed built-in Windows tools and read-only Windows APIs
```

The second boundary is the local filesystem. The packaged HTML and native executable are separate files in the extracted application directory. An attacker able to replace either file already has code execution at that user's privilege level; code signing and installer integrity are outside the current repository's guarantees.

## Protected Assets

- Administrator-level process capabilities.
- Integrity of process launches and their arguments.
- Local Windows health information and repair audit records.
- User control over whether a remediation tool is opened.

## Attacker Capabilities

- Send malformed, oversized, unexpected, or correctly shaped WebView2 messages.
- Control script running in a renderer after renderer compromise.
- Attempt to navigate the WebView2 instance to remote or alternate local content.
- Supply arbitrary command names and repair IDs at the message boundary.

The attacker is not assumed to have already replaced the native executable, modified the operating system, or obtained administrator code execution outside System Guardian. Those capabilities would make this message boundary irrelevant.

## Native Command Table

| Command | Parameters | Native effect | Required privilege |
| --- | --- | --- | --- |
| `app:ready` | None | Returns device name and whether the process is elevated | Standard |
| `scan:start` | None | Runs seven read-only Windows health checks | Standard; some data may require administrator access |
| `fix:run` | `id`: one fixed repair ID | Requests native approval, then opens one allowlisted built-in Windows tool | Native approval; app currently launches elevated by manifest |
| `window:minimize` | None | Minimizes the application window | Standard |
| `window:maximize` | None | Toggles normal/maximized state | Standard |
| `window:close` | None | Closes the application | Standard |
| `window:drag` | None | Moves the borderless application window | Standard |

Host-to-renderer status messages are not privileged commands. The renderer must still treat their text as data and escape it before inserting it into HTML.

## Fixed Repair Targets

The native action layer owns all executable names, URI schemes, and arguments. The renderer can choose only an ID from the fixed set: `windows-update`, `storage`, `startup`, `devices`, `defender`, `restart`, or `core-services`. No renderer-supplied path, executable, URI, argument string, registry path, or file path is accepted.

## Findings And Changes

### SG-TB-001: Web messages lacked source and navigation enforcement

**Before:** `WebMessageReceived` accepted messages without checking `CoreWebView2WebMessageReceivedEventArgs.Source`. The WebView2 instance also had no `NavigationStarting` or `NewWindowRequested` policy. The initial page was local and the current UI contains no remote links, which reduced reachability, but the host did not enforce that security assumption.

**Changed:** the host now computes one canonical packaged-document URI, accepts messages only when `Source` exactly matches it, cancels every navigation to another URI, blocks new windows, denies WebView permissions, disables host-object access, and disables browser accelerator keys. No `AdditionalBrowserArguments` or host objects are configured.

### SG-TB-002: Renderer confirmation was trusted for most repair commands

**Before:** repair IDs were allowlisted in native code, but only `restart` and `core-services` received a native confirmation. Other repair requests could be sent directly by a compromised renderer and would open their fixed Windows tools without a trustworthy approval prompt.

**Changed:** pending in the next focused commit. The host will parse a typed, exact command schema, reject unknown fields and IDs, and obtain native approval for every repair request.

## Controls Already Sound

- Repair selection was already a native switch over fixed IDs, not a general-purpose launcher.
- No UI-controlled string reached a process path, process argument, shell command, file path, or registry path.
- Scanner registry paths and WMI queries were compile-time constants.
- No WebView2 host objects or `AdditionalBrowserArguments` were configured.
- No report-upload path is connected to the desktop runtime.

## Explicitly Out Of Scope

- Compromise of Windows, WebView2 Runtime, or Microsoft/OEM tools.
- An attacker who can replace the native executable or packaged HTML on disk.
- Installer signing, update signing, and distribution-channel integrity.
- Physical attacks and already-administrator attackers.
- The optional backend, which is not wired into the desktop process.

## Residual Risk

The application manifest requests administrator rights at launch. This makes renderer isolation and native approval important, but it also increases impact if an unknown WebView2 escape exists. A future release should consider a split-process architecture with a standard-user UI and a narrowly scoped elevation broker; that architectural change is outside this hardening brief.
