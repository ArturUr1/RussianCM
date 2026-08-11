using System.Linq;
using System.Numerics;
using Content.Client._RMC14.LinkAccount;
using Content.Client.Stylesheets;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.DonorCapes;

public sealed class DonorCapeTab : BoxContainer
{
    private readonly IPrototypeManager _prototypeManager;
    private readonly LinkAccountManager _linkAccount;
    private readonly ButtonGroup _buttonGroup = new();
    private readonly GridContainer _capeGrid = new() { Columns = 4, HorizontalExpand = true };
    private readonly Label _tierLabel = new();
    private Button _noneButton = default!;
    private readonly List<(RMCDonorCapePrototype Cape, Button Button)> _capeButtons = new();

    private HumanoidCharacterProfile? _profile;

    public event Action<ProtoId<RMCDonorCapePrototype>?>? OnCapeSelected;

    public DonorCapeTab(IPrototypeManager prototypeManager, LinkAccountManager linkAccount)
    {
        _prototypeManager = prototypeManager;
        _linkAccount = linkAccount;

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;
        SeparationOverride = 8;

        AddChild(_tierLabel);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        scroll.AddChild(_capeGrid);
        AddChild(scroll);

        BuildCapeButtons();
        _linkAccount.Updated += RefreshAccess;
    }

    public void SetProfile(HumanoidCharacterProfile? profile)
    {
        _profile = profile;
        RefreshAccess();
    }

    [System.Obsolete]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _linkAccount.Updated -= RefreshAccess;

        base.Dispose(disposing);
    }

    private void BuildCapeButtons()
    {
        _noneButton = BuildButton(
            Loc.GetString("rmc-donor-capes-none"),
            selected: false,
            enabled: true,
            icon: null);
        _noneButton.OnPressed += _ => OnCapeSelected?.Invoke(null);
        _capeGrid.AddChild(_noneButton);

        foreach (var cape in _prototypeManager.EnumeratePrototypes<RMCDonorCapePrototype>().OrderBy(cape => cape.Number))
        {
            var button = BuildButton(
                Loc.GetString(cape.Name),
                selected: false,
                enabled: false,
                icon: cape.Icon);
            button.OnPressed += _ => OnCapeSelected?.Invoke(cape.ID);
            _capeButtons.Add((cape, button));
            _capeGrid.AddChild(button);
        }
    }

    private Button BuildButton(string label, bool selected, bool enabled, SpriteSpecifier? icon)
    {
        var button = new Button
        {
            MinSize = new Vector2(120, 126),
            MaxSize = new Vector2(120, 126),
            ToggleMode = true,
            Pressed = selected,
            Group = _buttonGroup,
            Disabled = !enabled,
            ToolTip = label,
            StyleClasses = { StyleBase.ButtonSquare },
        };

        var children = new List<Control>
        {
            new Label
            {
                Text = label,
                MinSize = new Vector2(108, 18),
                MaxSize = new Vector2(108, 36),
                Align = Label.AlignMode.Center,
                ClipText = true,
            },
        };

        if (icon is { } sprite)
        {
            var iconView = new AnimatedTextureRect
            {
                MinSize = new Vector2(96, 96),
                MaxSize = new Vector2(96, 96),
            };
            iconView.DisplayRect.MinSize = new Vector2(96, 96);
            iconView.DisplayRect.Stretch = TextureRect.StretchMode.Scale;
            iconView.SetFromSpriteSpecifier(sprite);
            children.Add(iconView);

        }

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 2,
        };
        foreach (var child in children)
            container.AddChild(child);

        button.AddChild(container);

        return button;
    }

    private void RefreshAccess()
    {
        var tier = _linkAccount.Tier;
        _tierLabel.Text = tier is { } currentTier
            ? Loc.GetString("rmc-donor-capes-tier", ("tier", currentTier.Tier))
            : Loc.GetString("rmc-donor-capes-no-access");

        var selected = _profile?.SelectedDonorCape;
        _noneButton.Pressed = selected is null;
        foreach (var (cape, button) in _capeButtons)
        {
            var access = DonorCapeAccess.HasAccess(tier, cape.RequiredPriority);
            button.Disabled = !access;
            button.Pressed = selected is { } selectedCape && selectedCape == cape.ID;
            button.ToolTip = access
                ? Loc.GetString(cape.Name)
                : Loc.GetString("rmc-donor-capes-locked", ("tier", GetRequiredTier(cape.RequiredPriority)));
        }
    }

    private string GetRequiredTier(int requiredPriority)
    {
        return requiredPriority switch
        {
            1 => Loc.GetString("rmc-donor-capes-tier-leader"),
            3 => Loc.GetString("rmc-donor-capes-tier-scout"),
            4 => Loc.GetString("rmc-donor-capes-tier-assault"),
            _ => requiredPriority.ToString(),
        };
    }
}
