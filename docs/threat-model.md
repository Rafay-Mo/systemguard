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

## Boundary Test Coverage

The suite executes 81 hostile assertions across 10 distinct attack classes, plus 21 assertions proving the seven valid command shapes still work.

| Attack class | Assertions | Coverage |
| --- | ---: | --- |
| Unknown or misspelled command | 6 | Unknown names and near-miss spellings |
| Oversized payload | 4 | Raw and JSON-shaped messages beyond 1,024 characters |
| Wrong or missing source origin | 7 | Missing, empty, remote, sibling-file, relative, and exact-source cases |
| Type confusion | 16 | Number, null, array, and object values in string fields |
| Path and traversal input | 8 | Relative traversal, absolute drive path, file URI, and UNC path |
| Repair ID allowlist | 6 | Executable-like, unknown, and case-variant IDs |
| Unexpected fields | 6 | Extra fields on valid scan and repair commands |
| Malformed or truncated JSON | 10 | Non-JSON, incomplete objects, and cut-off fields |
| Unicode and homoglyph command | 8 | Cyrillic and full-width lookalikes in command names |
| Missing schema fields | 10 | Null, empty, non-object, missing type, and missing repair ID |

The source-origin cases call the same HostDocumentPolicy method used by navigation and message handlers. The remaining hostile cases call the production HostCommandParser and native repair allowlist.
## Findings And Changes

### SG-TB-001: Web messages lacked source and navigation enforcement

**Before:** `WebMessageReceived` accepted messages without checking `CoreWebView2WebMessageReceivedEventArgs.Source`. The WebView2 instance also had no `NavigationStarting` or `NewWindowRequested` policy. The initial page was local and the current UI contains no remote links, which reduced reachability, but the host did not enforce that security assumption.

**Changed:** the host now computes one canonical packaged-document URI, accepts messages only when `Source` exactly matches it, cancels every navigation to another URI, blocks new windows, denies WebView permissions, disables host-object access, and disables browser accelerator keys. No `AdditionalBrowserArguments` or host objects are configured.

**Impact:** before the fix, a malicious document that reached this WebView, or script injected into the packaged document, could send the same messages as the legitimate UI. It could start scans, control the app window, and request allowlisted Windows tools. The practical impact was limited by the pre-existing fixed repair switch: it did not provide arbitrary command, path, or argument execution. Reachability also required navigation or renderer-script compromise because the shipped UI did not navigate remotely.
### SG-TB-002: Renderer confirmation was trusted for most repair commands

**Before:** repair IDs were allowlisted in native code, but only `restart` and `core-services` received a native confirmation. Other repair requests could be sent directly by a compromised renderer and would open their fixed Windows tools without a trustworthy approval prompt.

**Changed:** the host now accepts only JSON objects matching an exact typed command schema, rejects unknown commands, extra fields, oversized messages, non-string IDs, and IDs outside the fixed native allowlist. Every repair request receives a native `MessageBox` approval before a tool opens. The `restart` and `core-services` actions were also changed from direct `shutdown`/DISM/SFC execution to opening fixed Windows Settings surfaces, preserving the built-in-tool-only remediation boundary.

**Impact:** before the fix, a compromised renderer could open Windows Update, Storage Settings, Startup Apps, Device Manager, or Windows Security without a trustworthy user approval step. This could interrupt or mislead the user and place an elevated Windows management surface in front of them, but the renderer still could not select an arbitrary executable or directly mutate the system through those actions. The two actions that performed commands already required native confirmation, which further limited unattended impact.
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
