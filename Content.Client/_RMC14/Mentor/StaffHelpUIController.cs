using Content.Client._RuMC14.Governance;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Legacy controller name retained for existing menu references.
/// Mentor Help is retired: F1 and the old Help entry open the Governance Support Center directly.
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

    public void ToggleWindow()
    {
        _governanceAHelp.RequestOpen();
    }
}
