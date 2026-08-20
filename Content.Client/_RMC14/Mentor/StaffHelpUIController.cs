using Content.Client._RuMC14.Governance;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Legacy controller name retained for existing menu references.
/// Mentor Help is retired: callers are redirected to the Governance Support Center.
/// </summary>
public sealed partial class StaffHelpUIController : UIController
{
    [UISystemDependency] private GovernanceAHelpClientSystem _governanceAHelp = default!;

    public bool IsMentor => false;
    public event Action? MentorStatusUpdated;

    public void ToggleWindow()
    {
        _governanceAHelp.RequestOpen();
    }
}
