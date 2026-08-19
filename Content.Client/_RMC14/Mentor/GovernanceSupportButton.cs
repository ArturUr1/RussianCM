using Content.Client._RuMC14.Governance;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Mentor;

/// <summary>
/// Replaces the legacy Admin Help button without disturbing Mentor Help.
/// The hidden OnPressed event intentionally absorbs the old StaffHelpUIController subscription;
/// the real button press opens the native Governance support center instead of Bwoink.
/// </summary>
public sealed class GovernanceSupportButton : Button
{
    public new event Action<ButtonEventArgs>? OnPressed
    {
        add { }
        remove { }
    }

    public GovernanceSupportButton()
    {
        base.OnPressed += _ =>
        {
            IoCManager.Resolve<IEntityManager>()
                .System<GovernanceAHelpClientSystem>()
                .RequestOpen();

            Control? parent = Parent;
            while (parent != null)
            {
                if (parent is StaffHelpWindow window)
                {
                    window.Close();
                    break;
                }

                parent = parent.Parent;
            }
        };
    }
}
