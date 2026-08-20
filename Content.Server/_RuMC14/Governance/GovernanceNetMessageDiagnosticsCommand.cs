using System.Collections;
using System.Reflection;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._RuMC14.Governance;

/// <summary>
/// Temporary diagnostic command for direct NetMessage table mismatches.
/// Remove after the current invalid-id issue is identified.
/// </summary>
public sealed class GovernanceNetMessageDiagnosticsCommand : IConsoleCommand
{
    public string Command => "governance_netmsg";
    public string Description => "Shows the server NetMessage name assigned to a numeric string-table id.";
    public string Help => "Usage: governance_netmsg <0-255>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out var id) || id is < 0 or > byte.MaxValue)
        {
            shell.WriteError(Help);
            return;
        }

        try
        {
            var net = IoCManager.Resolve<INetManager>();
            var stringsField = net.GetType().GetField("_strings", BindingFlags.Instance | BindingFlags.NonPublic);
            var stringTable = stringsField?.GetValue(net);
            var stringsProperty = stringTable?.GetType().GetProperty(
                "Strings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (stringsProperty?.GetValue(stringTable) is not IDictionary strings)
            {
                shell.WriteError("Could not inspect the server NetMessage string table.");
                return;
            }

            var name = strings.Contains(id) ? strings[id]?.ToString() ?? "<null>" : "<missing>";
            shell.WriteLine($"server NetMessage id {id} = {name}");
        }
        catch (Exception e)
        {
            shell.WriteError($"Failed to inspect NetMessage string table: {e.Message}");
        }
    }
}
