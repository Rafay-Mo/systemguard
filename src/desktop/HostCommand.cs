using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

internal enum HostCommandType
{
    AppReady,
    ScanStart,
    FixRun,
    WindowMinimize,
    WindowMaximize,
    WindowClose,
    WindowDrag
}

internal sealed class HostCommand
{
    public HostCommand(HostCommandType type, string repairId)
    {
        Type = type;
        RepairId = repairId;
    }

    public HostCommandType Type { get; private set; }
    public string RepairId { get; private set; }
}

internal static class HostCommandParser
{
    private const int MaximumMessageLength = 1024;

    public static bool TryParse(string message, out HostCommand command)
    {
        command = null;
        if (string.IsNullOrWhiteSpace(message) || message.Length > MaximumMessageLength) return false;

        Dictionary<string, object> request;
        try
        {
            request = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(message);
        }
        catch
        {
            return false;
        }

        if (request == null) return false;
        object typeValue;
        if (!request.TryGetValue("type", out typeValue)) return false;
        string type = typeValue as string;
        if (type == null) return false;

        switch (type)
        {
            case "app:ready": return NoParameters(request, HostCommandType.AppReady, out command);
            case "scan:start": return NoParameters(request, HostCommandType.ScanStart, out command);
            case "window:minimize": return NoParameters(request, HostCommandType.WindowMinimize, out command);
            case "window:maximize": return NoParameters(request, HostCommandType.WindowMaximize, out command);
            case "window:close": return NoParameters(request, HostCommandType.WindowClose, out command);
            case "window:drag": return NoParameters(request, HostCommandType.WindowDrag, out command);
            case "fix:run":
                object idValue;
                string id;
                if (request.Count != 2 || !request.TryGetValue("id", out idValue) || (id = idValue as string) == null) return false;
                if (!GuardianActions.IsAllowed(id)) return false;
                command = new HostCommand(HostCommandType.FixRun, id);
                return true;
            default:
                return false;
        }
    }

    private static bool NoParameters(Dictionary<string, object> request, HostCommandType type, out HostCommand command)
    {
        command = null;
        if (request.Count != 1) return false;
        command = new HostCommand(type, null);
        return true;
    }
}
internal static class HostDocumentPolicy
{
    public static bool IsTrustedDocument(Uri trustedDocumentUri, string source)
    {
        Uri candidate;
        return trustedDocumentUri != null
            && !string.IsNullOrWhiteSpace(source)
            && Uri.TryCreate(source, UriKind.Absolute, out candidate)
            && string.Equals(candidate.AbsoluteUri, trustedDocumentUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }
}
