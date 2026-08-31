using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;

internal sealed class WindowsUpdateState
{
    public bool Available { get; set; }
    public int PendingCount { get; set; }
    public int OptionalPendingCount { get; set; }
}

internal sealed class DefenderState
{
    public bool Available { get; set; }
    public bool AntivirusEnabled { get; set; }
    public bool RealTimeProtectionEnabled { get; set; }
    public int SignatureAgeDays { get; set; }
}

internal sealed class GuardianDriveState
{
    public string Name { get; set; }
    public long AvailableFreeSpace { get; set; }
    public long TotalSize { get; set; }
}

internal interface IGuardianSystemProbe
{
    string DeviceName { get; }
    string WindowsVersion { get; }
    WindowsUpdateState ReadWindowsUpdate();
    DefenderState ReadDefender();
    IList<GuardianDriveState> ReadFixedDrives();
    int ReadStartupEntryCount();
    IList<string> ReadDeviceErrors();
    bool ReadRestartPending();
    IList<string> ReadStoppedCoreServices();
}

internal sealed class WindowsGuardianSystemProbe : IGuardianSystemProbe
{
    public string DeviceName { get { return Environment.MachineName; } }

    public string WindowsVersion
    {
        get
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

    public WindowsUpdateState ReadWindowsUpdate()
    {
        Type sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session");
        if (sessionType == null) return new WindowsUpdateState { Available = false };

        dynamic session = Activator.CreateInstance(sessionType);
        dynamic searcher = session.CreateUpdateSearcher();
        dynamic result = searcher.Search("IsInstalled=0 and IsHidden=0");
        int actionable = 0;
        int optional = 0;
        for (int i = 0; i < result.Updates.Count; i++)
        {
            dynamic update = result.Updates.Item(i);
            try
            {
                if (Convert.ToBoolean(update.BrowseOnly)) optional++;
                else actionable++;
            }
            catch { actionable++; }
        }
        return new WindowsUpdateState { Available = true, PendingCount = actionable, OptionalPendingCount = optional };
    }

    public DefenderState ReadDefender()
    {
        using (var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\Defender", "SELECT AntivirusEnabled,RealTimeProtectionEnabled,AntivirusSignatureAge FROM MSFT_MpComputerStatus"))
        {
            foreach (ManagementObject row in searcher.Get())
            {
                return new DefenderState
                {
                    Available = true,
                    AntivirusEnabled = Convert.ToBoolean(row["AntivirusEnabled"]),
                    RealTimeProtectionEnabled = Convert.ToBoolean(row["RealTimeProtectionEnabled"]),
                    SignatureAgeDays = Convert.ToInt32(row["AntivirusSignatureAge"])
                };
            }
        }

        return new DefenderState { Available = false };
    }

    public IList<GuardianDriveState> ReadFixedDrives()
    {
        var drives = new List<GuardianDriveState>();
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady || drive.TotalSize == 0) continue;
            drives.Add(new GuardianDriveState
            {
                Name = drive.Name,
                AvailableFreeSpace = drive.AvailableFreeSpace,
                TotalSize = drive.TotalSize
            });
        }
        return drives;
    }

    public int ReadStartupEntryCount()
    {
        int count = 0;
        count += CountRegistryValues(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run");
        count += CountRegistryValues(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run");
        count += CountRegistryValues(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run");
        count += CountFiles(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
        count += CountFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup));
        return count;
    }

    public IList<string> ReadDeviceErrors()
    {
        var names = new List<string>();
        using (var searcher = new ManagementObjectSearcher("SELECT Name,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0"))
        {
            foreach (ManagementObject row in searcher.Get()) names.Add(Convert.ToString(row["Name"]));
        }
        return names;
    }

    public bool ReadRestartPending()
    {
        return KeyExists(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")
            || KeyExists(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")
            || ValueExists(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "PendingFileRenameOperations");
    }

    public IList<string> ReadStoppedCoreServices()
    {
        var stopped = new List<string>();
        foreach (string serviceName in new[] { "EventLog", "Winmgmt" })
        {
            using (var service = new ServiceController(serviceName))
            {
                if (service.Status != ServiceControllerStatus.Running) stopped.Add(service.DisplayName);
            }
        }
        return stopped;
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
}
