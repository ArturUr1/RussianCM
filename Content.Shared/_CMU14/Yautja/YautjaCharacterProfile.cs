using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._CMU14.Yautja;

[Serializable, NetSerializable]
public enum YautjaGearMaterial : byte
{
    Ebony,
    Bronze,
    Silver,
    Crimson,
    Bone,
}

[Serializable, NetSerializable]
public enum YautjaBracerMaterial : byte
{
    Retro,
    Ebony,
    Silver,
    Bronze,
    Crimson,
    Bone,
    Dragon,
    Swamp,
    Enforcer,
    Collector,
}

[Serializable, NetSerializable]
public enum YautjaTranslatorType : byte
{
    Modern,
    Retro,
    Combo,
}

[Serializable, NetSerializable]
public enum YautjaInvisibilitySound : byte
{
    Modern,
    Retro,
}

[Serializable, NetSerializable]
public enum YautjaLegacySet : byte
{
    None,
    Dragon,
    Swamp,
    Enforcer,
    Collector,
}

[Serializable, NetSerializable]
public enum YautjaUniqueSet : byte
{
    None,
    Anubys,
    Cleopatra,
    Plated,
    Ronin,
}

[Serializable, NetSerializable]
public enum YautjaSkinColor : byte
{
    Tan,
    Green,
    Purple,
    Blue,
    Red,
    Black,
}

[Serializable, NetSerializable]
public enum YautjaEyeColor : byte
{
    Gold,
    Amber,
    Copper,
    Red,
    Jade,
    Slate,
    Black,
}

[Serializable, NetSerializable]
public enum YautjaDreadColor : byte
{
    MatchSkin,
    Black,
    DarkBrown,
    Brown,
    Auburn,
    Ash,
    Bone,
}

[Serializable, NetSerializable]
public enum YautjaCapeStyle : byte
{
    Full,
    Ceremonial,
    Third,
    Half,
    Quarter,
    Poncho,
    Damaged,
}

[Serializable, NetSerializable]
public enum YautjaQuillStyle : byte
{
    Standard,
    ShortThick,
    StraightThin,
    LongTied,
    ShortThin,
    LongCurved,
    LongStraight,
    LongWide,
    ShortWide,
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class YautjaCharacterProfile
{
    public const int MaxFlavorTextLength = 512;

    private const int DefaultArmorStyle = 1;
    private const int DefaultMaskStyle = 1;
    private const int DefaultGreavesStyle = 1;
    private const string QuillMarkingPrefix = "CMUYautjaDreadlocks";
    private static readonly Color DefaultCapeColor = C(0x65, 0x43, 0x21);

    public static readonly YautjaGearMaterial[] MaterialOrder =
    [
        YautjaGearMaterial.Ebony,
        YautjaGearMaterial.Silver,
        YautjaGearMaterial.Bronze,
        YautjaGearMaterial.Crimson,
        YautjaGearMaterial.Bone,
    ];

    public static readonly YautjaBracerMaterial[] BracerMaterialOrder =
    [
        YautjaBracerMaterial.Retro,
        YautjaBracerMaterial.Ebony,
        YautjaBracerMaterial.Silver,
        YautjaBracerMaterial.Bronze,
        YautjaBracerMaterial.Crimson,
        YautjaBracerMaterial.Bone,
        YautjaBracerMaterial.Dragon,
        YautjaBracerMaterial.Swamp,
        YautjaBracerMaterial.Enforcer,
        YautjaBracerMaterial.Collector,
    ];

    public static readonly YautjaBracerMaterial[] CasterMaterialOrder =
    [
        YautjaBracerMaterial.Retro,
        YautjaBracerMaterial.Ebony,
        YautjaBracerMaterial.Silver,
        YautjaBracerMaterial.Bronze,
        YautjaBracerMaterial.Crimson,
        YautjaBracerMaterial.Bone,
    ];

    public static readonly YautjaTranslatorType[] TranslatorTypeOrder =
    [
        YautjaTranslatorType.Modern,
        YautjaTranslatorType.Retro,
        YautjaTranslatorType.Combo,
    ];

    public static readonly YautjaInvisibilitySound[] InvisibilitySoundOrder =
    [
        YautjaInvisibilitySound.Modern,
        YautjaInvisibilitySound.Retro,
    ];

    public static readonly YautjaLegacySet[] LegacyOrder =
    [
        YautjaLegacySet.None,
        YautjaLegacySet.Dragon,
        YautjaLegacySet.Swamp,
        YautjaLegacySet.Enforcer,
        YautjaLegacySet.Collector,
    ];

    public static readonly YautjaProfileStatus[] StatusOrder =
    [
        YautjaProfileStatus.Normal,
        YautjaProfileStatus.Council,
        YautjaProfileStatus.Leader,
    ];

    public static readonly YautjaUniqueSet[] UniqueOrder =
    [
        YautjaUniqueSet.None,
        YautjaUniqueSet.Anubys,
        YautjaUniqueSet.Cleopatra,
        YautjaUniqueSet.Plated,
        YautjaUniqueSet.Ronin,
    ];

    public static readonly YautjaSkinColor[] SkinColorOrder =
    [
        YautjaSkinColor.Green,
        YautjaSkinColor.Tan,
        YautjaSkinColor.Purple,
        YautjaSkinColor.Blue,
        YautjaSkinColor.Red,
        YautjaSkinColor.Black,
    ];

    public static readonly YautjaQuillStyle[] QuillStyleOrder =
    [
        YautjaQuillStyle.Standard,
        YautjaQuillStyle.ShortThick,
        YautjaQuillStyle.StraightThin,
        YautjaQuillStyle.LongTied,
        YautjaQuillStyle.ShortThin,
        YautjaQuillStyle.LongCurved,
        YautjaQuillStyle.LongStraight,
        YautjaQuillStyle.LongWide,
        YautjaQuillStyle.ShortWide,
    ];

    public static readonly YautjaEyeColor[] EyeColorOrder =
    [
        YautjaEyeColor.Black,
        YautjaEyeColor.Gold,
        YautjaEyeColor.Amber,
        YautjaEyeColor.Copper,
        YautjaEyeColor.Red,
        YautjaEyeColor.Jade,
        YautjaEyeColor.Slate,
    ];

    public static readonly YautjaDreadColor[] DreadColorOrder =
    [
        YautjaDreadColor.MatchSkin,
        YautjaDreadColor.Black,
        YautjaDreadColor.DarkBrown,
        YautjaDreadColor.Brown,
        YautjaDreadColor.Auburn,
        YautjaDreadColor.Ash,
        YautjaDreadColor.Bone,
    ];

    public static readonly YautjaCapeStyle[] CapeStyleOrder =
    [
        YautjaCapeStyle.Full,
        YautjaCapeStyle.Ceremonial,
        YautjaCapeStyle.Third,
        YautjaCapeStyle.Half,
        YautjaCapeStyle.Quarter,
        YautjaCapeStyle.Poncho,
        YautjaCapeStyle.Damaged,
    ];

    public static readonly Color[] SkinToneColors =
    [
        C(166, 153, 100),
        C(120, 125, 101),
        C(136, 119, 144),
        C(125, 139, 150),
        C(105, 57, 59),
        C(72, 69, 77),
    ];

    public static readonly Color[] EyeColors =
    [
        C(196, 158, 65),
        C(181, 111, 48),
        C(155, 83, 52),
        C(124, 42, 40),
        C(76, 124, 92),
        C(111, 128, 140),
        C(20, 18, 16),
    ];

    public static YautjaCharacterProfile Default => new();

    [DataField]
    public string Name { get; private set; } = "ĞĞµĞ¸Ğ·Ğ²ĞµÑÑ‚Ğ½Ğ¾";

    [DataField]
    public int Age { get; private set; } = 100;

    [DataField]
    public Sex Sex { get; private set; } = Sex.Male;

    [DataField]
    public Gender Gender { get; private set; } = Gender.Male;

    [DataField]
    public HumanoidCharacterAppearance Appearance { get; private set; } = BuildDefaultAppearance();

    [DataField]
    public YautjaDreadColor DreadColor { get; private set; } = YautjaDreadColor.MatchSkin;

    [DataField]
    public YautjaGearMaterial ArmorMaterial { get; private set; } = YautjaGearMaterial.Ebony;

    [DataField]
    public int ArmorStyle { get; private set; } = DefaultArmorStyle;

    [DataField]
    public YautjaGearMaterial MaskMaterial { get; private set; } = YautjaGearMaterial.Ebony;

    [DataField]
    public int MaskStyle { get; private set; } = DefaultMaskStyle;

    [DataField]
    public int MaskAccessoryStyle { get; private set; }

    [DataField]
    public YautjaGearMaterial GreavesMaterial { get; private set; } = YautjaGearMaterial.Ebony;

    [DataField]
    public int GreavesStyle { get; private set; } = DefaultGreavesStyle;

    [DataField]
    public YautjaBracerMaterial BracerMaterial { get; private set; } = YautjaBracerMaterial.Ebony;

    [DataField]
    public YautjaBracerMaterial CasterMaterial { get; private set; } = YautjaBracerMaterial.Ebony;

    [DataField]
    public YautjaRank? ClanRank { get; private set; }

    [DataField]
    public YautjaBracerOwnerRank OwnerRank { get; private set; } = YautjaBracerOwnerRank.Unblooded;

    [DataField]
    public YautjaProfileStatus Status { get; private set; } = YautjaProfileStatus.Normal;

    [DataField]
    public YautjaCapeStyle CapeStyle { get; private set; } = YautjaCapeStyle.Full;

    [DataField]
    public Color CapeColor { get; private set; } = DefaultCapeColor;

    [DataField]
    public YautjaTranslatorType TranslatorType { get; private set; } = YautjaTranslatorType.Modern;

    [DataField]
    public YautjaInvisibilitySound InvisibilitySound { get; private set; } = YautjaInvisibilitySound.Modern;

    [DataField]
    public YautjaLegacySet Legacy { get; private set; } = YautjaLegacySet.None;

    [DataField]
    public YautjaUniqueSet Unique { get; private set; } = YautjaUniqueSet.None;

    [DataField]
    public string FlavorText { get; private set; } = string.Empty;

    public YautjaQuillStyle QuillStyle => GetQuillStyle(Appearance);
    public string QuillMarkingId => GetQuillMarkingId(QuillStyle);
    public YautjaSkinColor SkinColor => GetClosestSkinColor(Appearance.SkinColor);
    public YautjaEyeColor EyeColor => GetClosestEyeColor(Appearance.EyeColor);
    public Color DreadColorValue => GetDreadColorColor(DreadColor, Appearance.SkinColor);

    public string ArmorPrototype => Legacy != YautjaLegacySet.None
        ? $"CMUYautjaArmorLegacy{Legacy}"
        : Unique != YautjaUniqueSet.None
            ? $"CMUYautjaArmorUnique{Unique}"
            : ClanPrototype("CMUYautjaClanArmor", ArmorMaterial, Clamp(ArmorStyle, 1, 8));

    public string MaskPrototype => Legacy != YautjaLegacySet.None
        ? $"CMUYautjaMaskLegacy{Legacy}"
        : Unique != YautjaUniqueSet.None
            ? $"CMUYautjaMaskUnique{Unique}"
            : $"CMUYautjaMaskPred{Clamp(MaskStyle, 1, 20):00}{MaterialSuffix(MaskMaterial)}";

    public string? MaskAccessoryPrototype => MaskAccessoryStyle == 0
        ? null
        : $"CMUYautjaMaskAccessory{Clamp(MaskAccessoryStyle, 1, 3):00}{MaterialSuffix(MaskMaterial)}";

    public string GreavesPrototype => Legacy != YautjaLegacySet.None
        ? $"CMUYautjaGreavesLegacy{Legacy}"
        : Unique != YautjaUniqueSet.None
            ? $"CMUYautjaGreavesUnique{Unique}"
            : ClanPrototype("CMUYautjaClanGreaves", GreavesMaterial, Clamp(GreavesStyle, 1, 4));

    public string BracerPrototype => Legacy != YautjaLegacySet.None
        ? $"CMUYautjaBracerLegacy{Legacy}"
        : BracerMaterial switch
        {
            YautjaBracerMaterial.Retro => "CMUYautjaBracerRetro",
            YautjaBracerMaterial.Silver => "CMUYautjaBracerSilver",
            YautjaBracerMaterial.Bronze => "CMUYautjaBracerBronze",
            YautjaBracerMaterial.Crimson => "CMUYautjaBracerCrimson",
            YautjaBracerMaterial.Bone => "CMUYautjaBracerBone",
            YautjaBracerMaterial.Dragon => "CMUYautjaBracerLegacyDragon",
            YautjaBracerMaterial.Swamp => "CMUYautjaBracerLegacySwamp",
            YautjaBracerMaterial.Enforcer => "CMUYautjaBracerLegacyEnforcer",
            YautjaBracerMaterial.Collector => "CMUYautjaBracerLegacyCollector",
            _ => "CMUYautjaBracerEbony",
        };

    public string CasterPrototype => CasterMaterial switch
    {
        YautjaBracerMaterial.Retro => "CMUYautjaPlasmaCasterRetro",
        YautjaBracerMaterial.Silver => "CMUYautjaPlasmaCasterSilver",
        YautjaBracerMaterial.Bronze => "CMUYautjaPlasmaCasterBronze",
        YautjaBracerMaterial.Crimson => "CMUYautjaPlasmaCasterCrimson",
        YautjaBracerMaterial.Bone => "CMUYautjaPlasmaCasterBone",
        _ => "CMUYautjaPlasmaCasterEbony",
    };

    public string CapePrototype => CapeStyle switch
    {
        YautjaCapeStyle.Ceremonial => "CMUYautjaCapeCeremonial",
        YautjaCapeStyle.Third => "CMUYautjaCapeThird",
        YautjaCapeStyle.Half => "CMUYautjaCapeHalf",
        YautjaCapeStyle.Quarter => "CMUYautjaCapeQuarter",
        YautjaCapeStyle.Poncho => "CMUYautjaCapePoncho",
        YautjaCapeStyle.Damaged => "CMUYautjaCapeDamaged",
        _ => "CMUYautjaCapeFull",
    };

    public string ArmorDisplayName => Legacy != YautjaLegacySet.None
        ? $"cmu-yautja-profile-legacy-{LegacyKey(Legacy)}-armor"
        : Unique != YautjaUniqueSet.None
            ? $"cmu-yautja-profile-unique-{UniqueKey(Unique)}-armor"
            : GetArmorStyleDisplayName(ArmorMaterial, ArmorStyle);

    public string MaskDisplayName => Legacy != YautjaLegacySet.None
        ? $"cmu-yautja-profile-legacy-{LegacyKey(Legacy)}-mask"
        : Unique != YautjaUniqueSet.None
            ? $"cmu-yautja-profile-unique-{UniqueKey(Unique)}-mask"
            : GetMaskStyleDisplayName(MaskMaterial, MaskStyle);

    public string GreavesDisplayName => Legacy != YautjaLegacySet.None
        ? $"cmu-yautja-profile-legacy-{LegacyKey(Legacy)}-greaves"
        : Unique != YautjaUniqueSet.None
            ? $"cmu-yautja-profile-unique-{UniqueKey(Unique)}-greaves"
            : GetGreavesStyleDisplayName(GreavesMaterial, GreavesStyle);

    public string BracerDisplayName => Legacy != YautjaLegacySet.None
        ? $"cmu-yautja-profile-legacy-{LegacyKey(Legacy)}-bracer"
        : GetBracerDisplayName(BracerMaterial);

    public YautjaCharacterProfile()
    {
    }

    private YautjaCharacterProfile(YautjaCharacterProfile other)
    {
        Name = other.Name;
        Age = other.Age;
        Sex = Sex.Male;
        Gender = Gender.Male;
        DreadColor = SanitizeDreadColor(other.DreadColor);
        Appearance = SanitizeAppearance(other.Appearance, DreadColor);
        ArmorMaterial = SanitizeEnum(other.ArmorMaterial, YautjaGearMaterial.Ebony);
        ArmorStyle = other.ArmorStyle is >= 1 and <= 8 ? other.ArmorStyle : DefaultArmorStyle;
        MaskMaterial = SanitizeEnum(other.MaskMaterial, YautjaGearMaterial.Ebony);
        MaskStyle = other.MaskStyle is >= 1 and <= 20 ? other.MaskStyle : DefaultMaskStyle;
        MaskAccessoryStyle = other.MaskAccessoryStyle is >= 0 and <= 3 ? other.MaskAccessoryStyle : 0;
        GreavesMaterial = SanitizeEnum(other.GreavesMaterial, YautjaGearMaterial.Ebony);
        GreavesStyle = other.GreavesStyle is >= 1 and <= 4 ? other.GreavesStyle : DefaultGreavesStyle;
        BracerMaterial = SanitizeEnum(other.BracerMaterial, YautjaBracerMaterial.Ebony);
        CasterMaterial = SanitizeEnum(other.CasterMaterial, YautjaBracerMaterial.Ebony);
        ClanRank = other.ClanRank is { } clanRank && Enum.IsDefined(clanRank) ? clanRank : null;
        OwnerRank = SanitizeEnum(other.OwnerRank, YautjaBracerOwnerRank.Unblooded);
        Status = SanitizeEnum(other.Status, YautjaProfileStatus.Normal);
        CapeStyle = SanitizeEnum(other.CapeStyle, YautjaCapeStyle.Full);
        CapeColor = other.CapeColor;
        TranslatorType = SanitizeEnum(other.TranslatorType, YautjaTranslatorType.Modern);
        InvisibilitySound = SanitizeEnum(other.InvisibilitySound, YautjaInvisibilitySound.Modern);
        Legacy = SanitizeEnum(other.Legacy, YautjaLegacySet.None);
        Unique = SanitizeEnum(other.Unique, YautjaUniqueSet.None);
        FlavorTexÛnõ¶‰ËkºwµçUÉ¥…°¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ôµ…Ñ•É¥…°¥Ìe…ÕÑ©…	É…•É5…Ñ•É¥…°¹É…½¸½È(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹Mİ…µÀ½È(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹¹™½É•È½È(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹½±±•Ñ½È(€€€€€€€€€€€€ü€‰±•…äˆ(€€€€€€€€€€€€è€‰±…¸ˆì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ‰É…•Èµí	É…•É5…Ñ•É¥…±-•ä¡µ…Ñ•É¥…°¥ôµíÍÕ™™¥áôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ…ÍÑ•É¥ÍÁ±…å9…µ”¡e…ÕÑ©…	É…•É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ…ÍÑ•Èµí	É…•É5…Ñ•É¥…±-•ä¡µ…Ñ•É¥…°¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ…Á•¥ÍÁ±…å9…µ”¡e…ÕÑ©……Á•MÑå±”ÍÑå±”¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ôÍÑå±”Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©……Á•MÑå±”¹•É•µ½¹¥…°€ôø€‰•É•µ½¹¥…°ˆ°(€€€€€€€€€€€e…ÕÑ©……Á•MÑå±”¹Q¡¥É€ôø€‰Ñ¡¥Éˆ°(€€€€€€€€€€€e…ÕÑ©……Á•MÑå±”¹!…±˜€ôø€‰¡…±˜ˆ°(€€€€€€€€€€€e…ÕÑ©……Á•MÑå±”¹EÕ…ÉÑ•È€ôø€‰ÅÕ…ÉÑ•Èˆ°(€€€€€€€€€€€e…ÕÑ©……Á•MÑå±”¹A½¹¡¼€ôø€‰Á½¹¡¼ˆ°(€€€€€€€€€€€e…ÕÑ©……Á•MÑå±”¹…µ…•€ôø€‰‘…µ…•ˆ°(€€€€€€€€€€€|€ôø€‰™Õ±°ˆ°(€€€€€€€ôì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ…Á”µíÍÕ™™¥áôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ5…Í­•ÍÍ½Éå¥ÍÁ±…å9…µ”¡¥¹ĞÍÑå±”°e…ÕÑ©…•…É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸ÍÑå±”€ôô€À(€€€€€€€€€€€€ü€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µµ…Í¬µ…•ÍÍ½Éäµ¹½¹”ˆ(€€€€€€€€€€€€è€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µµ…Í¬µ…•ÍÍ½Éäµí5…Ñ•É¥…±-•ä¡µ…Ñ•É¥…°¥ôµí±…µÀ¡ÍÑå±”°€Ä°€Ì¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ5…Ñ•É¥…±¥ÍÁ±…å9…µ”¡e…ÕÑ©…•…É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µµ…Ñ•É¥…°µí5…Ñ•É¥…±-•ä¡µ…Ñ•É¥…°¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ	É…•É5…Ñ•É¥…±¥ÍÁ±…å9…µ”¡e…ÕÑ©…	É…•É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ‰É…•Èµµ…Ñ•É¥…°µí	É…•É5…Ñ•É¥…±-•ä¡µ…Ñ•É¥…°¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑQÉ…¹Í±…Ñ½ÉQåÁ•¥ÍÁ±…å9…µ”¡e…ÕÑ©…QÉ…¹Í±…Ñ½ÉQåÁ”ÑåÁ”¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ôÑåÁ”Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…QÉ…¹Í±…Ñ½ÉQåÁ”¹I•ÑÉ¼€ôø€‰É•ÑÉ¼ˆ°(€€€€€€€€€€€e…ÕÑ©…QÉ…¹Í±…Ñ½ÉQåÁ”¹½µ‰¼€ôø€‰½µ‰¼ˆ°(€€€€€€€€€€€|€ôø€‰µ½‘•É¸ˆ°(€€€€€€€ôì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µÑÉ…¹Í±…Ñ½ÈµíÍÕ™™¥áôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ%¹Ù¥Í¥‰¥±¥ÑåM½Õ¹‘¥ÍÁ±…å9…µ”¡e…ÕÑ©…%¹Ù¥Í¥‰¥±¥ÑåM½Õ¹Í½Õ¹¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ôÍ½Õ¹€ôôe…ÕÑ©…%¹Ù¥Í¥‰¥±¥ÑåM½Õ¹¹I•ÑÉ¼€ü€‰É•ÑÉ¼ˆ€è€‰µ½‘•É¸ˆì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ¥¹Ù¥Í¥‰¥±¥ÑäµÍ½Õ¹µíÍÕ™™¥áôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñ1•…å¥ÍÁ±…å9…µ”¡e…ÕÑ©…1•…åM•Ğ±•…ä¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ±•…äµí1•…å-•ä¡±•…ä¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑMÑ…ÑÕÍ¥ÍÁ±…å9…µ”¡e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌÍÑ…ÑÕÌ¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ôÍÑ…ÑÕÌÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌ¹½Õ¹¥°€ôø€‰½Õ¹¥°ˆ°(€€€€€€€€€€€e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌ¹1•…‘•È€ôø€‰±•…‘•Èˆ°(€€€€€€€€€€€|€ôø€‰¹½Éµ…°ˆ°(€€€€€€€ôì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µÍÑ…ÑÕÌµíÍÕ™™¥áôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑU¹¥ÅÕ•¥ÍÁ±…å9…µ”¡e…ÕÑ©…U¹¥ÅÕ•M•ĞÕ¹¥ÅÕ”¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µÕ¹¥ÅÕ”µíU¹¥ÅÕ•-•ä¡Õ¹¥ÅÕ”¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑM­¥¹½±½É¥ÍÁ±…å9…µ”¡e…ÕÑ©…M­¥¹½±½ÈÍ­¥¹½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µÍ­¥¸µ½±½ÈµíM­¥¹½±½É-•ä¡Í­¥¹½±½È¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•Ñå•½±½É¥ÍÁ±…å9…µ”¡e…ÕÑ©…å•½±½È•å•½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ•å”µ½±½Èµíå•½±½É-•ä¡•å•½±½È¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑÉ•…‘½±½É¥ÍÁ±…å9…µ”¡e…ÕÑ©…É•…‘½±½È‘É•…‘½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µ‘É•…µ½±½ÈµíÉ•…‘½±½É-•ä¡‘É•…‘½±½È¥ôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑEÕ¥±±MÑå±•¥ÍÁ±…å9…µ”¡e…ÕÑ©…EÕ¥±±MÑå±”ÍÑå±”¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ôÍÑå±”Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹M¡½ÉÑQ¡¥¬€ôø€‰Í¡½ÉĞµÑ¡¥¬ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹MÑÉ…¥¡ÑQ¡¥¸€ôø€‰ÍÑÉ…¥¡ĞµÑ¡¥¸ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹Q¥•€ôø€‰±½¹œµÑ¥•ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹M¡½ÉÑQ¡¥¸€ôø€‰Í¡½ÉĞµÑ¡¥¸ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹ÕÉÙ•€ôø€‰±½¹œµÕÉÙ•ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹MÑÉ…¥¡Ğ€ôø€‰±½¹œµÍÑÉ…¥¡Ğˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹]¥‘”€ôø€‰±½¹œµİ¥‘”ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹M¡½ÉÑ]¥‘”€ôø€‰Í¡½ÉĞµİ¥‘”ˆ°(€€€€€€€€€€€|€ôø€‰ÍÑ…¹‘…Éˆ°(€€€€€€€ôì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µÅÕ¥±°µíÍÕ™™¥áôˆì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ½±½È•ÑM­¥¹Q½¹•½±½È¡¥¹Ğ¥¹‘•à¤(€€€ì(€€€€€€€É•ÑÕÉ¸M­¥¹Q½¹•½±½ÉÍm±…µÀ¡¥¹‘•à°€À°M­¥¹Q½¹•½±½ÉÌ¹1•¹Ñ €´€Ä¥tì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ¥¹Ğ•Ñ±½Í•ÍÑM­¥¹Q½¹•%¹‘•à¡½±½È½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸ÉÉ…ä¹%¹‘•á=˜¡M­¥¹½±½É=É‘•È°•Ñ±½Í•ÍÑM­¥¹½±½È¡½±½È¤¤ì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ½±½È•Ñ±½Í•ÍÑM­¥¹Q½¹•½±½È¡½±½È½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸•ÑM­¥¹½±½É½±½È¡•Ñ±½Í•ÍÑM­¥¹½±½È¡½±½È¤¤ì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ½±½È•ÑM­¥¹½±½É½±½È¡e…ÕÑ©…M­¥¹½±½È½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸½±½ÈÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹É••¸€ôøM­¥¹Q½¹•½±½ÉÍlÅt°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹AÕÉÁ±”€ôøM­¥¹Q½¹•½±½ÉÍlÉt°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹	±Õ”€ôøM­¥¹Q½¹•½±½ÉÍlÍt°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹I•€ôøM­¥¹Q½¹•½±½ÉÍlÑt°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹	±…¬€ôøM­¥¹Q½¹•½±½ÉÍlÕt°(€€€€€€€€€€€|€ôøM­¥¹Q½¹•½±½ÉÍlÁt°(€€€€€€€ôì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ½±½È•Ñå•½±½É½±½È¡e…ÕÑ©…å•½±½È½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸½±½ÈÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹µ‰•È€ôøå•½±½ÉÍlÅt°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹½ÁÁ•È€ôøå•½±½ÉÍlÉt°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹I•€ôøå•½±½ÉÍlÍt°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹)…‘”€ôøå•½±½ÉÍlÑt°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹M±…Ñ”€ôøå•½±½ÉÍlÕt°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹	±…¬€ôøå•½±½ÉÍlÙt°(€€€€€€€€€€€|€ôøå•½±½ÉÍlÁt°(€€€€€€€ôì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ½±½È•ÑÉ•…‘½±½É½±½È¡e…ÕÑ©…É•…‘½±½È½±½È°½±½ÈÍ­¥¹½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸½±½ÈÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹	±…¬€ôø ÈÀ°€Äà°€ÄØ¤°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹…É­	É½İ¸€ôø ĞÔ°€ÌÈ°€ÈĞ¤°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹	É½İ¸€ôø Üà°€ÔĞ°€ÌĞ¤°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹Õ‰ÕÉ¸€ôø äĞ°€Ğà°€ÌØ¤°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹Í €ôø ÄÀÔ°€ÄÀÔ°€ÄÀÀ¤°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹	½¹”€ôø ÄàÔ°€ÄÜĞ°€ÄĞÔ¤°(€€€€€€€€€€€|€ôøÍ­¥¹½±½È¹]¥Ñ¡±Á¡„ Å˜¤°(€€€€€€€ôì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥Œ½±½È•Ñ±½Í•ÍÑå•½±½É½±½È¡½±½È½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸•Ñå•½±½É½±½È¡•Ñ±½Í•ÍÑå•½±½È¡½±½È¤¤ì(€€€ô((€€€ÁÕ‰±¥ŒÍÑ…Ñ¥ŒÍÑÉ¥¹œ•ÑEÕ¥±±5…É­¥¹%¡e…ÕÑ©…EÕ¥±±MÑå±”ÍÑå±”¤(€€€ì(€€€€€€€É•ÑÕÉ¸ÍÑå±”Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹M¡½ÉÑQ¡¥¬€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõM¡½ÉÑQ¡¥¬ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹MÑÉ…¥¡ÑQ¡¥¸€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõMÑÉ…¥¡ÑQ¡¥¸ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹Q¥•€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõ1½¹Q¥•ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹M¡½ÉÑQ¡¥¸€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõM¡½ÉÑQ¡¥¸ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹ÕÉÙ•€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõ1½¹ÕÉÙ•ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹MÑÉ…¥¡Ğ€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõ1½¹MÑÉ…¥¡Ğˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹1½¹]¥‘”€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõ1½¹]¥‘”ˆ°(€€€€€€€€€€€e…ÕÑ©…EÕ¥±±MÑå±”¹M¡½ÉÑ]¥‘”€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõM¡½ÉÑ]¥‘”ˆ°(€€€€€€€€€€€|€ôø€‰íEÕ¥±±5…É­¥¹AÉ•™¥áõMÑ…¹‘…Éˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹”	Õ¥±‘•™…Õ±ÑÁÁ•…É…¹” ¤(€€€ì(€€€€€€€Ù…ÈÍ­¥¸€ô•ÑM­¥¹½±½É½±½È¡e…ÕÑ©…M­¥¹½±½È¹É••¸¤ì(€€€€€€€É•ÑÕÉ¸¹•Ü!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹” (€€€€€€€€€€€!…¥ÉMÑå±•Ì¹•™…Õ±Ñ!…¥ÉMÑå±”°(€€€€€€€€€€€Í­¥¸°(€€€€€€€€€€€!…¥ÉMÑå±•Ì¹•™…Õ±Ñ…¥…±!…¥ÉMÑå±”°(€€€€€€€€€€€½±½È¹	±…¬°(€€€€€€€€€€€•Ñå•½±½É½±½È¡e…ÕÑ©…å•½±½È¹	±…¬¤°(€€€€€€€€€€€Í­¥¸°(€€€€€€€€€€€¹•Ü1¥ÍĞñ5…É­¥¹œø(€€€€€€€€€€€ì(€€€€€€€€€€€€€€€¹•Ü¡•ÑEÕ¥±±5…É­¥¹%¡e…ÕÑ©…EÕ¥±±MÑå±”¹MÑ…¹‘…É¤°¹•Ü1¥ÍĞñ½±½ÈøìÍ­¥¸ô¤°(€€€€€€€€€€€ô°(€€€€€€€€€€€!…¥ÉMÑå±•Ì¹•™…Õ±Ñ!…¥ÉMÑå±”°(€€€€€€€€€€€½±½È¹	±…¬°(€€€€€€€€€€€!…¥ÉMÑå±•Ì¹•™…Õ±Ñ…¥…±!…¥ÉMÑå±”°(€€€€€€€€€€€½±½È¹	±…¬¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹”M…¹¥Ñ¥é•ÁÁ•…É…¹” (€€€€€€€!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹”…ÁÁ•…É…¹”°(€€€€€€€e…ÕÑ©…É•…‘½±½È‘É•…‘½±½È¤(€€€ì(€€€€€€€Ù…ÈÍ­¥¹½±½È€ô•Ñ±½Í•ÍÑM­¥¹Q½¹•½±½È¡…ÁÁ•…É…¹”¹M­¥¹½±½È¤ì(€€€€€€€Ù…È¡…¥É½±½È€ô•ÑÉ•…‘½±½É½±½È¡M…¹¥Ñ¥é•É•…‘½±½È¡‘É•…‘½±½È¤°Í­¥¹½±½È¤ì(€€€€€€€É•ÑÕÉ¸ÁÁ±åEÕ¥±±MÑå±” (€€€€€€€€€€€…ÁÁ•…É…¹”¹±½¹” ¤(€€€€€€€€€€€€€€€€¹]¥Ñ¡M­¥¹½±½È¡Í­¥¹½±½È¤(€€€€€€€€€€€€€€€€¹]¥Ñ¡!…¥É½±½È¡¡…¥É½±½È¤(€€€€€€€€€€€€€€€€¹]¥Ñ¡å•½±½È¡•Ñ±½Í•ÍÑå•½±½É½±½È¡…ÁÁ•…É…¹”¹å•½±½È¤¤°(€€€€€€€€€€€•ÑEÕ¥±±MÑå±”¡…ÁÁ•…É…¹”¤¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œe…ÕÑ©…É•…‘½±½ÈM…¹¥Ñ¥é•É•…‘½±½È¡e…ÕÑ©…É•…‘½±½È‘É•…‘½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸¹Õ´¹%Í•™¥¹•¡‘É•…‘½±½È¤€ü‘É•…‘½±½È€èe…ÕÑ©…É•…‘½±½È¹5…Ñ¡M­¥¸ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒPM…¹¥Ñ¥é•¹Õ´ñPø¡PÙ…±Õ”°P™…±±‰…¬¤İ¡•É”P€èÍÑÉÕĞ°¹Õ´(€€€ì(€€€€€€€É•ÑÕÉ¸¹Õ´¹%Í•™¥¹•¡Ù…±Õ”¤€üÙ…±Õ”€è™…±±‰…¬ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹”ÁÁ±åEÕ¥±±MÑå±”¡!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹”…ÁÁ•…É…¹”°e…ÕÑ©…EÕ¥±±MÑå±”ÍÑå±”¤(€€€ì(€€€€€€€Ù…Èµ…É­¥¹Ì€ô¹•Ü1¥ÍĞñ5…É­¥¹œø ¤ì(€€€€€€€™½É•… €¡Ù…Èµ…É­¥¹œ¥¸…ÁÁ•…É…¹”¹5…É­¥¹Ì¤(€€€€€€€ì(€€€€€€€€€€€¥˜€¡%ÍEÕ¥±±5…É­¥¹œ¡µ…É­¥¹œ¹5…É­¥¹%¤¤(€€€€€€€€€€€€€€€½¹Ñ¥¹Õ”ì((€€€€€€€€€€€µ…É­¥¹Ì¹‘¡¹•Ü5…É­¥¹œ¡µ…É­¥¹œ¤¤ì(€€€€€€€ô((€€€€€€€µ…É­¥¹Ì¹‘¡¹•Ü5…É­¥¹œ¡•ÑEÕ¥±±5…É­¥¹%¡ÍÑå±”¤°¹•Ü1¥ÍĞñ½±½Èøì…ÁÁ•…É…¹”¹!…¥É½±½Èô¤¤ì(€€€€€€€É•ÑÕÉ¸…ÁÁ•…É…¹”¹]¥Ñ¡5…É­¥¹Ì¡µ…É­¥¹Ì¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œe…ÕÑ©…EÕ¥±±MÑå±”•ÑEÕ¥±±MÑå±”¡!Õµ…¹½¥‘¡…É…Ñ•ÉÁÁ•…É…¹”…ÁÁ•…É…¹”¤(€€€ì(€€€€€€€™½É•… €¡Ù…Èµ…É­¥¹œ¥¸…ÁÁ•…É…¹”¹5…É­¥¹Ì¤(€€€€€€€ì(€€€€€€€€€€€¥˜€ …%ÍEÕ¥±±5…É­¥¹œ¡µ…É­¥¹œ¹5…É­¥¹%¤¤(€€€€€€€€€€€€€€€½¹Ñ¥¹Õ”ì((€€€€€€€€€€€™½É•… €¡Ù…ÈÍÑå±”¥¸EÕ¥±±MÑå±•=É‘•È¤(€€€€€€€€€€€ì(€€€€€€€€€€€€€€€¥˜€¡µ…É­¥¹œ¹5…É­¥¹%€ôô•ÑEÕ¥±±5…É­¥¹%¡ÍÑå±”¤¤(€€€€€€€€€€€€€€€€€€€É•ÑÕÉ¸ÍÑå±”ì(€€€€€€€€€€€ô(€€€€€€€ô((€€€€€€€É•ÑÕÉ¸e…ÕÑ©…EÕ¥±±MÑå±”¹MÑ…¹‘…Éì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ‰½½°%ÍEÕ¥±±5…É­¥¹œ¡ÍÑÉ¥¹œµ…É­¥¹%¤(€€€ì(€€€€€€€É•ÑÕÉ¸µ…É­¥¹%¹MÑ…ÉÑÍ]¥Ñ ¡EÕ¥±±5…É­¥¹AÉ•™¥à°MÑÉ¥¹½µÁ…É¥Í½¸¹=É‘¥¹…°¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œe…ÕÑ©…M­¥¹½±½È•Ñ±½Í•ÍÑM­¥¹½±½È¡½±½È½±½È¤(€€€ì(€€€€€€€Ù…È‰•ÍÑ½±½È€ôe…ÕÑ©…M­¥¹½±½È¹É••¸ì(€€€€€€€Ù…È‰•ÍÑ¥ÍÑ…¹”€ô¥¹Ğ¹5…áY…±Õ”ì((€€€€€€€™½É•… €¡Ù…ÈÍ­¥¹½±½È¥¸M­¥¹½±½É=É‘•È¤(€€€€€€€ì(€€€€€€€€€€€Ù…ÈÑ½¹”€ô•ÑM­¥¹½±½É½±½È¡Í­¥¹½±½È¤ì(€€€€€€€€€€€Ù…ÈÉ•€ô½±½È¹I	åÑ”€´Ñ½¹”¹I	åÑ”ì(€€€€€€€€€€€Ù…ÈÉ••¸€ô½±½È¹	åÑ”€´Ñ½¹”¹	åÑ”ì(€€€€€€€€€€€Ù…È‰±Õ”€ô½±½È¹		åÑ”€´Ñ½¹”¹		åÑ”ì(€€€€€€€€€€€Ù…È‘¥ÍÑ…¹”€ôÉ•€¨É•€¬É••¸€¨É••¸€¬‰±Õ”€¨‰±Õ”ì((€€€€€€€€€€€¥˜€¡‘¥ÍÑ…¹”€øô‰•ÍÑ¥ÍÑ…¹”¤(€€€€€€€€€€€€€€€½¹Ñ¥¹Õ”ì((€€€€€€€€€€€‰•ÍÑ¥ÍÑ…¹”€ô‘¥ÍÑ…¹”ì(€€€€€€€€€€€‰•ÍÑ½±½È€ôÍ­¥¹½±½Èì(€€€€€€€ô((€€€€€€€É•ÑÕÉ¸‰•ÍÑ½±½Èì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œe…ÕÑ©…å•½±½È•Ñ±½Í•ÍÑå•½±½È¡½±½È½±½È¤(€€€ì(€€€€€€€Ù…È‰•ÍÑ½±½È€ôe…ÕÑ©…å•½±½È¹	±…¬ì(€€€€€€€Ù…È‰•ÍÑ¥ÍÑ…¹”€ô¥¹Ğ¹5…áY…±Õ”ì((€€€€€€€™½É•… €¡Ù…È•å•½±½È¥¸å•½±½É=É‘•È¤(€€€€€€€ì(€€€€€€€€€€€Ù…ÈÑ½¹”€ô•Ñå•½±½É½±½È¡•å•½±½È¤ì(€€€€€€€€€€€Ù…ÈÉ•€ô½±½È¹I	åÑ”€´Ñ½¹”¹I	åÑ”ì(€€€€€€€€€€€Ù…ÈÉ••¸€ô½±½È¹	åÑ”€´Ñ½¹”¹	åÑ”ì(€€€€€€€€€€€Ù…È‰±Õ”€ô½±½È¹		åÑ”€´Ñ½¹”¹		åÑ”ì(€€€€€€€€€€€Ù…È‘¥ÍÑ…¹”€ôÉ•€¨É•€¬É••¸€¨É••¸€¬‰±Õ”€¨‰±Õ”ì((€€€€€€€€€€€¥˜€¡‘¥ÍÑ…¹”€øô‰•ÍÑ¥ÍÑ…¹”¤(€€€€€€€€€€€€€€€½¹Ñ¥¹Õ”ì((€€€€€€€€€€€‰•ÍÑ¥ÍÑ…¹”€ô‘¥ÍÑ…¹”ì(€€€€€€€€€€€‰•ÍÑ½±½È€ô•å•½±½Èì(€€€€€€€ô((€€€€€€€É•ÑÕÉ¸‰•ÍÑ½±½Èì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œ±…¹AÉ½Ñ½ÑåÁ”¡ÍÑÉ¥¹œÁÉ•™¥à°e…ÕÑ©…•…É5…Ñ•É¥…°µ…Ñ•É¥…°°¥¹ĞÍÑå±”¤(€€€ì(€€€€€€€Ù…ÈÍÕ™™¥à€ô5…Ñ•É¥…±MÕ™™¥à¡µ…Ñ•É¥…°¤ì(€€€€€€€¥˜€¡µ…Ñ•É¥…°€ôôe…ÕÑ©…•…É5…Ñ•É¥…°¹‰½¹ä¤(€€€€€€€€€€€É•ÑÕÉ¸ÍÑå±”€ôô€Ä€üÁÉ•™¥à€è€‰íÁÉ•™¥áõíÍÑå±•ôˆì((€€€€€€€É•ÑÕÉ¸ÍÑå±”€ôô€Ä€ü€‰íÁÉ•™¥áõíÍÕ™™¥áôˆ€è€‰íÁÉ•™¥áõíÍÕ™™¥áõíÍÑå±•ôˆì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œ5…Ñ•É¥…±MÕ™™¥à¡e…ÕÑ©…•…É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸µ…Ñ•É¥…°Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹	É½¹é”€ôø€‰	É½¹é”ˆ°(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹M¥±Ù•È€ôø€‰M¥±Ù•Èˆ°(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹É¥µÍ½¸€ôø€‰É¥µÍ½¸ˆ°(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹	½¹”€ôø€‰	½¹”ˆ°(€€€€€€€€€€€|€ôø€‰‰½¹äˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œ•…É¥ÍÁ±…å9…µ”¡e…ÕÑ©…•…É5…Ñ•É¥…°µ…Ñ•É¥…°°ÍÑÉ¥¹œ¥Ñ•µ9…µ”°¥¹ĞÍÑå±”¤(€€€ì(€€€€€€€É•ÑÕÉ¸€‰µÔµå…ÕÑ©„µÁÉ½™¥±”µí¥Ñ•µ9…µ•ôµí5…Ñ•É¥…±-•ä¡µ…Ñ•É¥…°¥ôµíÍÑå±•ôˆì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œ5…Ñ•É¥…±-•ä¡e…ÕÑ©…•…É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸µ…Ñ•É¥…°Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹	É½¹é”€ôø€‰‰É½¹é”ˆ°(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹M¥±Ù•È€ôø€‰Í¥±Ù•Èˆ°(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹É¥µÍ½¸€ôø€‰É¥µÍ½¸ˆ°(€€€€€€€€€€€e…ÕÑ©…•…É5…Ñ•É¥…°¹	½¹”€ôø€‰‰½¹”ˆ°(€€€€€€€€€€€|€ôø€‰•‰½¹äˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œ	É…•É5…Ñ•É¥…±-•ä¡e…ÕÑ©…	É…•É5…Ñ•É¥…°µ…Ñ•É¥…°¤(€€€ì(€€€€€€€É•ÑÕÉ¸µ…Ñ•É¥…°Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹I•ÑÉ¼€ôø€‰É•ÑÉ¼ˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹M¥±Ù•È€ôø€‰Í¥±Ù•Èˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹	É½¹é”€ôø€‰‰É½¹é”ˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹É¥µÍ½¸€ôø€‰É¥µÍ½¸ˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹	½¹”€ôø€‰‰½¹”ˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹É…½¸€ôø€‰‘É…½¸ˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹Mİ…µÀ€ôø€‰Íİ…µÀˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹¹™½É•È€ôø€‰•¹™½É•Èˆ°(€€€€€€€€€€€e…ÕÑ©…	É…•É5…Ñ•É¥…°¹½±±•Ñ½È€ôø€‰½±±•Ñ½Èˆ°(€€€€€€€€€€€|€ôø€‰•‰½¹äˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œ1•…å-•ä¡e…ÕÑ©…1•…åM•Ğ±•…ä¤(€€€ì(€€€€€€€É•ÑÕÉ¸±•…äÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…1•…åM•Ğ¹É…½¸€ôø€‰‘É…½¸ˆ°(€€€€€€€€€€€e…ÕÑ©…1•…åM•Ğ¹Mİ…µÀ€ôø€‰Íİ…µÀˆ°(€€€€€€€€€€€e…ÕÑ©…1•…åM•Ğ¹¹™½É•È€ôø€‰•¹™½É•Èˆ°(€€€€€€€€€€€e…ÕÑ©…1•…åM•Ğ¹½±±•Ñ½È€ôø€‰½±±•Ñ½Èˆ°(€€€€€€€€€€€|€ôø€‰¹½¹”ˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œU¹¥ÅÕ•-•ä¡e…ÕÑ©…U¹¥ÅÕ•M•ĞÕ¹¥ÅÕ”¤(€€€ì(€€€€€€€É•ÑÕÉ¸Õ¹¥ÅÕ”Íİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…U¹¥ÅÕ•M•Ğ¹¹Õ‰åÌ€ôø€‰…¹Õ‰åÌˆ°(€€€€€€€€€€€e…ÕÑ©…U¹¥ÅÕ•M•Ğ¹±•½Á…ÑÉ„€ôø€‰±•½Á…ÑÉ„ˆ°(€€€€€€€€€€€e…ÕÑ©…U¹¥ÅÕ•M•Ğ¹A±…Ñ•€ôø€‰Á±…Ñ•ˆ°(€€€€€€€€€€€e…ÕÑ©…U¹¥ÅÕ•M•Ğ¹I½¹¥¸€ôø€‰É½¹¥¸ˆ°(€€€€€€€€€€€|€ôø€‰¹½¹”ˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œM­¥¹½±½É-•ä¡e…ÕÑ©…M­¥¹½±½ÈÍ­¥¹½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸Í­¥¹½±½ÈÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹É••¸€ôø€‰É••¸ˆ°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹AÕÉÁ±”€ôø€‰ÁÕÉÁ±”ˆ°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹	±Õ”€ôø€‰‰±Õ”ˆ°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹I•€ôø€‰É•ˆ°(€€€€€€€€€€€e…ÕÑ©…M­¥¹½±½È¹	±…¬€ôø€‰‰±…¬ˆ°(€€€€€€€€€€€|€ôø€‰Ñ…¸ˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œå•½±½É-•ä¡e…ÕÑ©…å•½±½È•å•½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸•å•½±½ÈÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹½±€ôø€‰½±ˆ°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹µ‰•È€ôø€‰…µ‰•Èˆ°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹½ÁÁ•È€ôø€‰½ÁÁ•Èˆ°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹I•€ôø€‰É•ˆ°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹)…‘”€ôø€‰©…‘”ˆ°(€€€€€€€€€€€e…ÕÑ©…å•½±½È¹M±…Ñ”€ôø€‰Í±…Ñ”ˆ°(€€€€€€€€€€€|€ôø€‰‰±…¬ˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÍÑÉ¥¹œÉ•…‘½±½É-•ä¡e…ÕÑ©…É•…‘½±½È‘É•…‘½±½È¤(€€€ì(€€€€€€€É•ÑÕÉ¸‘É•…‘½±½ÈÍİ¥Ñ (€€€€€€€ì(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹	±…¬€ôø€‰‰±…¬ˆ°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹…É­	É½İ¸€ôø€‰‘…É¬µ‰É½İ¸ˆ°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹	É½İ¸€ôø€‰‰É½İ¸ˆ°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹Õ‰ÕÉ¸€ôø€‰…Õ‰ÕÉ¸ˆ°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹Í €ôø€‰…Í ˆ°(€€€€€€€€€€€e…ÕÑ©…É•…‘½±½È¹	½¹”€ôø€‰‰½¹”ˆ°(€€€€€€€€€€€|€ôø€‰µ…Ñ µÍ­¥¸ˆ°(€€€€€€€ôì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ¥¹Ğ±…µÀ¡¥¹ĞÙ…±Õ”°¥¹Ğµ¥¸°¥¹Ğµ…à¤(€€€ì(€€€€€€€É•ÑÕÉ¸5…Ñ ¹±…µÀ¡Ù…±Õ”°µ¥¸°µ…à¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ½±½È¡‰åÑ”É•°‰åÑ”É••¸°‰åÑ”‰±Õ”¤(€€€ì(€€€€€€€É•ÑÕÉ¸¹•Ü½±½È¡É•°É••¸°‰±Õ”¤ì(€€€ô)ô(