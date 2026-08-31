using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;

internal sealed class GuardianCheck
{
    public GuardianCheck()
    {
        EvidenceLevel = "Microsoft supported";
        Verification = "Run a fresh System Guardian scan after completing the action.";
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Status { get; set; }
    public string Summary { get; set; }
    public string Detail { get; set; }
    public string Action { get; set; }
    public string ActionLabel { get; set; }
    public bool Fixable { get; set; }
    public int AutomationLevel { get; set; }
    public string EvidenceLevel { get; set; }
    public string Verification { get; set; }
}

internal sealed class GuardianReport
{
    public GuardianReport()
    {
        Checks = new List<GuardianCheck>();
    }

    public string GeneratedAt { get; set; }
    public string DeviceName { get; set; }
    public string WindowsVersion { get; set; }
    public int Score { get; set; }
    public List<GuardianCheck> Checks { get; set; }
}

internal static class GuardianScanner
{
    public static GuardianReport Run(Action<int, string> progress)
    {
        var report = new GuardianReport
        {
            GeneratedAt = DateTime.Now.ToString("o"),
            DeviceName = Environment.MachineName,
            WindowsVersion = ReadWindowsVersion()
        };

        Add(report, progress, 8, "Checking Windows Update", CheckWindowsUpdate);
        Add(report, progress, 23, "Reading Microsoft Defender status", CheckDefender);
        Add(report, progress, 39, "Measuring available storage", CheckStorage);
        Add(report, progress, 54, "Reviewing startup load", CheckStartup);
        Add(report, progress, 69, "Checking devices and drivers", CheckDevices);
        Add(report, progress, 84, "Checking restart status", CheckRestart);
        Add(report, progress, 96, "Verifying core Windows services", CheckCoreServices);

        int deductions = 0;
        foreach (GuardianCheck check in report.Checks)
        {
            if (check.Status == "attention") deductions += 16;
            if (check.Status == "review") deductions += 7;
        }

        report.Score = Math.Max(35, 100 - deductions);
        progress(100, "Preparing your health report");
        return report;
    }

    private static void Add(GuardianReport report, Action<int, string> progress, int percent, string label, Func<GuardianCheck> check)
    {
        progress(percent, label);
        try
        {
            report.Checks.Add(check());
        }
        catch (Exception ex)
        {
            report.Checks.Add(Result(
                label.ToLowerInvariant().Replace(" ", "-"),
                label.Replace("Checking ", ""),
                "System",
                "review",
                "This check could not be completed.",
                ex.Message,
                "Run the app as administrator and scan again.",
                "Review",
                false));
        }
    }

    private static GuardianCheck CheckWindowsUpdate()
    {
        Type sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
        if (sessionType == null)
        {
            return Result("windows-update", "Windows Update", "Updates", "review", "Windows Update was not available for this scan.", "The Windows Update API could not be opened.", "Open Windows Update and check manually.", "Open Update", true);
        }

        dynamic session = Activator.CreateInstance(sessionType);
        dynamic searcher = session.CreateUpdateSearcher();
        dynamic result = searcher.Search("IsInstalled=0 and IsHidden=0");
        int count = result.Updates.Count;
        if (count == 0)
        {
            return Result("windows-update", "Windows Update", "Updates", "good", "Windows is up to date.", "No visible pending updates were returned by Windows Update.", "No action needed.", "Open Update", false);
        }

        return Result("windows-update", "Windows Update", "Updates", "attention", count + " update" + (count == 1 ? " is" : "s are") + " waiting.", "Installing current updates can improve stability and security.", "Open Windows Update, install the pending updates, then restart when convenient.", "Open Update", true);
    }

    private static GuardianCheck CheckDefender()
    {
        using (var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender", "SELECT AntivirusEnabled,RealTimeProtectionEnabled,AntivirusSignatureAge FROM MSFT_MpComputerStatus"))
        {
            foreach (ManagementObject row in searcher.Get())
            {
                bool antivirus = Convert.ToBoolean(row["AntivirusEnabled"]);
                bool realtime = Convert.ToBoolean(row["RealTimeProtectionEnabled"]);
                int age = Convert.ToInt32(row["AntivirusSignatureAge"]);
                if (!antivirus || !realtime)
                {
                    return Result("defender", "Microsoft Defender", "Security", "attention", "Windows protection needs attention.", "Antivirus or real-time protection is not reporting as active.", "Open Windows Security and restore the recommended protection settings.", "Open Security", true);
                }

                if (age > 3)
                {
                    return Result("defender", "Microsoft Defender", "Security", "review", "Security intelligence is out of date.", "Defender definitions are " + age + " days old.", "Update Microsoft Defender security intelligence.", "Update Defender", true);
                }

                return Result("defender", "Microsoft Defender", "Security", "good", "Microsoft Defender is protecting this PC.", "Antivirus, real-time protection, and security intelligence look current.", "No action needed.", "Open Security", false);
            }
        }

        return Result("defender", "Microsoft Defender", "Security", "review", "Defender status was unavailable.", "No Defender status was returned. This can happen when another antivirus product is active.", "Open Windows Security and confirm that a security provider is active.", "Open Security", true);
    }

    private static GuardianCheck CheckStorage()
    {
        var low = new List<string>();
        var details = new List<string>();
        double lowest = 100;
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady || drive.TotalSize == 0) continue;
            double freePercent = drive.AvailableFreeSpace * 100d / drive.TotalSize;
            lowest = Math.Min(lowest, freePercent);
            details.Add(drive.Name + " " + Math.Round(freePercent) + "% free");
            if (freePercent < 15) low.Add(drive.Name);
        }

        string detail = details.Count == 0 ? "No fixed drive details were available." : string.Join(" | ", details.ToArray());
        if (low.Count > 0)
        {
            return Result("storage", "Storage space", "Performance", lowest < 8 ? "attention" : "review", "A drive is running low on space.", detail, "Use Windows cleanup recommendations to remove temporary files safely.", "Clean Storage", true);
        }

        return Result("storage", "Storage space", "Performance", "good", "Your drives have comfortable free space.", detail, "No action needed.", "Open Storage", false);
    }

    private static GuardianCheck CheckStartup()
    {
        int count = 0;
        count += CountRegistryValues(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run");
        count += CountRegistryValues(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run");
        count += CountRegistryValues(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run");
        count += CountFiles(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
        count += CountFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup));

        if (count > 12)
        {
            return Result("startup", "Startup apps", "Performance", "review", count + " apps may start with Windows.", "A large startup list can make sign-in feel slower.", "Review the Startup Apps page and disable only apps you recognize and do not need immediately.", "Review Startup", true);
        }

        return Result("startup", "Startup apps", "Performance", "good", "Startup load looks reasonable.", count + " startup entries were found.", "No action needed.", "Review Startup", false);
    }

    private static GuardianCheck CheckDevices()
    {
        int errors = 0;
        var names = new List<string>();
        using (var searcher = new ManagementObjectSearcher("SELECT Name,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0"))
        {
            foreach (ManagementObject row in searcher.Get())
            {
                errors++;
                if (names.Count < 3) names.Add(Convert.ToString(row["Name"]));
            }
        }

        if (errors > 0)
        {
            return Result("devices", "Devices and drivers", "Stability", "review", errors + " device" + (errors == 1 ? " needs" : "s need") + " review.", names.Count == 0 ? "Windows reported a device error." : string.Join(" | ", names.ToArray()), "Open Device Manager and use Windows Update or the hardware maker's official driver.", "Open Devices", true);
        }

        return Result("devices", "Devices and drivers", "Stability", "good", "Windows reports no device errors.", "Device Manager returned no active error codes.", "No action needed.", "Open Devices", false);
    }

    private static GuardianCheck CheckRestart()
    {
        bool pending = KeyExists(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")
            || KeyExists(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
            || ValueExists(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "PendingFileRenameOperations");
        return pending
            ? Result("restart", "Restart status", "Stability", "review", "Windows is waiting for a restart.", "An update or system change has files waiting to finish at restart.", "Open Windows Update, save your work, and choose Restart now when convenient.", "Open Update", true)
            : Result("restart", "Restart status", "Stability", "good", "No restart is pending.", "Windows is not reporting unfinished restart work.", "No action needed.", "Restart PC", false);
    }

    private static GuardianCheck CheckCoreServices()
    {
        var stopped = new List<string>();
        foreach (string serviceName in new[] { "EventLog", "Winmgmt" })
        {
            using (var service = new ServiceController(serviceName))
            {
                if (service.Status != ServiceControllerStatus.Running) stopped.Add(service.DisplayName);
            }
        }

        return stopped.Count > 0
            ? Result("core-services", "Core Windows services", "Stability", "attention", "An essential Windows service is stopped.", string.Join(" | ", stopped.ToArray()), "Open Windows troubleshooters and review recovery options. System Guardian will not change service state directly.", "Open Troubleshooting", true)
            : Result("core-services", "Core Windows services", "Stability", "good", "Core Windows services are running.", "Windows Event Log and Windows Management Instrumentation are active.", "No action needed.", "Repair Windows", false);
    }

    private static GuardianCheck Result(string id, string name, string category, string status, string summary, string detail, string action, string actionLabel, bool fixable)
    {
        return new GuardianCheck
        {
            Id = id,
            Name = name,
            Category = category,
            Status = status,
            Summary = summary,
            Detail = detail,
            Action = action,
            ActionLabel = actionLabel,
            Fixable = fixable,
            AutomationLevel = GuardianActions.GetAutomationLevel(id),
            Verification = GuardianActions.GetVerification(id)
        };
    }

    private static int CountRegistryValues(RegistryKey root, string path)
    {
        using (RegistryKey key = root.OpenSubKey(path)) return key == null ? 0 : key.GetValueNames().Length;
    }

    private static int CountFiles(string path)
    {
        try { return Directory.Exists(path) ? Directory.GetFiles(path).Length : 0; }
        catch { return 0; }
    }

    private static bool KeyExists(RegistryKey root, string path)
    {
        using (RegistryKey key = root.OpenSubKey(path)) return key != null;
    }

    private static bool ValueExists(RegistryKey root, string path, string name)
    {
        using (RegistryKey key = root.OpenSubKey(path)) return key != null && key.GetValue(name) != null;
    }

    private static string ReadWindowsVersion()
    {
        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                if (key == null) return "Windows";
                string product = Convert.ToString(key.GetValue("ProductName", "Windows"));
                string display = Convert.ToString(key.GetValue("DisplayVersion", ""));
                return (product + " " + display).Trim();
            }
        }
        catch { return "Windows"; }
    }
}

internal static class GuardianActions
{
    public static bool IsAllowed(string id)
    {
        switch (id)
        {
            case "windows-update":
            case "storage":
            case "startup":
            case "devices":
            case "defender":
            case "restart":
            case "core-services":
                return true;
            default:
                return false;
        }
    }

    public static int GetAutomationLevel(string id)
    {
        return IsAllowed(id) ? 1 : 0;
    }

    public static string GetDisplayName(string id)
    {
        switch (id)
        {
            case "windows-update": return "Open Windows Update";
            case "storage": return "Open Storage cleanup";
            case "startup": return "Open Startup Apps";
            case "devices": return "Open Device Manager";
            case "defender": return "Open Windows Security";
            case "restart": return "Open restart options";
            case "core-services": return "Open Windows troubleshooters";
            default: throw new ArgumentOutOfRangeException("id");
        }
    }

    public static string GetConfirmation(string id)
    {
        if (!IsAllowed(id)) throw new ArgumentOutOfRangeException("id");
        return "System Guardian will open a fixed built-in Windows tool. It will not make the change itself. Continue?";
    }

    public static string GetVerification(string id)
    {
        switch (id)
        {
            case "windows-update": return "Install the offered updates, restart if requested, then scan again.";
            case "storage": return "Finish cleanup, then scan again to confirm free space improved.";
            case "startup": return "Restart or sign in again, then check whether startup feels faster.";
            case "devices": return "Confirm the device error is gone in Device Manager, then scan again.";
            case "defender": return "Confirm Windows Security shows active protection, then scan again.";
            case "restart": return "After Windows starts again, run a fresh scan.";
            case "core-services": return "After following the Windows troubleshooting guidance, run a fresh scan.";
            default: throw new ArgumentOutOfRangeException("id");
        }
    }

    public static string Run(string id)
    {
        switch (id)
        {
            case "windows-update":
                Open("ms-settings:windowsupdate");
                return "Windows Update opened. Install the pending updates, then scan again.";
            case "storage":
                Open("ms-settings:storagerecommendations");
                return "Windows cleanup recommendations opened. Review the files before removing them.";
            case "startup":
                Open("ms-settings:startupapps");
                return "Startup Apps opened. Disable only apps you recognize and do not need at sign-in.";
            case "devices":
                Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
                return "Device Manager opened. Use Windows Update or an official hardware-vendor driver.";
            case "defender":
                Open("windowsdefender:");
                return "Windows Security opened. Restore recommended protection or check for updates.";
            case "restart":
                Open("ms-settings:windowsupdate");
                return "Windows Update opened. Save your work before choosing Restart now.";
            case "core-services":
                Open("ms-settings:troubleshoot");
                return "Windows troubleshooters opened. Review the available recovery guidance.";
            default:
                throw new ArgumentOutOfRangeException("id");
        }
    }

    private static void Open(string target)
    {
        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }
}
