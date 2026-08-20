using Content.Client.UserInterface.Systems.Bwoink;
using Content.Client._RuMC14.Governance;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Owns the legacy StaffHelp entry point only so existing references keep compiling.
/// Mentor chat has been removed: F1 always opens the Governance Support Center for players
/// or the Governance responder workspace for active Duty responders.
/// </summary>
public sealed partial class StaffHelpUIController : UIController
{
    [UISystemDependency] private GovernanceAHelpClientSystem _governanceAHelp = default!;

    public bool IsMentor => false;
    public event Action? MentorStatusUpdated;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .BindBefore(
                ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => _governanceAHelp.RequestOpen()),
                typeof(AHelpUIController))
            .Register<StaffHelpUIController>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<StaffHelpUIController>();
        base.Shutdown();
    }

    public void ToggleWindow()
    {
        _governanceAHelp.RequestOpen();
    }
}
