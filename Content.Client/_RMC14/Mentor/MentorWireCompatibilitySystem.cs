using Content.Shared._RMC14.Mentor;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Temporary wire-only compatibility for the retired RMC mentor backend.
/// The mentor UI/chat no longer exists, but the server still registers and may emit these legacy
/// NetMessage types while the backend is being removed. Register them from an EntitySystem so the
/// message-name table is ready before player login; UIController initialization is too late for this.
/// </summary>
public sealed class MentorWireCompatibilitySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<MentorStatusMsg>();
        _net.RegisterNetMessage<MentorMessagesReceivedMsg>();
        _net.RegisterNetMessage<MentorSendMessageMsg>();
        _net.RegisterNetMessage<MentorHelpClientMsg>();
        _net.RegisterNetMessage<DeMentorMsg>();
        _net.RegisterNetMessage<ReMentorMsg>();
        _net.RegisterNetMessage<MentorHelpClientTypingUpdatedMsg>();
        _net.RegisterNetMessage<MentorHelpTypingUpdatedMsg>();
        _net.RegisterNetMessage<MentorClientClaimMsg>();
        _net.RegisterNetMessage<MentorClientUnclaimMsg>();
        _net.RegisterNetMessage<MentorClaimMsg>();
        _net.RegisterNetMessage<MentorUnclaimMsg>();
        _net.RegisterNetMessage<MentorClientTeleportMsg>();
    }
}
