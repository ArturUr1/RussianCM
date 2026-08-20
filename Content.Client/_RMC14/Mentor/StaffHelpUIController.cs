using Content.Client.Administration.Systems;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client._RuMC14.Governance;
using Content.Shared._RMC14.Mentor;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Compatibility owner for the old RMC mentor wire types. The mentor chat UI has been removed:
/// F1 now opens the Governance Support Center directly for players and the responder workspace for
/// active Duty responders.
/// </summary>
public sealed partial class StaffHelpUIController : UIController, IOnSystemChanged<BwoinkSystem>
{
    [Dependency] private INetManager _net = default!;
    [UISystemDependency] private GovernanceAHelpClientSystem _governanceAHelp = default!;

    public bool IsMentor => false;
    public event Action? MentorStatusUpdated;

    public override void Initialize()
    {
        // Keep the old wire types registered while the server-side mentor backend is being retired.
        // No mentor message is displayed and the client never sends mentor-chat actions anymore.
        _net.RegisterNetMessage<MentorStatusMsg>(_ => MentorStatusUpdated?.Invoke());
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

    public void OnSystemLoaded(BwoinkSystem system)
    {
        CommandBinds.Builder
            .BindBefore(
                ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => _governanceAHelp.RequestOpen()),
                typeof(AHelpUIController))
            .Register<StaffHelpUIController>();
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        CommandBinds.Unregister<StaffHelpUIController>();
    }

    public void ToggleWindow()
    {
        _governanceAHelp.RequestOpen();
    }
}
