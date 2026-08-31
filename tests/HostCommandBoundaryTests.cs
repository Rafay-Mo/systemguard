using System;
using System.Collections.Generic;

internal static class HostCommandBoundaryTests
{
    private static int assertions;
    private static string currentGroup;
    private static readonly Dictionary<string, int> GroupAssertions = new Dictionary<string, int>();

    private static void Main()
    {
        Group("Valid command baseline", delegate
        {
            Accept("{\"type\":\"app:ready\"}", HostCommandType.AppReady, null);
            Accept("{\"type\":\"scan:start\"}", HostCommandType.ScanStart, null);
            Accept("{\"type\":\"window:minimize\"}", HostCommandType.WindowMinimize, null);
            Accept("{\"type\":\"window:maximize\"}", HostCommandType.WindowMaximize, null);
            Accept("{\"type\":\"window:close\"}", HostCommandType.WindowClose, null);
            Accept("{\"type\":\"window:drag\"}", HostCommandType.WindowDrag, null);
            Accept("{\"type\":\"fix:run\",\"id\":\"windows-update\"}", HostCommandType.FixRun, "windows-update");
        });

        Group("Unknown or misspelled command", delegate
        {
            Reject("{\"type\":\"unknown\"}");
            Reject("{\"type\":\"scan:star\"}");
            Reject("{\"type\":\"window:closed\"}");
        });

        Group("Oversized payload", delegate
        {
            Reject(new string('x', 1025));
            Reject("{\"type\":\"scan:start\",\"padding\":\"" + new string('a', 1024) + "\"}");
        });

        Group("Wrong or missing source origin", delegate
        {
            var trusted = new Uri("file:///C:/Program%20Files/SystemGuardian/SystemGuardianUI.html");
            Assert(HostDocumentPolicy.IsTrustedDocument(trusted, trusted.AbsoluteUri), "Exact packaged document must be trusted.");
            Assert(HostDocumentPolicy.IsTrustedDocument(trusted, "FILE:///C:/Program%20Files/SystemGuardian/SystemGuardianUI.html"), "Windows file URI casing must be accepted.");
            Assert(!HostDocumentPolicy.IsTrustedDocument(trusted, null), "Missing source must be rejected.");
            Assert(!HostDocumentPolicy.IsTrustedDocument(trusted, string.Empty), "Empty source must be rejected.");
            Assert(!HostDocumentPolicy.IsTrustedDocument(trusted, "https://example.test/SystemGuardianUI.html"), "Remote source must be rejected.");
            Assert(!HostDocumentPolicy.IsTrustedDocument(trusted, "file:///C:/Program%20Files/SystemGuardian/other.html"), "Sibling local document must be rejected.");
            Assert(!HostDocumentPolicy.IsTrustedDocument(trusted, "SystemGuardianUI.html"), "Relative source must be rejected.");
        });

        Group("Type confusion", delegate
        {
            Reject("{\"type\":42}");
            Reject("{\"type\":null}");
            Reject("{\"type\":[]}");
            Reject("{\"type\":{}}");
            Reject("{\"type\":\"fix:run\",\"id\":42}");
            Reject("{\"type\":\"fix:run\",\"id\":null}");
            Reject("{\"type\":\"fix:run\",\"id\":[]}");
            Reject("{\"type\":\"fix:run\",\"id\":{}}");
        });

        Group("Path and traversal input", delegate
        {
            Reject("{\"type\":\"fix:run\",\"id\":\"../windows-update\"}");
            Reject("{\"type\":\"fix:run\",\"id\":\"C:\\\\Windows\\\\System32\\\\cmd.exe\"}");
            Reject("{\"type\":\"fix:run\",\"id\":\"file:///C:/Windows/System32/cmd.exe\"}");
            Reject("{\"type\":\"fix:run\",\"id\":\"\\\\\\\\server\\\\share\\\\tool.exe\"}");
        });

        Group("Repair ID allowlist", delegate
        {
            Reject("{\"type\":\"fix:run\",\"id\":\"cmd.exe\"}");
            Reject("{\"type\":\"fix:run\",\"id\":\"optional-update\"}");
            Reject("{\"type\":\"fix:run\",\"id\":\"WINDOWS-UPDATE\"}");
        });

        Group("Unexpected fields", delegate
        {
            Reject("{\"type\":\"scan:start\",\"extra\":true}");
            Reject("{\"type\":\"fix:run\",\"id\":\"windows-update\",\"args\":\"anything\"}");
            Reject("{\"type\":\"fix:run\",\"id\":\"windows-update\",\"approved\":true}");
        });

        Group("Malformed or truncated JSON", delegate
        {
            Reject("not json");
            Reject("{");
            Reject("{\"type\":\"scan:start\"");
            Reject("{\"type\":\"fix:run\",\"id\":\"windows-update\"");
            Reject("{\"type\":");
        });

        Group("Unicode and homoglyph command", delegate
        {
            Reject("{\"type\":\"sc\\u0430n:start\"}");
            Reject("{\"type\":\"scan\\uff1astart\"}");
            Reject("{\"type\":\"f\\u0456x:run\",\"id\":\"windows-update\"}");
            Reject("{\"type\":\"window:cl\\u043ese\"}");
        });

        Group("Missing schema fields", delegate
        {
            Reject(null);
            Reject(string.Empty);
            Reject("window:close");
            Reject("{}");
            Reject("{\"type\":\"fix:run\"}");
        });

        foreach (KeyValuePair<string, int> group in GroupAssertions)
        {
            Console.WriteLine("{0}: {1} assertions", group.Key, group.Value);
        }

        Console.WriteLine("Host command boundary verified: {0} assertions across {1} classes.", assertions, GroupAssertions.Count);
    }

    private static void Group(string name, Action tests)
    {
        currentGroup = name;
        GroupAssertions[name] = 0;
        tests();
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
        GroupAssertions[currentGroup]++;
        if (!condition) throw new InvalidOperationException(message);
    }
}