using System;

internal static class NativeScannerSmoke
{
    private static int Main()
    {
        try
        {
            GuardianReport report = GuardianScanner.Run(delegate(int percent, string label)
            {
                Console.WriteLine("{0,3}%  {1}", percent, label);
            });

            Console.WriteLine("Score: {0}", report.Score);
            Console.WriteLine("Checks: {0}", report.Checks.Count);
            foreach (GuardianCheck check in report.Checks)
            {
                Console.WriteLine("[{0}] {1}: {2}", check.Status, check.Name, check.Summary);
                if (check.AutomationLevel < 0 || check.AutomationLevel > 4) return 4;
                if (string.IsNullOrWhiteSpace(check.Verification)) return 5;
            }

            if (report.Checks.Count != 7) return 2;
            if (report.Score < 0 || report.Score > 100) return 3;
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
