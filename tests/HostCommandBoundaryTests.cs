using System;

internal static class HostCommandBoundaryTests
{
    private static int assertions;

    private static void Main()
    {
        Accept("{\"type\":\"app:ready\"}", HostCommandType.AppReady, null);
        Accept("{\"type\":\"scan:start\"}", HostCommandType.ScanStart, null);
        Accept("{\"type\":\"window:minimize\"}", HostCommandType.WindowMinimize, null);
        Accept("{\"type\":\"window:maximize\"}", HostCommandType.WindowMaximize, null);
        Accept("{\"type\":\"window:close\"}", HostCommandType.WindowClose, null);
        Accept("{\"type\":\"window:drag\"}", HostCommandType.WindowDrag, null);
        Accept("{\"type\":\"fix:run\",\"id\":\"windows-update\"}", HostCommandType.FixRun, "windows-update");

        Reject(null);
        Reject(string.Empty);
        Reject("window:close");
        Reject("not json");
        Reject("{}");
        Reject("{\"type\":42}");
        Reject("{\"type\":\"unknown\"}");
        Reject("{\"type\":\"scan:start\",\"extra\":true}");
        Reject("{\"type\":\"fix:run\"}");
        Reject("{\"type\":\"fix:run\",\"id\":42}");
        Reject("{\"type\":\"fix:run\",\"id\":\"cmd.exe\"}");
        Reject("{\"type\":\"fix:run\",\"id\":\"windows-update\",\"args\":\"anything\"}");
        Reject(new string('x', 1025));

        Console.WriteLine("Host command boundary verified: {0} assertions.", assertions);
    }

    private static void Accept(string message, HostCommandType type, string repairId)
    {
        HostCommand command;
        Assert(HostCommandParser.TryParse(message, out command), "Expected command to be accepted: " + message);
        Assert(command.Type == type, "Unexpected command type.");
        Assert(command.RepairId == repairId, "Unexpected repair id.");
    }

    private static void Reject(string message)
    {
        HostCommand command;
        Assert(!HostCommandParser.TryParse(message, out command), "Expected command to be rejected: " + message);
        Assert(command == null, "Rejected command must be null.");
    }

    private static void Assert(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }
}
