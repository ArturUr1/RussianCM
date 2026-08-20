using System.Collections;
using System.Reflection;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Content.Server._RuMC14.Governance;

/// <summary>
/// Temporary startup diagnostic for a client/server direct NetMessage mismatch.
/// Remove once the message currently occupying server string-table id 44 is identified.
/// </summary>
public sealed class GovernanceNetMessageDiagnosticsSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    private bool _logged;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_logged || !_net.IsServer)
            return;

        _logged = true;

        try
        {
            var stringsField = _net.GetType().GetField("_strings", BindingFlags.Instance | BindingFlags.NonPublic);
            var stringTable = stringsField?.GetValue(_net);
            var stringsProperty = stringTable?.GetType().GetProperty(
                "Strings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (stringsProperty?.GetValue(stringTable) is not IDictionary strings)
            {
                Log.Warning("[Governance net debug] Could not inspect the server NetMessage string table.");
                return;
            }

            for (var id = 40; id <= 48; id++)
            {
                var name = strings.Contains(id) ? strings[id]?.ToString() ?? "<null>" : "<missing>";
                Log.Warning($"[Governance net debug] server NetMessage id {id} = {name}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"[Governance net debug] Failed to inspect NetMessage string table: {e}");
        }
    }
}
