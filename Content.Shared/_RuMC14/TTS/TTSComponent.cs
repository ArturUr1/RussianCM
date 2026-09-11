using Robust.Shared.GameStates;

namespace Content.Shared.Corvax.TTS;

/// <summary>
/// Enables text-to-speech for entity speech.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// TTS voice prototype ID or runtime custom voice ID.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice")]
    public string? VoicePrototypeId { get; set; }

    /// <summary>
    /// Speaker hearing faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("faction")]
    public HearingFaction Faction { get; set; } = HearingFaction.Human;
}

public enum HearingFaction
{
    Human,
    Xeno
}
