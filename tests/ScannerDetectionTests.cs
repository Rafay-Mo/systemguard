using System;
using System.Collections.Generic;

internal static class ScannerDetectionTests
{
    private sealed class Fixture
    {
        public string Condition;
        public string CheckId;
        public string ExpectedStatus;
        public bool Clean;
        public Action<FakeProbe> Configure;
    }

    private sealed class FakeProbe : IGuardianSystemProbe
    {
        public string DeviceName { get { return "fixture-pc"; } }
        public string WindowsVersion { get { return "Windows fixture"; } }
        public WindowsUpdateState Update = new WindowsUpdateState { Available = true, PendingCount = 0 };
        public DefenderState Defender = new DefenderState { Available = true, AntivirusEnabled = true, RealTimeProtectionEnabled = true, SignatureAgeDays = 0 };
        public IList<GuardianDriveState> Drives = new List<GuardianDriveState> { new GuardianDriveState { Name = "C:\\", AvailableFreeSpace = 30, TotalSize = 100 } };
        public int StartupEntries = 4;
        public IList<string> DeviceErrors = new List<string>();
        public bool RestartPending;
        public IList<string> StoppedServices = new List<string>();

        public WindowsUpdateState ReadWindowsUpdate() { return Update; }
        public DefenderState ReadDefender() { return Defender; }
        public IList<GuardianDriveState> ReadFixedDrives() { return Drives; }
        public int ReadStartupEntryCount() { return StartupEntries; }
        public IList<string> ReadDeviceErrors() { return DeviceErrors; }
        public bool ReadRestartPending() { return RestartPending; }
        public IList<string> ReadStoppedCoreServices() { return StoppedServices; }
    }

    private static int Main()
    {
        Fixture[] fixtures =
        {
            Clean("Clean baseline", delegate { }),
            Clean("Clean threshold boundaries", delegate(FakeProbe p) { p.Defender.SignatureAgeDays = 3; p.Drives[0].AvailableFreeSpace = 15; p.StartupEntries = 12; }),
            Broken("Pending Windows updates", "windows-update", "attention", delegate(FakeProbe p) { p.Update.PendingCount = 3; }),
            Broken("Windows Update API unavailable", "windows-update", "review", delegate(FakeProbe p) { p.Update.Available = false; }),
            Broken("Defender antivirus off", "defender", "attention", delegate(FakeProbe p) { p.Defender.AntivirusEnabled = false; }),
            Broken("Defender real-time protection off", "defender", "attention", delegate(FakeProbe p) { p.Defender.RealTimeProtectionEnabled = false; }),
            Broken("Defender signatures four days old", "defender", "review", delegate(FakeProbe p) { p.Defender.SignatureAgeDays = 4; }),
            Broken("Disk 96 percent full", "storage", "attention", delegate(FakeProbe p) { p.Drives[0].AvailableFreeSpace = 4; }),
            Broken("Disk 90 percent full", "storage", "review", delegate(FakeProbe p) { p.Drives[0].AvailableFreeSpace = 10; }),
            Broken("Fifteen startup entries", "startup", "review", delegate(FakeProbe p) { p.StartupEntries = 15; }),
            Broken("Device Manager error", "devices", "review", delegate(FakeProbe p) { p.DeviceErrors.Add("Fixture device"); }),
            Broken("Pending restart flag", "restart", "review", delegate(FakeProbe p) { p.RestartPending = true; }),
            Broken("Windows Event Log stopped", "core-services", "attention", delegate(FakeProbe p) { p.StoppedServices.Add("Windows Event Log"); }),
            Broken("Windows Management Instrumentation stopped", "core-services", "attention", delegate(FakeProbe p) { p.StoppedServices.Add("Windows Management Instrumentation"); })
        };

        int detected = 0;
        int broken = 0;
        int cleanFalsePositives = 0;
        Console.WriteLine("| Condition | Expected | Detected | Other findings |");
        Console.WriteLine("| --- | --- | --- | ---: |");
        foreach (Fixture fixture in fixtures)
        {
            var probe = new FakeProbe();
            fixture.Configure(probe);
            GuardianReport report = GuardianScanner.Run(probe, delegate { });
            int findings = CountFindings(report, fixture.CheckId);
            bool passed;
            if (fixture.Clean)
            {
                passed = findings == 0;
                cleanFalsePositives += findings;
            }
            else
            {
                broken++;
                GuardianCheck target = Find(report, fixture.CheckId);
                passed = target != null && target.Status == fixture.ExpectedStatus && findings == 0;
                if (passed) detected++;
            }

            Console.WriteLine("| {0} | {1} | {2} | {3} |", fixture.Condition, fixture.Clean ? "all good" : fixture.ExpectedStatus, passed ? "yes" : "no", findings);
            if (!passed) return 1;
        }

        Console.WriteLine("Detection: {0}/{1}; clean false positives: {2}.", detected, broken, cleanFalsePositives);
        return detected == broken && cleanFalsePositives == 0 ? 0 : 2;
    }

    private static Fixture Clean(string condition, Action<FakeProbe> configure)
    {
        return new Fixture { Condition = condition, Clean = true, Configure = configure };
    }

    private static Fixture Broken(string condition, string checkId, string status, Action<FakeProbe> configure)
    {
        return new Fixture { Condition = condition, CheckId = checkId, ExpectedStatus = status, Configure = configure };
    }

    private static GuardianCheck Find(GuardianReport report, string id)
    {
        foreach (GuardianCheck check in report.Checks) if (check.Id == id) return check;
        return null;
    }

    private static int CountFindings(GuardianReport report, string expectedId)
    {
        int count = 0;
        foreach (GuardianCheck check in report.Checks)
        {
            if (check.Status != "good" && check.Id != expectedId) count++;
        }
        return count;
    }
}
