using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>Show the cosmetic Party Time lineup. Can be toggled live in the pre-round lobby.</summary>
    public static readonly CVarDef<bool> LobbyPartyTime =
        CVarDef.Create("cmu.lobby_party_time", false, CVar.SERVER | CVar.REPLICATED);
}
