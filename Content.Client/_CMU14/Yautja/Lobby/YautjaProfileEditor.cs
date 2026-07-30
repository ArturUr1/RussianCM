using System.Linq;
using System.Numerics;
using Content.Client._CMU14.Yautja;
using Content.Client.Lobby;
using Content.Client.Humanoid;
using Content.Client.Stylesheets;
using Content.Shared._CMU14.Yautja;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Lobby;
using Content.Shared.Preferences;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._CMU14.Yautja.Lobby;

public sealed partial class YautjaProfileEditor : ScrollContainer
{
    private const int VisualButtonSize = 108;
    private const int VisualSpriteSize = 102;
    private const int LabeledVisualButtonSize = VisualButtonSize;
    private const int LabeledVisualSpriteSize = 86;
    private static readonly ProtoId<SpeciesPrototype> YautjaSpecies = "Yautja";
    private static readonly SoundPathSpecifier ModernCloakPreviewSound = new("/Audio/_CMU14/Yautja/pred_cloakon_modern.wav");
    private static readonly SoundPathSpecifier RetroCloakPreviewSound = new("/Audio/_CMU14/Yautja/Equipment/pred_cloakon.wav");
    private static readonly ResPath BracerRsi = new("/Textures/_CMU14/Yautja/bracer.rsi");
    private static readonly ResPath RankRsi = new("/Textures/_CMU14/Yautja/hud_yautja.rsi");

    private readonly LineEdit _name = new();
    private readonly LineEdit _age = new();
    private readonly AnimatedTextureRect _rankIcon = new();
    private readonly Label _rankName = new();
    private readonly OptionButton _status = new();
    private readonly CheckBox _previewWithoutGear = new();
    private readonly Label _summarySet = new();
    private readonly Label _summaryArmor = new();
    private readonly Label _summaryMask = new();
    private readonly Label _summaryGreaves = new();
    private readonly Label _summaryCape = new();
    private readonly Label _summaryBracer = new();
    private readonly Label _summaryCaster = new();
    private readonly OptionButton _translatorType = new();
    private readonly OptionButton _invisibilitySound = new();
    private readonly RichTextLabel _translatorHelp = new();
    private readonly RichTextLabel _invisibilityHelp = new();
    private readonly Label _flavorLimit = new();
    private readonly TextEdit _flavorText = new()
    {
        MinHeight = 90,
        HorizontalExpand = true,
        // MaxLength = YautjaCharacterProfile.MaxFlavorTextLength, Ğ²Ñ‹Ğ´Ğ°ĞµÑ‚ Ğ¾ÑˆĞ¸Ğ±ĞºÑƒ.
    };

    private readonly GridContainer _skinGrid = new() { Columns = 6 };
    private readonly GridContainer _eyeGrid = new() { Columns = 7 };
    private readonly GridContainer _dreadGrid = new() { Columns = 7 };
    private readonly GridContainer _quillGrid = new() { Columns = 6 };
    private readonly GridContainer _legacyGrid = new() { Columns = 4 };
    private readonly GridContainer _uniqueGrid = new() { Columns = 4 };
    private readonly BoxContainer _armorSections = EquipmentSectionContainer();
    private readonly BoxContainer _maskSections = EquipmentSectionContainer();
    private readonly GridContainer _maskAccessoryGrid = new() { Columns = 4 };
    private readonly BoxContainer _greavesSections = EquipmentSectionContainer();
    private readonly BoxContainer _bracerSections = EquipmentSectionContainer();
    private readonly BoxContainer _casterSections = EquipmentSectionContainer();
    private readonly GridContainer _capeGrid = new() { Columns = 4 };
    private readonly ButtonGroup _categoryButtonGroup = new();
    private readonly BoxContainer _categoryNavigation = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
        SeparationOverride = 4,
    };
    private readonly BoxContainer _categoryPages = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
        HorizontalExpand = true,
        VerticalExpand = true,
    };
    private readonly Dictionary<YautjaProfileEditorCategory, Control> _categoryPageControls = new();
    private readonly Dictionary<YautjaProfileEditorCategory, Button> _categoryButtons = new();
    private readonly Dictionary<GridContainer, int> _responsiveGrids = new();
    private readonly List<GridContainer> _bracerResponsiveGrids = new();
    private readonly List<GridContainer> _casterResponsiveGrids = new();
    private YautjaProfileEditorCategory _activeCategory = YautjaProfileEditorCategory.Appearance;

    private readonly SpriteView _preview = new()
    {
        MinSize = new Vector2(190, 230),
        Scale = new Vector2(4, 4),
        OverrideDirection = Direction.South,
        Stretch = SpriteView.StretchMode.Fit,
    };

    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IClientPreferencesManager _preferencesManager = default!;

    private readonly List<EntityUid> _selectorDummies = new();
    private HumanoidCharacterProfile? _profile;
    private EntityUid _previewDummy = EntityUid.Invalid;
    private Direction _previewRotation = Direction.South;
    private YautjaBracerMaterial? _bracerFilter;
    private YautjaBracerMaterial? _casterFilter;
    private bool _updating;
    private YautjaProfileCapabilities _capabilities = YautjaProfileCapabilities.Default;
    private YautjaProfileCapabilities _effectiveCapabilities = YautjaProfileCapabilities.Default;

    public event Action<HumanoidCharacterProfile>? OnProfileChanged;

    public YautjaProfileEditor()
    {
        IoCManager.InjectDependencies(this);
        _previewWithoutGear.Text = Loc.GetString("cmu-yautja-lobby-preview-without-gear");
        _flavorText.Placeholder = new Rope.Leaf(Loc.GetString("cmu-yautja-lobby-flavor-placeholder"));
        _flavorText.ToolTip = Loc.GetString("cmu-yautja-lobby-flavor-limit-tooltip", ("max", YautjaCharacterProfile.MaxFlavorTextLength));
        _flavorLimit.FontColorOverride = Color.FromHex("#b8aaa0");
        UpdateFlavorLimit(0);

        HorizontalExpand = true;
        VerticalExpand = true;

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10),
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 8,
        };
        AddChild(root);

        _rankIcon.MinSize = new Vector2(32, 32);
        _rankIcon.DisplayRect.MinSize = new Vector2(32, 32);
        _rankIcon.DisplayRect.Stretch = TextureRect.StretchMode.Scale;

        var workArea = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 12,
        };
        root.AddChild(workArea);

        var previewColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinWidth = 210,
            Children =
            {
                new PanelContainer
                {
                    MinSize = new Vector2(210, 250),
                    Children = { _preview },
                },
                Row("cmu-yautja-lobby-name", _name),
                Row("cmu-yautja-lobby-age", _age),
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 8,
                    Margin = new Thickness(0, 0, 0, 6),
                    Children =
                    {
                        new Label
                        {
                            Text = Loc.GetString("cmu-yautja-rank"),
                            MinWidth = 110,
                            VerticalAlignment = VAlignment.Center,
                        },
                        _rankIcon,
                        _rankName,
                    },
                },
                Row("cmu-yautja-lobby-status", _status),
                PreviewRotationControls(),
                _previewWithoutGear,
                new PanelContainer
                {
                    Children =
                    {
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            Margin = new Thickness(6),
                            Children =
                            {
                                _summarySet,
                                _summaryArmor,
                                _summaryMask,
                                _summaryGreaves,
                                _summaryCape,
                                _summaryBracer,
                                _summaryCaster,
                            },
                        },
                    },
                },
            },
        };
        workArea.AddChild(previewColumn);

        var categoryWorkspace = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 8,
        };
        categoryWorkspace.AddChild(new PanelContainer
        {
            MinWidth = 176,
            Children = { _categoryNavigation },
        });
        categoryWorkspace.AddChild(new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { _categoryPages },
        });
        workArea.AddChild(categoryWorkspace);

        AddCategory(YautjaProfileEditorCategory.Appearance, new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children =
            {
                VisualBlock("cmu-yautja-lobby-skin-color", _skinGrid),
                VisualBlock("cmu-yautja-lobby-eyes", _eyeGrid),
                VisualBlock("cmu-yautja-lobby-dread-color", _dreadGrid),
                VisualBlock("cmu-yautja-lobby-quills", _quillGrid),
            },
        });
        AddCategory(YautjaProfileEditorCategory.Equipment, BuildEquipmentPage());
        AddCategory(YautjaProfileEditorCategory.Sets, BuildSetsPage());
        AddCategory(YautjaProfileEditorCategory.Technology, BuildTechnologyPage());
        AddCategory(YautjaProfileEditorCategory.Description, FlavorBlock());
        SelectCategory(_activeCategory);
        _categoryPages.OnResized += UpdateResponsiveGridColumns;

        AddTranslatorTypeOptions(_translatorType);
        AddInvisibilitySoundOptions(_invisibilitySound);

        _name.OnTextChanged += args => Mutate(profile => profile.WithName(args.Text));
        _age.OnTextChanged += args =>
        {
            if (int.TryParse(args.Text, out var age))
                Mutate(profile => profile.WithAge(age));
        };
        _previewWithoutGear.OnPressed += _ =>
        {
            if (_profile != null)
                ReloadPreview(_profile.YautjaProfile);
        };
        _flavorText.OnTextChanged += args => OnFlavorTextChanged(args.Control);
        _translatorType.OnItemSelected += args =>
        {
            _translatorType.SelectId(args.Id);
            UpdateTechHelp((YautjaTranslatorType) args.Id, (YautjaInvisibilitySound) _invisibilitySound.SelectedId);
            Mutate(profile => profile.WithTranslatorType((YautjaTranslatorType) args.Id));
        };
        _invisibilitySound.OnItemSelected += args =>
        {
            _invisibilitySound.SelectId(args.Id);
            UpdateTechHelp((YautjaTranslatorType) _translatorType.SelectedId, (YautjaInvisibilitySound) args.Id);
            PlayPreviewSound(GetInvisibilityPreviewSound(args.Id));
            Mutate(profile => profile.WithInvisibilitySound((YautjaInvisibilitySound) args.Id));
        };
        _status.OnItemSelected += args =>
        {
            _status.SelectId(args.Id);
            Mutate(profile => profile.WithStatus((YautjaProfileStatus) args.Id), true);
        };
    }

    public void SetProfile(HumanoidCharacterProfile? profile)
    {
        _profile = profile;
        _updating = true;

        var yautja = profile?.YautjaProfile ?? YautjaCharacterProfile.Default;
        _capabilities = _preferencesManager.YautjaCapabilities;
        _effectiveCapabilities = _capabilities.ForStatus(yautja.Status);
        RebuildStatusSelector(yautja);
        _name.Text = yautja.Name;
        _age.Text = yautja.Age.ToString();
        UpdateRankPresentation();
        _flavorText.TextRope = new Rope.Leaf(yautja.FlavorText);
        UpdateFlavorLimit(yautja.FlavorText.Length);
        _translatorType.SelectId((int) yautja.TranslatorType);
        _invisibilitySound.SelectId((int) yautja.InvisibilitySound);
        UpdateTechHelp(yautja.TranslatorType, yautja.InvisibilitySound);
        RebuildVisualSelectors(yautja);
        UpdateSelectionSummary(yautja);

        _updating = false;
        ReloadPreview(yautja);
    }

    private void RebuildStatusSelector(YautjaCharacterProfile yautja)
    {
        _status.Clear();
        foreach (var status in YautjaCharacterProfile.StatusOrder)
        {
            if (!_capabilities.CanUseStatus(status))
                continue;

            _status.AddItem(Loc.GetString(YautjaCharacterProfile.GetStatusDisplayName(status)), (int) status);
        }

        var selectedStatus = _capabilities.SanitizeStatus(yautja.Status);
        _status.SelectId((int) selectedStatus);
    }

    private void Mutate(Func<YautjaCharacterProfile, YautjaCharacterProfile> update, bool rebuildSelectors = false)
    {
        if (_updating || _profile == null)
            return;

        var yautja = update(_profile.YautjaProfile);
        if (rebuildSelectors)
            yautja = yautja.SanitizeForCapabilities(_capabilities);

        var profile = _profile.WithYautjaProfile(yautja);
        _profile = profile;
        _effectiveCapabilities = _capabilities.ForStatus(profile.YautjaProfile.Status);
        UpdateRankPresentation();
        UpdateSelectionSummary(profile.YautjaProfile);

        if (rebuildSelectors)
            RebuildVisualSelectors(profile.YautjaProfile);

        ReloadPreview(profile.YautjaProfile);
        OnProfileChanged?.Invoke(profile);
    }

    private void UpdateSelectionSummary(YautjaCharacterProfile yautja)
    {
        var summary = YautjaProfileEditorLayout.BuildSummary(yautja);
        _summarySet.Text = Loc.GetString("cmu-yautja-lobby-summary-set", ("value", summary.Set == "â€”"
            ? Loc.GetString("cmu-yautja-lobby-summary-custom")
            : summary.Set));
        _summaryArmor.Text = Loc.GetString(
    ×NvŞÚ$z{-®éÜj×ÒÀ¢Ó° ¢&WGW&âæWræVÄ6öçF–æW ¢°¢†÷&—¦öçFÄW‡æBÒ6ö×7BÀ¢æVÄ÷fW'&–FRÒæWr7G–ÆT&÷„fÆ@¢°¢&6¶w&÷VæD6öÆ÷"Ò6öÆ÷"äg&öÔ†W‚‚"3CR"’À¢&÷&FW$6öÆ÷"Ò6öÆ÷"äg&öÔ†W‚‚"3F#63&"’À¢&÷&FW%F†–6¶æW72ÒæWrF†–6¶æW72ƒ’À¢ÒÀ¢6†–ÆG&VâÒ²–ææW"ÒÀ¢Ó°¢Ğ ¢&—fFR7FF–27G&–ærÖFW&–ÅF—FÆR…–WF¦vV$ÖFW&–ÂÖFW&–Â¢°¢&WGW&âÆö2ävWE7G&–ær…–WF¦6†&7FW%&öf–ÆRävWDÖFW&–ÄF—7Æ”æÖR†ÖFW&–Â’’åFõWW$–çf&–çB‚“°¢Ğ ¢&—fFRfö–B6WD'&6W$f–ÇFW"…–WF¦'&6W$ÖFW&–ÃòÖFW&–Â¢°¢ö'&6W$f–ÇFW"ÒÖFW&–Ã°¢–b…÷&öf–ÆRÒçVÆÂ¢°¢&V'V–ÆD'&6W%6VÆV7F÷"…÷&öf–ÆRå–WF¦&öf–ÆR“°¢WFFU&W7öç6—fTw&–D6öÇVÖç2‚“°¢Ğ¢Ğ ¢&—fFRfö–B6WD67FW$f–ÇFW"…–WF¦'&6W$ÖFW&–ÃòÖFW&–Â¢°¢ö67FW$f–ÇFW"ÒÖFW&–Ã°¢–b…÷&öf–ÆRÒçVÆÂ¢°¢&V'V–ÆD67FW%6VÆV7F÷"…÷&öf–ÆRå–WF¦&öf–ÆR“°¢WFFU&W7öç6—fTw&–D6öÇVÖç2‚“°¢Ğ¢Ğ ¢&—fFR&÷„6öçF–æW"'V–ÆDÖFW&–Äf–ÇFW%6VÆV7F÷"€¢–WF¦'&6W$ÖFW&–Ãò6VÆV7FVBÀ¢•&VDöæÇ”6öÆÆV7F–öãÅ–WF¦'&6W$ÖFW&–ÃâÖFW&–Ç2À¢7F–öãÅ–WF¦'&6W$ÖFW&–Ãóâöå6VÆV7FVB¢°¢f"6VÆV7F÷"ÒæWr÷F–öä'WGFöà¢°¢Ö–åv–GF‚ÒƒÀ¢FööÅF—ÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’Öf–ÇFW""’À¢Ó°¢6VÆV7F÷"äFD—FVÒ„Æö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’Öf–ÇFW"ÖÆÂ"’ÂÓ“°¢f÷&V6‚‡f"ÖFW&–Â–âÖFW&–Ç2¢6VÆV7F÷"äFD—FVÒ„Æö2ävWE7G&–ær…–WF¦6†&7FW%&öf–ÆRävWD'&6W$ÖFW&–ÄF—7Æ”æÖR†ÖFW&–Â’’Â†–çB’ÖFW&–Â“° ¢6VÆV7F÷"å6VÆV7D–B‡6VÆV7FVB—2²ÒÖFW&–Äf–ÇFW"ò†–çB’ÖFW&–Äf–ÇFW"¢Ó“°¢6VÆV7F÷"äöä—FVÕ6VÆV7FVB³Ò&w2Óà¢°¢6VÆV7F÷"å6VÆV7D–B†&w2ä–B“°¢öå6VÆV7FVB†&w2ä–BÂòçVÆÂ¢…–WF¦'&6W$ÖFW&–Â’&w2ä–B“°¢Ó° ¢f"&÷rÒæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâä†÷&—¦öçFÂÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢6W&F–öä÷fW'&–FRÒ‚À¢Ö&v–âÒæWrF†–6¶æW72ƒÂÂÂB’À¢6†–ÆG&VâĞ¢°¢æWrÆ&VÀ¢°¢FW‡BÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’Öf–ÇFW""’À¢Ö–åv–GF‚ÒS"À¢fW'F–6ÄÆ–væÖVçBÒdÆ–væÖVçBä6VçFW"À¢föçD6öÆ÷$÷fW'&–FRÒ6öÆ÷"äg&öÔ†W‚‚"6Cf&c“B"’À¢ÒÀ¢6VÆV7F÷"À¢ÒÀ¢Ó° ¢&WGW&â&÷s°¢Ğ ¢&—fFR7FF–2‡7G&–ærF—FÆRÂ–WF¦'&6W$ÖFW&–ÅµÒÖFW&–Ç2•µÒ'&6W%6V7F–öç2‚¢°¢&WGW&à¢°¢‚&6×R×–WF¦ÖÆö&'’Ö'&6W"×6V7F–öâÖ6÷&R"Â°¢–WF¦'&6W$ÖFW&–Âå&WG&òÀ¢–WF¦'&6W$ÖFW&–ÂäV&öç’À¢–WF¦'&6W$ÖFW&–Âå6–ÇfW"À¢Ò’À¢‚&6×R×–WF¦ÖÆö&'’Ö'&6W"×6V7F–öâ×&VÖ—VÒ"Â°¢–WF¦'&6W$ÖFW&–Âä'&öç¦RÀ¢–WF¦'&6W$ÖFW&–Âä7&–×6öâÀ¢–WF¦'&6W$ÖFW&–Âä&öæRÀ¢Ò’À¢‚&6×R×–WF¦ÖÆö&'’Ö'&6W"×6V7F–öâÖÆVv7’"Â°¢–WF¦'&6W$ÖFW&–ÂäG&vöâÀ¢–WF¦'&6W$ÖFW&–Âå7v×À¢–WF¦'&6W$ÖFW&–ÂäVæf÷&6W"À¢–WF¦'&6W$ÖFW&–Âä6öÆÆV7F÷"À¢Ò’À¢Ó°¢Ğ ¢&—fFRfö–BFD6FVv÷'’…–WF¦&öf–ÆTVF—F÷$6FVv÷'’6FVv÷'’Â6öçG&öÂ6öçFVçB¢°¢f"vRÒ6FVv÷'•67&öÆÂ†6öçFVçB“°¢vRåf—6–&ÆRÒö6FVv÷'•vT6öçG&öÇ2ä6÷VçBÓÒ°¢ö6FVv÷'•vW2äFD6†–ÆB‡vR“°¢ö6FVv÷'•vT6öçG&öÇ5¶6FVv÷'•ÒÒvS° ¢f"FVf–æ—F–öâÒ–WF¦&öf–ÆTVF—F÷$Æ–÷WBä6FVv÷&–W2å6–ævÆR†–æfòÓâ–æfòä–BÓÒ6FVv÷'’“°¢f"'WGFöâÒæWr'WGFöà¢°¢FW‡BÒÆö2ävWE7G&–ær†FVf–æ—F–öâäÆö6Æ—¦F–öä¶W’’À¢FövvÆTÖöFRÒG'VRÀ¢w&÷WÒö6FVv÷'”'WGFöäw&÷WÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢&W76VBÒö6FVv÷'•vT6öçG&öÇ2ä6÷VçBÓÒÀ¢Ó°¢'WGFöâäöå&W76VB³ÒòÓâ6VÆV7D6FVv÷'’†6FVv÷'’“°¢ö6FVv÷'”æf–vF–öâäFD6†–ÆB†'WGFöâ“°¢ö6FVv÷'”'WGFöç5¶6FVv÷'•ÒÒ'WGFöã°¢Ğ ¢&—fFRfö–B&W6WE&W7öç6—fTw&–G2‚¢°¢÷&W7öç6—fTw&–G2ä6ÆV"‚“°¢ö'&6W%&W7öç6—fTw&–G2ä6ÆV"‚“°¢ö67FW%&W7öç6—fTw&–G2ä6ÆV"‚“°¢&Vv—7FW%&W7öç6—fTw&–B…÷6¶–äw&–BÂb“°¢&Vv—7FW%&W7öç6—fTw&–B…öW–Tw&–BÂr“°¢&Vv—7FW%&W7öç6—fTw&–B…öG&VDw&–BÂr“°¢&Vv—7FW%&W7öç6—fTw&–B…÷V–ÆÄw&–BÂb“°¢&Vv—7FW%&W7öç6—fTw&–B…öÆVv7”w&–BÂB“°¢&Vv—7FW%&W7öç6—fTw&–B…÷Væ—VTw&–BÂB“°¢&Vv—7FW%&W7öç6—fTw&–B…öÖ6´66W76÷'”w&–BÂB“°¢&Vv—7FW%&W7öç6—fTw&–B…ö6Tw&–BÂB“°¢Ğ ¢&—fFRw&–D6öçF–æW"&Vv—7FW%&W7öç6—fTw&–B„w&–D6öçF–æW"w&–BÂ–çB&VfW'&VD6öÇVÖç2¢°¢w&–Bä…6W&F–öä÷fW'&–FRÒƒ°¢÷&W7öç6—fTw&–G5¶w&–EÒÒ&VfW'&VD6öÇVÖç3°¢&WGW&âw&–C°¢Ğ ¢&—fFR7FF–2w&–D6öçF–æW"&Vv—7FW%6V7F–öå&W7öç6—fTw&–B„Æ—7CÄw&–D6öçF–æW#â6V7F–öäw&–G2Âw&–D6öçF–æW"w&–B¢°¢6V7F–öäw&–G2äFB†w&–B“°¢&WGW&âw&–C°¢Ğ ¢&—fFRfö–BVç&Vv—7FW%&W7öç6—fTw&–G2„Æ—7CÄw&–D6öçF–æW#âw&–G2¢°¢f÷&V6‚‡f"w&–B–âw&–G2¢÷&W7öç6—fTw&–G2å&VÖ÷fR†w&–B“° ¢w&–G2ä6ÆV"‚“°¢Ğ ¢&—fFRfö–BWFFU&W7öç6—fTw&–D6öÇVÖç2‚¢°¢f"f–Æ&ÆUv–GF‚ÒÖF„bäÖ‚ƒÂö6FVv÷'•vW2åv–GF‚Òb“°¢f÷&V6‚‡f"†w&–BÂ&VfW'&VD6öÇVÖç2’–â÷&W7öç6—fTw&–G2¢°¢w&–Bä6öÇVÖç2Ò–WF¦&öf–ÆTVF—F÷$Æ–÷WBävWE&W7öç6—fT6öÇVÖä6÷VçB†f–Æ&ÆUv–GF‚Â&VfW'&VD6öÇVÖç2“°¢Ğ¢Ğ ¢&—fFRfö–B6VÆV7D6FVv÷'’…–WF¦&öf–ÆTVF—F÷$6FVv÷'’6FVv÷'’¢°¢ö7F—fT6FVv÷'’Ò6FVv÷'“°¢f÷&V6‚‡f"†–BÂvR’–âö6FVv÷'•vT6öçG&öÇ2¢vRåf—6–&ÆRÒ–WF¦&öf–ÆTVF—F÷$Æ–÷WBä—46FVv÷'”7F—fR†6FVv÷'’Â–B“° ¢f÷&V6‚‡f"†–BÂ'WGFöâ’–âö6FVv÷'”'WGFöç2¢'WGFöâå&W76VBÒ–WF¦&öf–ÆTVF—F÷$Æ–÷WBä—46FVv÷'”7F—fR†6FVv÷'’Â–B“°¢Ğ ¢&—fFR6öçG&öÂ'V–ÆDWV—ÖVçEvR‚¢°¢&WGW&âæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâåfW'F–6ÂÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢6†–ÆG&VâĞ¢°¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’Ö&Ö÷""Âö&Ö÷%6V7F–öç2’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’ÖÖ6²"ÂöÖ6µ6V7F–öç2’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’ÖÖ6²Ö66W76÷'’"ÂöÖ6´66W76÷'”w&–B’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’Öw&VfW2"Âöw&VfW56V7F–öç2’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’Ö'&6W""Âö'&6W%6V7F–öç2’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’Ö67FW""Âö67FW%6V7F–öç2’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’Ö6R"Âö6Tw&–B’À¢ÒÀ¢Ó°¢Ğ ¢&—fFR6öçG&öÂ'V–ÆE6WG5vR‚¢°¢&WGW&âæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâåfW'F–6ÂÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢6†–ÆG&VâĞ¢°¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’ÖÆVv7’"ÂöÆVv7”w&–B’À¢f—7VÄ&Æö6²‚&6×R×–WF¦ÖÆö&'’×Væ—VR"Â÷Væ—VTw&–B’À¢ÒÀ¢Ó°¢Ğ ¢&—fFR6öçG&öÂ'V–ÆEFV6†æöÆöw•vR‚¢°¢&WGW&âæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâåfW'F–6ÂÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢6W&F–öä÷fW'&–FRÒ‚À¢6†–ÆG&VâĞ¢°¢FV6„÷F–öä&Æö6²€¢&6×R×–WF¦ÖÆö&'’×G&ç6ÆF÷"×G—R"À¢÷G&ç6ÆF÷%G—RÀ¢÷G&ç6ÆF÷$†VÇÀ¢çVÆÂ’À¢FV6„÷F–öä&Æö6²€¢&6×R×–WF¦ÖÆö&'’Ö–çf—6–&–Æ—G’×6÷VæB"À¢ö–çf—6–&–Æ—G•6÷VæBÀ¢ö–çf—6–&–Æ—G”†VÇÀ¢‚’ÓâÆ•&Wf–Wu6÷VæB„vWD–çf—6–&–Æ—G•&Wf–Wu6÷VæB…ö–çf—6–&–Æ—G•6÷VæBå6VÆV7FVD–B’’’À¢ÒÀ¢Ó°¢Ğ ¢&—fFR7FF–26öçG&öÂ6FVv÷'•67&öÆÂ„6öçG&öÂ6öçG&öÂ¢°¢6öçG&öÂä†÷&—¦öçFÄW‡æBÒG'VS°¢&WGW&âæWr67&öÆÄ6öçF–æW ¢°¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢fW'F–6ÄW‡æBÒG'VRÀ¢Ö–å6—¦RÒæWrfV7F÷#"ƒÂCC’À¢…67&öÆÄVæ&ÆVBÒfÇ6RÀ¢6†–ÆG&VâÒ²6öçG&öÂÒÀ¢Ó°¢Ğ ¢&—fFR6öçG&öÂ&Wf–Wu&÷FF–öä6öçG&öÇ2‚¢°¢f"ÆVgBÒæWr'WGFöà¢°¢FW‡BÒ#Â"À¢Ö–åv–GF‚Ò3"À¢FööÅF—ÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’×&Wf–Wr×&÷FFRÖÆVgB"’À¢Ó°¢f"&–v‡BÒæWr'WGFöà¢°¢FW‡BÒ#â"À¢Ö–åv–GF‚Ò3"À¢FööÅF—ÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’×&Wf–Wr×&÷FFR×&–v‡B"’À¢Ó°¢ÆVgBäöå&W76VB³ÒòÓà¢°¢÷&Wf–Wu&÷FF–öâÒ÷&Wf–Wu&÷FF–öâåGW&ä7r‚“°¢6WE&Wf–Wu&÷FF–öâ…÷&Wf–Wu&÷FF–öâ“°¢Ó°¢&–v‡Bäöå&W76VB³ÒòÓà¢°¢÷&Wf–Wu&÷FF–öâÒ÷&Wf–Wu&÷FF–öâåGW&ä67r‚“°¢6WE&Wf–Wu&÷FF–öâ…÷&Wf–Wu&÷FF–öâ“°¢Ó° ¢&WGW&âæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâä†÷&—¦öçFÂÀ¢†÷&—¦öçFÄÆ–væÖVçBÒ„Æ–væÖVçBä6VçFW"À¢6W&F–öä÷fW'&–FRÒBÀ¢Ö&v–âÒæWrF†–6¶æW72ƒÂBÂÂ"’À¢6†–ÆG&VâĞ¢°¢ÆVgBÀ¢&–v‡BÀ¢ÒÀ¢Ó°¢Ğ ¢&—fFRfö–B6WE&Wf–Wu&÷FF–öâ„F—&V7F–öâF—&V7F–öâ¢°¢÷&Wf–Wrä÷fW'&–FTF—&V7F–öâÒ„F—&V7F–öâ’‚†–çB’F—&V7F–öâRB¢"“°¢Ğ ¢&—fFR6öçG&öÂfÆf÷$&Æö6²‚¢°¢öfÆf÷%FW‡Bä†÷&—¦öçFÄW‡æBÒG'VS°¢öfÆf÷$Æ–Ö—Bä†÷&—¦öçFÄW‡æBÒG'VS°¢öfÆf÷$Æ–Ö—BåFööÅF—ÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’ÖfÆf÷"ÖÆ–Ö—B×FööÇF—"Â‚&Ö‚"Â–WF¦6†&7FW%&öf–ÆRäÖ„fÆf÷%FW‡DÆVæwF‚’“° ¢&WGW&âæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâåfW'F–6ÂÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢Ö&v–âÒæWrF†–6¶æW72ƒÂ"ÂÂ‚’À¢6†–ÆG&VâĞ¢°¢æWrÆ&VÂ²FW‡BÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’ÖfÆf÷""’ÒÀ¢öfÆf÷%FW‡BÀ¢öfÆf÷$Æ–Ö—BÀ¢ÒÀ¢Ó°¢Ğ ¢&—fFRfö–BöäfÆf÷%FW‡D6†ævVB…FW‡DVF—B–çWB¢°¢f"FW‡BÒ&÷Rä6öÆÆ6R†–çWBåFW‡E&÷R“°¢WFFTfÆf÷$Æ–Ö—B‡FW‡BäÆVæwF‚“°¢×WFFR‡&öf–ÆRÓâ&öf–ÆRåv—F„fÆf÷%FW‡B‡FW‡B’“°¢Ğ ¢&—fFRfö–BWFFTfÆf÷$Æ–Ö—B†–çBÆVæwF‚¢°¢öfÆf÷$Æ–Ö—BåFW‡BÒÆö2ävWE7G&–ær€¢&6×R×–WF¦ÖÆö&'’ÖfÆf÷"ÖÆ–Ö—B"À¢‚&6÷VçB"ÂÖF‚äÖ–â†ÆVæwF‚Â–WF¦6†&7FW%&öf–ÆRäÖ„fÆf÷%FW‡DÆVæwF‚’’À¢‚&Ö‚"Â–WF¦6†&7FW%&öf–ÆRäÖ„fÆf÷%FW‡DÆVæwF‚’“°¢Ğ ¢&—fFR6öçG&öÂFV6„÷F–öä&Æö6²‡7G&–ærÆ&VÂÂ÷F–öä'WGFöâ÷F–öâÂ&–6…FW‡DÆ&VÂ†VÇÂ7F–öãò&Wf–Wr¢°¢÷F–öâä†÷&—¦öçFÄW‡æBÒG'VS°¢÷F–öâäÖ–ä†V–v‡BÒ3C° ¢'WGFöãò&Wf–Wt'WGFöâÒçVÆÃ°¢–b‡&Wf–WrÒçVÆÂ¢°¢&Wf–Wt'WGFöâÒæWr'WGFöà¢°¢FW‡BÒÆö2ävWE7G&–ær‚&6×R×–WF¦ÖÆö&'’×&Wf–Wr×6÷VæB"’À¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢Ö–ä†V–v‡BÒ3"À¢Ó°¢&Wf–Wt'WGFöâäöå&W76VB³ÒòÓâ&Wf–Wr‚“°¢Ğ ¢†VÇä†÷&—¦öçFÄW‡æBÒG'VS°¢†VÇåfW'F–6ÄW‡æBÒfÇ6S° ¢f"6öçFVçBÒæWr&÷„6öçF–æW ¢f"&Æö6²ÒæWr&÷„6öçF–æW ¢°¢÷&–VçFF–öâÒ&÷„6öçF–æW"äÆ–÷WD÷&–VçFF–öâåfW'F–6ÂÀ¢†÷&—¦öçFÄW‡æBÒG'VRÀ¢6W&F–öä÷fW'&–FRÒ–WF¦&öf–ÆTVF—F÷$Æ–÷WBåFV6„÷F–öå76–ærÀ¢6†–ÆG&VâĞ¢°¢–WF¦'&6W%V•7G–ÆRäÆ&VÂ„Æö2ävWE7G&–ær†Æ&VÂ’Â–WF¦'&6W%V•7G–ÆRä†÷E&VBÂ$Æ&VÄ†VF–ær"’À¢Ö&v–âÒæWrF†–6¶æW72ƒÂÂÂ–WF¦&öf–ÆTVF—F÷$Æ–÷WBåFV6„÷F–öä&÷GFöÔÖ&v–â’À¢6†–ÆG&VâĞ¢°¢æWrÆ&VÂ²FW‡BÒÆö2ävWE7G&–ær†Æ&VÂ’Â†÷&—¦öçFÄW‡æBÒG'VRÒÀ¢÷F–öâÀ¢ÒÀ¢Ó° ¢–b‡&Wf–Wt'WGFöâÒçVÆÂ¢6öçFVçBäFD6†–ÆB‡&Wf–Wt'WGFöâ“° ¢6öçFVçBäFD6†–ÆB††VÇ“°¢f"6&BÒ–WF¦'&6W%V•7G–ÆRåw&€¢6öçFVçBÀ¢–WF¦'&6W%V•7G–ÆRä6&BÀ¢–WF¦'&6W%V•7G–ÆRä&÷&FW"À¢æWrF†–6¶æW72ƒÂ‚’“°¢6&BäÖ&v–âÒæWrF†–6¶æW72ƒÂÂÂ–WF¦&öf–ÆTVF—F÷$Æ–÷WBåFV6„÷F–öä&÷GFöÔÖ&v–â“°¢&WGW&â6&C°¢&Æö6²äFD6†–ÆB‡&Wf–Wt'WGFöâ“° ¢&Æö6²äFD6†–ÆB††VÇ“°¢&WGW&â&Æö6³°¢Ğ ¢&—fFRfö–BWFFUFV6„†VÇ…–WF¦G&ç6ÆF÷%G—RG&ç6ÆF÷%G—RÂ–WF¦–çf—6–&–Æ—G•6÷VæB–çf—6–&–Æ—G•6÷VæB¢°¢÷G&ç6ÆF÷$†VÇå6WDÖW76vR„Æö2ävWE7G&–ær‡G&ç6ÆF÷%G—R7v—F6€¢°¢–WF¦G&ç6ÆF÷%G—Rå&WG&òÓâ&6×R×–WF¦ÖÆö&'’×G&ç6ÆF÷"Ö†VÇ×&WG&ò"À¢–WF¦G&ç6ÆF÷%G—Rä6öÖ&òÓâ&6×R×–WF¦ÖÆö&'’×G&ç6ÆF÷"Ö†VÇÖ6öÖ&ò"À¢òÓâ&6×R×–WF¦ÖÆö&'’×G&ç6ÆF÷"Ö†VÇÖÖöFW&â"À¢Ò’Â–WF¦'&6W%V•7G–ÆRä×WFVB“°¢ö–çf—6–&–Æ—G”†VÇå6WDÖW76vR„Æö2ävWE7G&–ær†–çf—6–&–Æ—G•6÷VæBÓÒ–WF¦–çf—6–&–Æ—G•6÷VæBå&WG&ğ¢ò&6×R×–WF¦ÖÆö&'’Ö–çf—6–&–Æ—G’Ö†VÇ×&WG&ò ¢¢&6×R×–WF¦ÖÆö&'’Ö–çf—6–&–Æ—G’Ö†VÇÖÖöFW&â"’Â–WF¦'&6W%V•7G–ÆRä×WFVB“°¢Ğ ¢&—fFR7FF–26÷VæEF…7V6–f–W"vWD–çf—6–&–Æ—G•&Wf–Wu6÷VæB†–çB–B¢°¢&WGW&â…–WF¦–çf—6–&–Æ—G•6÷VæB’–BÓÒ–WF¦–çf—6–&–Æ—G•6÷VæBå&WG&ğ¢ò&WG&ô6Æöµ&Wf–Wu6÷Væ@¢¢ÖöFW&ä6Æöµ&Wf–Wu6÷VæC°¢Ğ ¢&—fFRfö–BÆ•&Wf–Wu6÷VæB…6÷VæE7V6–f–W"6÷VæB¢°¢öVçDÖævW"å7—7FVÓÅ6†&VDVF–õ7—7FVÓâ‚’åÆ”vÆö&Â‡6÷VæBÂf–ÇFW"äÆö6Â‚’ÂfÇ6RÂVF–õ&×2äFVfVÇBåv—F…föÇVÖR‚ÓFb’“°¢Ğ ¢&—fFR7FF–2fö–BFEG&ç6ÆF÷%G—T÷F–öç2„÷F–öä'WGFöâ'WGFöâ¢°¢f÷&V6‚‡f"fÇVR–â–WF¦6†&7FW%&öf–ÆRåG&ç6ÆF÷%G—T÷&FW"¢'WGFöâäFD—FVÒ„Æö2ävWE7G&–ær…–WF¦6†&7FW%&öf–ÆRävWEG&ç6ÆF÷%G—TF—7Æ”æÖR‡fÇVR’’Â†–çB’fÇVR“°¢Ğ ¢&—fFR7FF–2fö–BFD–çf—6–&–Æ—G•6÷VæD÷F–öç2„÷F–öä'WGFöâ'WGFöâ¢°¢f÷&V6‚‡f"fÇVR–â–WF¦6†&7FW%&öf–ÆRä–çf—6–&–Æ—G•6÷VæD÷&FW"¢'WGFöâäFD—FVÒ„Æö2ävWE7G&–ær…–WF¦6†&7FW%&öf–ÆRävWD–çf—6–&–Æ—G•6÷VæDF—7Æ”æÖR‡fÇVR’’Â†–çB’fÇVR“°¢Ğ ¢&—fFRfö–BFVÆWFU&Wf–Wr‚¢°¢÷&Wf–Wrå6WDVçF—G’†çVÆÂ“°¢–b…öVçDÖævW"äVçF—G”W†—7G2…÷&Wf–WtGVÖ×’’¢öVçDÖævW"äFVÆWFTVçF—G’…÷&Wf–WtGVÖ×’“°¢÷&Wf–WtGVÖ×’ÒVçF—G•V–Bä–çfÆ–C°¢Ğ ¢&—fFRfö–BF—7÷6U6VÆV7F÷$GVÖÖ–W2‚¢°¢f÷&V6‚‡f"GVÖ×’–â÷6VÆV7F÷$GVÖÖ–W2¢°¢–b…öVçDÖævW"äVçF—G”W†—7G2†GVÖ×’’¢öVçDÖævW"äFVÆWFTVçF—G’†GVÖ×’“°¢Ğ ¢÷6VÆV7F÷$GVÖÖ–W2ä6ÆV"‚“°¢Ğ ¢&÷FV7FVB÷fW'&–FRfö–BW†—FVEG&VR‚¢°¢&6RäW†—FVEG&VR‚“°¢FVÆWFU&Wf–Wr‚“°¢F—7÷6U6VÆV7F÷$GVÖÖ–W2‚“°¢Ğ§Ğ 