using Content.Shared._RMC14.Mentor;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Temporary early wire registration for the retired mentor backend.
/// The server still registers these legacy direct NetMessage types. They must be registered before
/// the message-name handshake, so this lives in content IoC / IPostInjectInit rather than an
/// EntitySystem or UIController. No mentor UI or chat behavior is provided here.
/// </summary>
public sealed class MentorWireCompatibilityManager : IPostInjectInit
{
    [Dependency] private INetManager _net = default!;

    void IPostInjectInit.PostInject()
    {
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
