using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            Application.Run(new SystemGuardianHostForm());
        }
        catch (Exception ex)
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            File.WriteAllText(Path.Combine(folder, "SystemGuardianWebHost-error.log"), ex.ToString());
            MessageBox.Show(
                "System Guardian could not open.\n\n" + ex.Message,
                "System Guardian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal sealed class SystemGuardianHostForm : Form
{
    private const int WmNclButtonDown = 0xA1;
    private const int HtCaption = 0x2;

    private readonly WebView2 webView;
    private readonly JavaScriptSerializer json = new JavaScriptSerializer();
    private Uri trustedDocumentUri;
    private bool scanRunning;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    public SystemGuardianHostForm()
    {
        Text = "System Guardian";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1080, 700);
        Size = new Size(1320, 840);
        BackColor = Color.FromArgb(15, 20, 24);
        FormBorderStyle = FormBorderStyle.None;

        webView = new WebView2
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 20, 24),
            DefaultBackgroundColor = Color.FromArgb(15, 20, 24)
        };

        Controls.Add(webView);
        Load += async delegate { await InitializeWebViewAsync(); };
    }

    private async Task InitializeWebViewAsync()
    {
        string appFolder = AppDomain.CurrentDomain.BaseDirectory;
        string uiPath = Path.Combine(appFolder, "SystemGuardianUI.html");
        if (!File.Exists(uiPath))
        {
            MessageBox.Show(
                "Missing SystemGuardianUI.html next to the app.",
                "System Guardian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Close();
            return;
        }

        string profileFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SystemGuardian",
            "WebView2Profile");

        CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, profileFolder);
        await webView.EnsureCoreWebView2Async(environment);

        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
        webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        trustedDocumentUri = new Uri(Path.GetFullPath(uiPath));
        webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        webView.CoreWebView2.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
        };
        webView.CoreWebView2.PermissionRequested += delegate(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            e.State = CoreWebView2PermissionState.Deny;
        };
        webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        webView.CoreWebView2.Navigate(trustedDocumentUri.AbsoluteUri);
    }

    private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!IsTrustedDocument(e.Uri)) e.Cancel = true;
    }

    private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string message;
        if (!IsTrustedDocument(e.Source)) return;

        try
        {
            message = e.TryGetWebMessageAsString();
        }
        catch
        {
            message = string.Empty;
        }

        if (message.StartsWith("{"))
        {
            HandleAppMessage(message);
            return;
        }

        switch (message)
        {
            case "window:minimize":
                WindowState = FormWindowState.Minimized;
                break;
            case "window:maximize":
                WindowState = WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
                break;
            case "window:close":
                Close();
                break;
            case "window:drag":
                if (WindowState == FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Normal;
                }

                ReleaseCapture();
                SendMessage(Handle, WmNclButtonDown, HtCaption, 0);
                break;
        }
    }


    private bool IsTrustedDocument(string source)
    {
        Uri candidate;
        return trustedDocumentUri != null
            && Uri.TryCreate(source, UriKind.Absolute, out candidate)
            && string.Equals(candidate.AbsoluteUri, trustedDocumentUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }
    private async void HandleAppMessage(string message)
    {
        Dictionary<string, object> request;
        try { request = json.Deserialize<Dictionary<string, object>>(message); }
        catch { return; }

        object typeValue;
        if (!request.TryGetValue("type", out typeValue)) return;
        string type = Convert.ToString(typeValue);

        if (type == "app:ready")
        {
            PostToUi(new
            {
                type = "host:ready",
                payload = new
                {
                    deviceName = Environment.MachineName,
                    isAdmin = IsAdministrator()
                }
            });
            return;
        }

        if (type == "scan:start" && !scanRunning)
        {
            scanRunning = true;
            try
            {
                GuardianReport report = await Task.Run(delegate
                {
                    return GuardianScanner.Run(delegate(int percent, string label)
                    {
                        BeginInvoke(new Action(delegate
                        {
                            PostToUi(new { type = "scan:progress", payload = new { progress = percent, label = label } });
                        }));
                    });
                });
                PostToUi(new { type = "scan:complete", payload = report });
            }
            catch (Exception ex)
            {
                PostToUi(new { type = "scan:error", payload = new { message = ex.Message } });
            }
            finally { scanRunning = false; }
            return;
        }

        if (type == "fix:run")
        {
            object idValue;
            if (!request.TryGetValue("id", out idValue)) return;
            string id = Convert.ToString(idValue);
            try
            {
                if (id == "restart")
                {
                    DialogResult answer = MessageBox.Show(this, "Save your work first. Restart this PC in 60 seconds?", "Restart Windows", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes)
                    {
                        RepairAuditLog.Write(id, GuardianActions.GetAutomationLevel(id), "cancelled", "Restart confirmation was declined.");
                        PostToUi(new { type = "fix:cancelled", payload = new { id = id } });
                        return;
                    }
                }

                if (id == "core-services")
                {
                    DialogResult answer = MessageBox.Show(this, "System Guardian will run Microsoft's DISM and SFC repair tools in the background. The repair can take a while. Keep the app open until it finishes. Continue?", "Repair Windows", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (answer != DialogResult.Yes)
                    {
                        RepairAuditLog.Write(id, GuardianActions.GetAutomationLevel(id), "cancelled", "Administrator repair confirmation was declined.");
                        PostToUi(new { type = "fix:cancelled", payload = new { id = id } });
                        return;
                    }
                }

                int level = GuardianActions.GetAutomationLevel(id);
                RepairAuditLog.Write(id, level, "approved", "The user approved the allowlisted action.");
                string result;
                if (id == "core-services")
                {
                    PostToUi(new { type = "fix:progress", payload = new { id = id, message = "Repairing Windows system files. This can take several minutes." } });
                    result = await Task.Run(delegate { return GuardianActions.Run(id); });
                }
                else
                {
                    result = GuardianActions.Run(id);
                }

                RepairAuditLog.Write(id, level, "completed", result);
                PostToUi(new { type = "fix:complete", payload = new { id = id, message = result, verification = GuardianActions.GetVerification(id) } });
            }
            catch (Exception ex)
            {
                RepairAuditLog.Write(id, GuardianActions.GetAutomationLevel(id), "failed", ex.Message);
                PostToUi(new { type = "fix:error", payload = new { id = id, message = ex.Message } });
            }
        }
    }

    private void PostToUi(object payload)
    {
        if (webView.CoreWebView2 == null) return;
        webView.CoreWebView2.PostWebMessageAsJson(json.Serialize(payload));
    }

    private static bool IsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}

internal static class RepairAuditLog
{
    private static readonly object SyncRoot = new object();

    public static void Write(string actionId, int automationLevel, string outcome, string message)
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemGuardian",
                "Logs");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "repair-audit.jsonl");
            string line = new JavaScriptSerializer().Serialize(new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                actionId = actionId,
                automationLevel = automationLevel,
                outcome = outcome,
                message = message
            });
            lock (SyncRoot)
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never turn a supported repair into a failed repair.
        }
    }
}
