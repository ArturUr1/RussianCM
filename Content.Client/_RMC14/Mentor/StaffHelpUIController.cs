using Content.Client._RuMC14.Governance;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Legacy controller name retained for existing menu references.
/// Mentor Help is retired: F1 and the old Help entry open the Governance Support Center directly.
/// </summary>
public sealed partial class StaffHelpUIController : UIController, IOnSystemChanged<GovernanceAHelpClientSystem>
{
    [UISystemDependency] private GovernanceAHelpClientSystem _governanceAHelp = default!;

    public bool IsMentor => false;
    public event Action? MentorStatusUpdated;

    public void OnSystemLoaded(GovernanceAHelpClientSystem system)
    {
        CommandBinds.Builder
            .BindBefore(
                ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => system.RequestOpen()),
                typeof(AHelpUIController))
            .Register<GovernanceAHelpClientSystem>();
    }

    public void OnSystemUnloaded(GovernanceAHelpClientSystem system)
    {
        CommandBinds.Unregister<GovernanceAHelpClientSystem>();
    }

    public void ToggleWindow()
    {
        _governanceAHelp.RequestOpen();
    }
}
