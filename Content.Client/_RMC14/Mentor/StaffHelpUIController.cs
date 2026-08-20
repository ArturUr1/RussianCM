using Content.Client.Administration.Systems;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client._RuMC14.Governance;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Legacy name kept only because existing menu/chat code still references this UI controller.
/// Mentor chat itself is retired: F1 and the old Help button both open Governance Support Center.
/// Legacy mentor NetMessage registration lives in MentorWireCompatibilitySystem so it is ready
/// before login while the server-side mentor backend is being physically removed.
/// </summary>
public sealed partial class StaffHelpUIController : UIController, IOnSystemChanged<BwoinkSystem>
{
    [UISystemDependency] private GovernanceAHelpClientSystem _governanceAHelp = default!;

    public bool IsMentor => false;
    public event Action? MentorStatusUpdated;

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
