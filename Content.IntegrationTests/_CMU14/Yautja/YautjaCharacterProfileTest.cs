using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._CMU14.Yautja;
using Content.Client._CMU14.Yautja;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Content.Shared._CMU14.Yautja;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaCharacterProfileTest
{
    [Test]
    public void YautjaProfileCopiesWithoutChangingNormalSpecies()
    {
        var yautjaAppearance = new HumanoidCharacterAppearance()
            .WithSkinColor(new Color((byte) 56, (byte) 90, (byte) 48))
            .WithEyeColor(Color.Gold)
            .WithHairColor(new Color((byte) 24, (byte) 18, (byte) 14))
            .WithMarkings(new List<Marking>
            {
                new("CMUYautjaDreadlocksStandard", new List<Color> { new((byte) 24, (byte) 18, (byte) 14) }),
            });

        var yautja = YautjaCharacterProfile.Default
            .WithName("Kainde Amedha")
            .WithAge(420)
            .WithAppearance(yautjaAppearance)
            .WithSkinColor(YautjaSkinColor.Green)
            .WithQuillStyle(YautjaQuillStyle.LongCurved)
            .WithArmor(YautjaGearMaterial.Bronze, 3)
            .WithMask(YautjaGearMaterial.Bone, 12)
            .WithMaskAccessory(2)
            .WithGreaves(YautjaGearMaterial.Silver, 2)
            .WithBracer(YautjaBracerMaterial.Crimson)
            .WithCaster(YautjaBracerMaterial.Silver)
            .WithOwnerRank(YautjaBracerOwnerRank.Elder)
            .WithTranslatorType(YautjaTranslatorType.Combo)
            .WithInvisibilitySound(YautjaInvisibilitySound.Retro)
            .WithUnique(YautjaUniqueSet.Ronin)
            .WithCapeStyle(YautjaCapeStyle.Poncho)
            .WithCapeColor(new Color((byte) 0x2a, (byte) 0x5c, (byte) 0x8a))
            .WithFlavorText("A quiet hunter.");

        var normal = HumanoidCharacterProfile.DefaultWithSpecies("Human")
            .WithName("John Human")
            .WithYautjaProfile(yautja);

        var copied = normal.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(copied.Species, Is.EqualTo("Human"));
            Assert.That(copied.Name, Is.EqualTo("John Human"));
            Assert.That(copied.YautjaProfile.Name, Is.EqualTo("Kainde Amedha"));
            Assert.That(copied.YautjaProfile.Age, Is.EqualTo(420));
            Assert.That(copied.YautjaProfile.Appearance.SkinColor,
                Is.EqualTo(YautjaCharacterProfile.GetSkinColorColor(YautjaSkinColor.Green)));
            Assert.That(copied.YautjaProfile.SkinColor, Is.EqualTo(YautjaSkinColor.Green));
            Assert.That(copied.YautjaProfile.ArmorPrototype, Is.EqualTo("CMUYautjaArmorUniqueRonin"));
            Assert.That(copied.YautjaProfile.MaskPrototype, Is.EqualTo("CMUYautjaMaskUniqueRonin"));
            Assert.That(copied.YautjaProfile.MaskAccessoryPrototype, Is.EqualTo("CMUYautjaMaskAccessory02Bone"));
            Assert.That(copied.YautjaProfile.GreavesPrototype, Is.EqualTo("CMUYautjaGreavesUniqueRonin"));
            Assert.That(copied.YautjaProfile.BracerPrototype, Is.EqualTo("CMUYautjaBracerCrimson"));
            Assert.That(copied.YautjaProfile.CasterPrototype, Is.EqualTo("CMUYautjaPlasmaCasterSilver"));
            Assert.That(copied.YautjaProfile.OwnerRank, Is.EqualTo(YautjaBracerOwnerRank.Elder));
            Assert.That(copied.YautjaProfile.CapePrototype, Is.EqualTo("CMUYautjaCapePoncho"));
            Assert.That(copied.YautjaProfile.CapeColor, Is.EqualTo(new Color((byte) 0x2a, (byte) 0x5c, (byte) 0x8a)));
            Assert.That(copied.YautjaProfile.QuillMarkingId, Is.EqualTo("CMUYautjaDreadlocksLongCurved"));
            Assert.That(copied.YautjaProfile.TranslatorType, Is.EqualTo(YautjaTranslatorType.Combo));
            Assert.That(copied.YautjaProfile.InvisibilitySound, Is.EqualTo(YautjaInvisibilitySound.Retro));
            Assert.That(copied.YautjaProfile.FlavorText, Is.EqualTo("A quiet hunter."));
        });
    }

    [Test]
    public void DefaultYautjaProfileMatchesCmss13PickerDefaults()
    {
        var yautja = YautjaCharacterProfile.Default;

        Assert.Multiple(() =>
        {
            Assert.That(yautja.Name, Is.EqualTo("ÐÐµÐ¸Ð·Ð²ÐµÑÑ‚Ð½Ð¾"));
            Assert.That(yautja.Age, Is.EqualTo(100));
            Assert.That(yautja.QuillStyle, Is.EqualTo(YautjaQuillStyle.Standard));
            Assert.That(yautja.SkinColor, Is.EqualTo(YautjaSkinColor.Green));
            Assert.That(yautja.EyeColor, Is.EqualTo(YautjaEyeColor.Black));
            Assert.That(yautja.TranslatorType, Is.EqualTo(YautjaTranslatorType.Modern));
            Assert.That(yautja.InvisibilitySound, Is.EqualTo(YautjaInvisibilitySound.Modern));
            Assert.That(yautja.Legacy, Is.EqualTo(YautjaLegacySet.None));
            Assert.That(yautja.Unique, Is.EqualTo(YautjaUniqueSet.None));
            Assert.That(yautja.MaskAccessoryStyle, Is.EqualTo(0));
            Assert.That(yautja.CasterMaterial, Is.EqualTo(YautjaBracerMaterial.Ebony));
            Assert.That(yautja.OwnerRank, Is.EqualTo(YautjaBracerOwnerRank.Unblooded));
            Assert.That(yautja.CapeStyle, Is.EqualTo(YautjaCapeStyle.Full));
            Assert.That(yautja.CapePrototype, Is.EqualTo("CMUYautjaCapeFull"));
            Assert.That(yautja.CapeColor, Is.EqualTo(new Color((byte) 0x65, (byte) 0x43, (byte) 0x21)));
            Assert.That(yautja.FlavorText, Is.Empty);
        });
    }

    [Test]
    public void GearDisplayNamesUseCmss13ItemNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(YautjaCharacterProfile.GetArmorStyleDisplayName(YautjaGearMaterial.Bronze, 3),
                Is.EqualTo("cmu-yautja-profile-armor-bronze-3"));
            Assert.That(YautjaCharacterProfile.GetMaskStyleDisplayName(YautjaGearMaterial.Bone, 12),
                Is.EqualTo("cmu-yautja-profile-mask-bone-12"));
            Assert.That(YautjaCharacterProfile.GetGreavesStyleDisplayName(YautjaGearMaterial.Silver, 2),
                Is.EqualTo("cmu-yautja-profile-greaves-silver-2"));
            Assert.That(YautjaCharacterProfile.CapeStyleOrder,
                Is.EqualTo(new[]
                {
                    YautjaCapeStyle.Full,
                    YautjaCapeStyle.Ceremonial,
                    YautjaCapeStyle.Third,
                    YautjaCapeStyle.Half,
                    YautjaCapeStyle.Quarter,
                    YautjaCapeStyle.Poncho,
                    YautjaCapeStyle.Damaged,
                }));
            Assert.That(YautjaCharacterProfile.GetCapeDisplayName(YautjaCapeStyle.Poncho),
                Is.EqualTo("cmu-yautja-profile-cape-poncho"));
            Assert.That(YautjaCharacterProfile.Default.WithCapeStyle(YautjaCapeStyle.Damaged).CapePrototype,
                Is.EqualTo("CMUYautjaCapeDamaged"));
        });
    }

    [Test]
    public void BracerDisplayNamesUseCmss13Materials()
    {
        Assert.Multiple(() =>
        {
            Assert.That(YautjaCharacterProfile.BracerMaterialOrder,
                Is.EqualTo(new[]
                {
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
                }));
            Assert.That(YautjaCharacterProfile.GetBracerDisplayName(YautjaBracerMaterial.Silver),
                Is.EqualTo("cmu-yautja-profile-bracer-silver-clan"));
            Assert.That(YautjaCharacterProfile.Default.WithBracer(YautjaBracerMaterial.Retro).BracerPrototype,
                Is.EqualTo("CMUYautjaBracerRetro"));
            Assert.That(YautjaCharacterProfile.Default.WithBracer(YautjaBracerMaterial.Dragon).BracerPrototype,
                Is.EqualTo("CMUYautjaBracerLegacyDragon"));
            Assert.That(YautjaCharacterProfile.GetBracerDisplayName(YautjaBracerMaterial.Collector),
                Is.EqualTo("cmu-yautja-profile-bracer-collector-legacy"));
            Assert.That(YautjaCharacterProfile.Default.WithLegacy(YautjaLegacySet.Enforcer).BracerPrototype,
                Is.EqualTo("CMUYautjaBracerLegacyEnforcer"));
        });
    }

    [Test]
    public void ColorCustomizationUsesMutedPresetPalettes()
    {
        var skinColor = YautjaCharacterProfile.GetSkinColorColor(YautjaSkinColor.Green);
        var yautja = YautjaCharacterProfile.Default
            .WithSkinColor(YautjaSkinColor.Green)
            .WithEyeColor(YautjaEyeColor.Copper);
        var quills = yautja.Appearance.Markings.Single(marking => marking.MarkingId == yautja.QuillMarkingId);

        Assert.Multiple(() =>
        {
            Assert.That(YautjaCharacterProfile.SkinColorOrder,
                Is.EqualTo(new[]
                {
                    YautjaSkinColor.Green,
                    YautjaSkinColor.Tan,
                    YautjaSkinColor.Purple,
                    YautjaSkinColor.Blue,
                    YautjaSkinColor.Red,
                    YautjaSkinColor.Black,
                }));
            Assert.That(YautjaCharacterProfile.EyeColorOrder,
                Is.EqualTo(new[]
                {
                    YautjaEyeColor.Black,
                    YautjaEyeColor.Gold,
                    YautjaEyeColor.Amber,
                    YautjaEyeColor.Copper,
                    YautjaEyeColor.Red,
                    YautjaEyeColor.Jade,
                    YautjaEyeColor.Slate,
                }));
            Assert.That(yautja.Appearance.SkinColor, Is.EqualTo(skinColor));
            Assert.That(yautja.Appearance.HairColor, Is.EqualTo(skinColor));
            Assert.That(quills.MarkingColors.Single(), Is.EqualTo(skinColor));
            Assert.That(yautja.Appearance.EyeColor,
                Is.EqualTo(YautjaCharacterProfile.GetEyeColorColor(YautjaEyeColor.Copper)));
            Assert.That(YautjaCharacterProfile.Default.WithEyeColor(YautjaEyeColor.Black).Appearance.EyeColor,
                Is.EqualTo(YautjaCharacterProfile.GetEyeColorColor(YautjaEyeColor.Black)));
        });
    }

    [Test]
    public void DreadColorCanFollowSkinOrRemainIndependent()
    {
        var brown = new Color((byte) 78, (byte) 54, (byte) 34);
        var linked = YautjaCharacterProfile.Default
            .WithSkinColor(YautjaSkinColor.Red);
        var fixedColor = linked
            .WithDreadColor(YautjaDreadColor.Brown)
            .WithSkinColor(YautjaSkinColor.Blue)
            .WithQuillStyle(YautjaQuillStyle.LongTied);
        var copied = fixedColor.Clone();
        var fixedQuills = fixedColor.Appearance.Markings.Single(marking =>
            marking.MarkingId == "CMUYautjaDreadlocksLongTied");

        Assert.Multiple(() =>
        {
            Assert.That(YautjaCharacterProfile.Default.DreadColor, Is.EqualTo(YautjaDreadColor.MatchSkin));
            Assert.That(linked.Appearance.HairColor,
                Is.EqualTo(new Color((byte) 105, (byte) 57, (byte) 59)));
            Assert.That(fixedColor.DreadColor, Is.EqualTo(YautjaDreadColor.Brown));
            Assert.That(fixedColor.Appearance.HairColor, Is.EqualTo(brown));
            Assert.That(fixedQuills.MarkingColors.Single(), Is.EqualTo(brown));
            Assert.That(copied.DreadColor, Is.EqualTo(YautjaDreadColor.Brown));
            Assert.That(copied.Appearance.HairColor, Is.EqualTo(brown));
        });
    }

    [Test]
    public void EmptyMaskAccessoryDisplayNameFitsVisualSelector()
    {
        Assert.That(YautjaCharacterProfile.GetMaskAccessoryDisplayName(0, YautjaGearMaterial.Ebony),
            Is.EqualTo("cmu-yautja-profile-mask-accessory-none"));
    }

    [Test]
    public async Task MaskAccessoryHasMatchingOnMobSpriteState()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var cache = client.ResolveDependency<IResourceCache>();
            var prototypes = client.ResolveDependency<IPrototypeManager>();
            var factory = client.EntMan.ComponentFactory;
            var accessory = prototypes.Index<EntityPrototype>("CMUYautjaMaskAccessory02Bronze");

            Assert.That(accessory.TryGetComponent<SpriteComponent>(out var sprite, factory), Is.True);
            var state = sprite!.AllLayers.First().RsiState.Name;
            var rsiPath = new ResPath("/Textures/_CMU14/Yautja/mask_accessories_onmob.rsi");

            Assert.Multiple(() =>
            {
                Assert.That(state, Is.EqualTo("pred_accessory2_bronze"));
                Assert.That(cache.TryGetResource<RSIResource>(rsiPath, out var resource), Is.True);
                Assert.That(resource!.RSI.Size, Is.EqualTo(new Vector2i(32, 64)),
                    "CMSS13 mask accessories are 32x64 on-mob overlays; shrinking them to 32x32 moves the preview layer off the helmet.");
                Assert.That(resource!.RSI.TryGetState($"equipped-{state}", out _), Is.True,
                    "The client visual system maps mask accessory icon states to equipped-* on-mob states.");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task YautjaMaskAccessoryPrototypesMatchCmss13SourceFacts()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var cache = client.ResolveDependency<IResourceCache>();
            var prototypes = client.ResolveDependency<IPrototypeManager>();
            var factory = client.EntMan.ComponentFactory;
            var onMobRsiPath = new ResPath("/Textures/_CMU14/Yautja/mask_accessories_onmob.rsi");

            Assert.That(cache.TryGetResource<RSIResource>(onMobRsiPath, out var onMobResource), Is.True);
            Assert.That(onMobResource!.RSI.Size, Is.EqualTo(new Vector2i(32, 64)),
                "CMSS13 mask accessories use a separate on-mob accessory DMI for WEAR_FACE overlays.");

            var basePrototype = prototypes.Index<EntityPrototype>("CMUYautjaMaskOrnament");
            Asseã}´¶‰žËkºwµç@€€€€€…¹UÍ•1•…äèÑÉÕ”°(€€€€€€€€€€€…¹UÍ•½Õ¹¥±MÑ…ÑÕÌèÑÉÕ”°(€€€€€€€€€€€…¹UÍ•1•…‘•ÉMÑ…ÑÕÌèÑÉÕ”¤ì(€€€€€€€Ù…ÈÁÉ½™¥±”€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡MÑ…ÑÕÌ¡e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌ¹9½Éµ…°¤(€€€€€€€€€€€€¹]¥Ñ¡U¹¥ÅÕ”¡e…ÕÑ©…U¹¥ÅÕ•M•Ð¹¹Õ‰åÌ¤(€€€€€€€€€€€€¹]¥Ñ¡1•…ä¡e…ÕÑ©…1•…åM•Ð¹9½¹”¤(€€€€€€€€€€€€¹]¥Ñ¡…Á•MÑå±”¡e…ÕÑ©……Á•MÑå±”¹•É•µ½¹¥…°¤(€€€€€€€€€€€€¹]¥Ñ¡	É…•È¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹	½¹”¤ì((€€€€€€€Ù…ÈÍ…¹¥Ñ¥é•€ôÁÉ½™¥±”¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡…Á…‰¥±¥Ñ¥•Ì¤ì((€€€€€€€ÍÍ•ÉÐ¹5Õ±Ñ¥Á±”  ¤€ôø(€€€€€€€ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í…¹¥Ñ¥é•¹MÑ…ÑÕÌ°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌ¹9½Éµ…°¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í…¹¥Ñ¥é•¹±…¹I…¹¬°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…I…¹¬¹	±½½‘•¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í…¹¥Ñ¥é•¹U¹¥ÅÕ”°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…U¹¥ÅÕ•M•Ð¹¹Õ‰åÌ¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í…¹¥Ñ¥é•¹…Á•MÑå±”°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©……Á•MÑå±”¹•É•µ½¹¥…°¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Í…¹¥Ñ¥é•¹	É…•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹	½¹”¤¤ì(€€€€€€€ô¤ì(€€€ô((€€€€mQ•ÍÑt(€€€€ÁÕ‰±¥ŒÙ½¥AÉ½™¥±•M…¹¥Ñ¥é•É¹™½É•ÍÅÕ¥Áµ•¹Ñ•ÍÍA½±¥ä ¤(€€€mQ•ÍÑt(€€€ÁÕ‰±¥ŒÙ½¥AÉ½™¥±•M…¹¥Ñ¥é•É¹™½É•ÍÅÕ¥Áµ•¹Ñ•ÍÍA½±¥ä ¤(€€€ì(€€€€€€€Ù…È½É‘¥¹…Éå…Á…‰¥±¥Ñ¥•Ì€ô¹•Üe…ÕÑ©…AÉ½™¥±•…Á…‰¥±¥Ñ¥•Ì¡e…ÕÑ©…I…¹¬¹	±½½‘•°™…±Í”°™…±Í”¤ì(€€€€€€€Ù…È•±¥Ñ•…Á…‰¥±¥Ñ¥•Ì€ô¹•Üe…ÕÑ©…AÉ½™¥±•…Á…‰¥±¥Ñ¥•Ì¡e…ÕÑ©…I…¹¬¹±¥Ñ”°ÑÉÕ”°™…±Í”¤ì(€€€€€€€Ù…È±•…‘•É…Á…‰¥±¥Ñ¥•Ì€ô¹•Üe…ÕÑ©…AÉ½™¥±•…Á…‰¥±¥Ñ¥•Ì¡e…ÕÑ©…I…¹¬¹1•…‘•È°ÑÉÕ”°™…±Í”¤ì(€€€€€€€Ù…È±•…å…Á…‰¥±¥Ñ¥•Ì€ô¹•Üe…ÕÑ©…AÉ½™¥±•…Á…‰¥±¥Ñ¥•Ì¡e…ÕÑ©…I…¹¬¹	±½½‘•°™…±Í”°ÑÉÕ”¤ì((€€€€€€€Ù…È½É‘¥¹…Éä€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡…Á•MÑå±”¡e…ÕÑ©……Á•MÑå±”¹•É•µ½¹¥…°¤(€€€€€€€€€€€€¹]¥Ñ¡	É…•È¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹	É½¹é”¤(€€€€€€€€€€€€¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡½É‘¥¹…Éå…Á…‰¥±¥Ñ¥•Ì¤ì(€€€€€€€Ù…ÈÕ¹…ÕÑ¡½É¥é•‘1•…å	É…•È€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡	É…•È¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹É…½¸¤(€€€€€€€€€€€€¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡½É‘¥¹…Éå…Á…‰¥±¥Ñ¥•Ì¤ì(€€€€€€€Ù…È•±¥Ñ”€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡	É…•È¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹É¥µÍ½¸¤(€€€€€€€€€€€€¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡•±¥Ñ•…Á…‰¥±¥Ñ¥•Ì¤ì(€€€€€€€Ù…È±•…‘•È€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡…Á•MÑå±”¡e…ÕÑ©……Á•MÑå±”¹•É•µ½¹¥…°¤(€€€€€€€€€€€€¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡±•…‘•É…Á…‰¥±¥Ñ¥•Ì¤ì(€€€€€€€Ù…È±•…ä€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡1•…ä¡e…ÕÑ©…1•…åM•Ð¹½±±•Ñ½È¤(€€€€€€€€€€€€¹]¥Ñ¡	É…•È¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹¹™½É•È¤(€€€€€€€€€€€€¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡±•…å…Á…‰¥±¥Ñ¥•Ì¤ì((€€€€€€€ÍÍ•ÉÐ¹5Õ±Ñ¥Á±”  ¤€ôø(€€€€€€€ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½É‘¥¹…Éä¹…Á•MÑå±”°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©……Á•MÑå±”¹Õ±°¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½É‘¥¹…Éä¹	É…•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡Õ¹…ÕÑ¡½É¥é•‘1•…å	É…•È¹	É…•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•±¥Ñ”¹	É…•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹É¥µÍ½¸¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡±•…‘•È¹…Á•MÑå±”°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©……Á•MÑå±”¹•É•µ½¹¥…°¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡±•…ä¹1•…ä°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…1•…åM•Ð¹½±±•Ñ½È¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡±•…ä¹	É…•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹¹™½É•È¤¤ì(€€€€€€€ô¤ì(€€€ô((€€€mQ•ÍÑt(€€€ÁÕ‰±¥ŒÙ½¥AÉ½™¥±•M…¹¥Ñ¥é•É9½Éµ…±¥é•ÍU¹‘•™¥¹•‘ÅÕ¥Áµ•¹ÑY…±Õ•Ì ¤(€€€ì(€€€€€€€Ù…È…Á…‰¥±¥Ñ¥•Ì€ô¹•Üe…ÕÑ©…AÉ½™¥±•…Á…‰¥±¥Ñ¥•Ì (€€€€€€€€€€€e…ÕÑ©…I…¹¬¹¹¥•¹Ð°(€€€€€€€€€€€ÑÉÕ”°(€€€€€€€€€€€ÑÉÕ”°(€€€€€€€€€€€…¹UÍ•½Õ¹¥±MÑ…ÑÕÌèÑÉÕ”°(€€€€€€€€€€€…¹UÍ•1•…‘•ÉMÑ…ÑÕÌèÑÉÕ”¤ì((€€€€€€€Ù…È¥¹Ù…±¥€ôe…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð(€€€€€€€€€€€€¹]¥Ñ¡Éµ½È ¡e…ÕÑ©…•…É5…Ñ•É¥…°¤‰åÑ”¹5…áY…±Õ”°¥¹Ð¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡5…Í¬ ¡e…ÕÑ©…•…É5…Ñ•É¥…°¤‰åÑ”¹5…áY…±Õ”°¥¹Ð¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡É•…Ù•Ì ¡e…ÕÑ©…•…É5…Ñ•É¥…°¤‰åÑ”¹5…áY…±Õ”°¥¹Ð¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡	É…•È ¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¤‰åÑ”¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡…ÍÑ•È ¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¤‰åÑ”¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡…Á•MÑå±” ¡e…ÕÑ©……Á•MÑå±”¤‰åÑ”¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡1•…ä ¡e…ÕÑ©…1•…åM•Ð¤‰åÑ”¹5…áY…±Õ”¤(€€€€€€€€€€€€¹]¥Ñ¡U¹¥ÅÕ” ¡e…ÕÑ©…U¹¥ÅÕ•M•Ð¤‰åÑ”¹5…áY…±Õ”¤(€€€€€€€€€€€€¹M…¹¥Ñ¥é•½É…Á…‰¥±¥Ñ¥•Ì¡…Á…‰¥±¥Ñ¥•Ì¤ì((€€€€€€€ÍÍ•ÉÐ¹5Õ±Ñ¥Á±”  ¤€ôø(€€€€€€€ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹Éµ½É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…•…É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹5…Í­5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…•…É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹É•…Ù•Í5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…•…É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹	É…•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹…ÍÑ•É5…Ñ•É¥…°°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…	É…•É5…Ñ•É¥…°¹‰½¹ä¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹…Á•MÑå±”°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©……Á•MÑå±”¹Õ±°¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹1•…ä°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…1•…åM•Ð¹9½¹”¤¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡¥¹Ù…±¥¹U¹¥ÅÕ”°%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…U¹¥ÅÕ•M•Ð¹9½¹”¤¤ì(€€€€€€€ô¤ì(€€€ô((€€€mQ•ÍÑt(€€€ÁÕ‰±¥Œ…Íå¹ŒQ…Í¬ÁÁ±¥•‘AÉ½™¥±•UÍ•Í™™•Ñ¥Ù•M•±•Ñ•‘MÑ…ÑÕÍ½É¹Ñ¥ÑåI…¹¬ ¤(€€€ì(€€€€€€€…Ý…¥ÐÕÍ¥¹œÙ…ÈÁ…¥È€ô…Ý…¥ÐA½½±5…¹…•È¹•ÑM•ÉÙ•É±¥•¹Ð ¤ì(€€€€€€€Ù…ÈÍ•ÉÙ•È€ôÁ…¥È¹M•ÉÙ•Èì(€€€€€€€Ù…Èµ…À€ô…Ý…¥ÐÁ…¥È¹É•…Ñ•Q•ÍÑ5…À ¤ì((€€€€€€€…Ý…¥ÐÍ•ÉÙ•È¹]…¥ÑÍÍ•ÉÑ¥½¸  ¤€ôø(€€€€€€€ì(€€€€€€€€€€€Ù…È•¹Ñ5…¸€ôÍ•ÉÙ•È¹¹Ñ5…¸ì(€€€€€€€€€€€Ù…ÈÁÉ½™¥±•ÁÁ±ä€ô•¹Ñ5…¸¹MåÍÑ•´ñe…ÕÑ©…AÉ½™¥±•ÁÁ±åMåÍÑ•´ø ¤ì(€€€€€€€€€€€Ù…È…Á…‰¥±¥Ñ¥•Ì€ô¹•Üe…ÕÑ©…AÉ½™¥±•…Á…‰¥±¥Ñ¥•Ì (€€€€€€€€€€€€€€€e…ÕÑ©…I…¹¬¹¹¥•¹Ð°(€€€€€€€€€€€€€€€ÑÉÕ”°(€€€€€€€€€€€€€€€™…±Í”°(€€€€€€€€€€€€€€€…¹UÍ•½Õ¹¥±MÑ…ÑÕÌèÑÉÕ”°(€€€€€€€€€€€€€€€…¹UÍ•1•…‘•ÉMÑ…ÑÕÌèÑÉÕ”¤ì(€€€€€€€€€€€Ù…È¹½Éµ…°€ô•¹Ñ5…¸¹MÁ…Ý¹¹Ñ¥Ñä ‰5U5½‰e…ÕÑ©„ˆ°µ…À¹É¥‘½½É‘Ì¤ì(€€€€€€€€€€€Ù…È½Õ¹¥°€ô•¹Ñ5…¸¹MÁ…Ý¹¹Ñ¥Ñä ‰5U5½‰e…ÕÑ©„ˆ°µ…À¹É¥‘½½É‘Ì¤ì((€€€€€€€€€€€ÑÉä(€€€€€€€€€€€ì(€€€€€€€€€€€€€€€ÁÉ½™¥±•ÁÁ±ä¹ÁÁ±åAÉ½™¥±” (€€€€€€€€€€€€€€€€€€€¹½Éµ…°°(€€€€€€€€€€€€€€€€€€€e…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð¹]¥Ñ¡MÑ…ÑÕÌ¡e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌ¹9½Éµ…°¤°(€€€€€€€€€€€€€€€€€€€…ÕÑ¡½É¥Ñ…Ñ¥Ù•…Á…‰¥±¥Ñ¥•Ìè…Á…‰¥±¥Ñ¥•Ì¤ì(€€€€€€€€€€€€€€€ÁÉ½™¥±•ÁÁ±ä¹ÁÁ±åAÉ½™¥±” (€€€€€€€€€€€€€€€€€€€½Õ¹¥°°(€€€€€€€€€€€€€€€€€€€e…ÕÑ©…¡…É…Ñ•ÉAÉ½™¥±”¹•™…Õ±Ð¹]¥Ñ¡MÑ…ÑÕÌ¡e…ÕÑ©…AÉ½™¥±•MÑ…ÑÕÌ¹½Õ¹¥°¤°(€€€€€€€€€€€€€€€€€€€…ÕÑ¡½É¥Ñ…Ñ¥Ù•…Á…‰¥±¥Ñ¥•Ìè…Á…‰¥±¥Ñ¥•Ì¤ì((€€€€€€€€€€€€€€€ÍÍ•ÉÐ¹5Õ±Ñ¥Á±”  ¤€ôø(€€€€€€€€€€€€€€€ì(€€€€€€€€€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð (€€€€€€€€€€€€€€€€€€€€€€€•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñe…ÕÑ©…½µÁ½¹•¹Ðø¡¹½Éµ…°¤¹±…¹I…¹¬°(€€€€€€€€€€€€€€€€€€€€€€€%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…I…¹¬¹	±½½‘•¤¤ì(€€€€€€€€€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð (€€€€€€€€€€€€€€€€€€€€€€€•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñe…ÕÑ©…½µÁ½¹•¹Ðø¡½Õ¹¥°¤¹±…¹I…¹¬°(€€€€€€€€€€€€€€€€€€€€€€€%Ì¹ÅÕ…±Q¼¡e…ÕÑ©…I…¹¬¹¹¥•¹Ð¤¤ì(€€€€€€€€€€€€€€€ô¤ì(€€€€€€€€€€€ô(€€€€€€€€€€€™¥¹…±±ä(€€€€€€€€€€€ì(€€€€€€€€€€€€€€€•¹Ñ5…¸¹•±•Ñ•¹Ñ¥Ñä¡¹½Éµ…°¤ì(€€€€€€€€€€€€€€€•¹Ñ5…¸¹•±•Ñ•¹Ñ¥Ñä¡½Õ¹¥°¤ì(€€€€€€€€€€€ô(€€€€€€€ô¤ì((€€€€€€€…Ý…¥ÐÁ…¥È¹±•…¹I•ÑÕÉ¹Íå¹Œ ¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ%¹Õµ•É…‰±”ñ5…Í­•ÍÍ½ÉåI½Üø5…Í­•ÍÍ½ÉåI½ÝÌ ¤(€€€ì(€€€€€€€™½É•… €¡Ù…Èµ…Ñ•É¥…°¥¸¹•Ýmtì€‰‰½¹äˆ°€‰	É½¹é”ˆ°€‰M¥±Ù•Èˆ°€‰É¥µÍ½¸ˆ°€‰	½¹”ˆô¤(€€€€€€€ì(€€€€€€€€€€€Ù…ÈÍÑ…Ñ•5…Ñ•É¥…°€ôµ…Ñ•É¥…°¹Q½1½Ý•É%¹Ù…É¥…¹Ð ¤ì(€€€€€€€€€€€™½È€¡Ù…ÈÍÑå±”€ô€ÄìÍÑå±”€ðô€ÌìÍÑå±”¬¬¤(€€€€€€€€€€€€€€€å¥•±É•ÑÕÉ¸¹•Ü5…Í­•ÍÍ½ÉåI½Ü ‰5Ue…ÕÑ©…5…Í­•ÍÍ½ÉåíÍÑå±”èÀÁõíµ…Ñ•É¥…±ôˆ°€‰ÁÉ•‘}…•ÍÍ½ÉåíÍÑå±•õ}íÍÑ…Ñ•5…Ñ•É¥…±ôˆ¤ì(€€€€€€€ô(€€€ô((€€€ÁÉ¥Ù…Ñ”Í•…±•É•½É5…Í­•ÍÍ½ÉåI½Ü¡ÍÑÉ¥¹œ%°ÍÑÉ¥¹œMÑ…Ñ”¤ì((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥ÍÍ•ÉÑAÉ½™¥±•5…Í­MÑ…Ñ¥…ÑÌ¡¹Ñ¥Ñå5…¹…•È•¹Ñ5…¸°¹Ñ¥ÑåU¥Õ¥°AÉ½™¥±•5…Í­I½ÜÉ½Ü¤(€€€ì(€€€€€€€Ù…Èµ•Ñ„€ô•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñ5•Ñ……Ñ…½µÁ½¹•¹Ðø¡Õ¥¤ì(€€€€€€€Ù…È±½Ñ¡¥¹œ€ô•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñ±½Ñ¡¥¹½µÁ½¹•¹Ðø¡Õ¥¤ì(€€€€€€€Ù…È…Éµ½È€ô•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñ5Éµ½É½µÁ½¹•¹Ðø¡Õ¥¤ì((€€€€€€€ÍÍ•ÉÐ¹5Õ±Ñ¥Á±”  ¤€ôø(€€€€€€€ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ„¹¹Ñ¥Ñå9…µ”°%Ì¹ÅÕ…±Q¼¡É½Ü¹9…µ”¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ„¹¹Ñ¥Ñå•ÍÉ¥ÁÑ¥½¸°%Ì¹ÅÕ…±Q¼¡É½Ü¹•ÍÉ¥ÁÑ¥½¸¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡±½Ñ¡¥¹œ¹M±½ÑÌ°%Ì¹ÅÕ…±Q¼¡M±½Ñ±…Ì¹5M,ðM±½Ñ±…Ì¹MU%QMQ=I¤°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ôµ…ÁÌÍ½ÕÉ”]I}…¹±½…°ÍÕ¥ÐµÍÑ½É…”ÁÉ½™¥±”É•Á±…•µ•¹ÐÍ±½Ð¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹!…Í½µÁ½¹•¹Ðñe…ÕÑ©…5…Í­½µÁ½¹•¹Ðø¡Õ¥¤°%Ì¹QÉÕ”°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ô­••ÁÌÑ¡”™Õ¹Ñ¥½¹…°5MLÄÌe…ÕÑ©„µ…Í¬‰•¡…Ù¥½ÈÍÕÉ™…”¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹!…Í½µÁ½¹•¹Ðñe…ÕÑ©…5…Í­•ÍÍ½Éå!½±‘•É½µÁ½¹•¹Ðø¡Õ¥¤°%Ì¹QÉÕ”°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ô¥¹¡•É¥ÑÌ5MLÄÌÙ…±¥‘}…•ÍÍ½Éå}Í±½ÑÌ€ôMM=Ie}M1=Q}eUQ)}5M,¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹!…Í½µÁ½¹•¹Ðñe…ÕÑ©…Q•¡%Ñ•µ½µÁ½¹•¹Ðø¡Õ¥¤°%Ì¹QÉÕ”°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ôµ…ÁÌÍ½ÕÉ”%Q5}AIQ=H¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹QÉå•Ñ½µÁ½¹•¹Ðñ½ÉÉ½‘¥‰±•½µÁ½¹•¹Ðø¡Õ¥°½ÕÐÙ…È½ÉÉ½‘¥‰±”¤°%Ì¹QÉÕ”°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ôµ…ÁÌÍ½ÕÉ”Õ¹…¥‘…‰±”¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½ÉÉ½‘¥‰±”„¹%Í½ÉÉ½‘¥‰±”°%Ì¹…±Í”°€‰íÉ½Ü¹%‘ôµ…ÁÌÍ½ÕÉ”Õ¹…¥‘…‰±”¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹5•±•”°%Ì¹ÅÕ…±Q¼ ÐÀ¤°€‰íÉ½Ü¹%‘ôµ…ÁÌ¡Õ¹Ñ•Èµ…Í¬…Éµ½É}µ•±•”€ô1=Q!%9}I5=I}5%U4¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹	Õ±±•Ð°%Ì¹ÅÕ…±Q¼ ÔÀ¤°€‰íÉ½Ü¹%‘ôµ…ÁÌ¡Õ¹Ñ•Èµ…Í¬…Éµ½É}‰Õ±±•Ð€ô1=Q!%9}I5=I}!% ¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹	¥¼°%Ì¹ÅÕ…±Q¼ ÐÔ¤°€‰íÉ½Ü¹%‘ôµ…ÁÌ¡Õ¹Ñ•Èµ…Í¬…Éµ½É}‰¥¼€ô1=Q!%9}I5=I}5%U5!% ¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹áÁ±½Í¥½¹Éµ½È°%Ì¹ÅÕ…±Q¼ ÔÀ¤°€‰íÉ½Ü¹%‘ôµ…ÁÌ¡Õ¹Ñ•Èµ…Í¬…Éµ½É}‰½µˆ€ô1=Q!%9}I5=I}!% ¸ˆ¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹•Ñ½µÁ½¹•¹ÐñA…É…Í¥Ñ•I•Í¥ÍÑ…¹•½µÁ½¹•¹Ðø¡Õ¥¤¹5…á½Õ¹Ð°%Ì¹ÅÕ…±Q¼ ÄÀÀ¤°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ô¥¹¡•É¥ÑÌ5MLÄÌ¡Õ¹Ñ•È…¹Ñ¥}¡Õœ€ô€ÄÀÀ¸ˆ¤ì(€€€€€€€ô¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒÙ½¥ÍÍ•ÉÑMÁ•¥…±5…Í­MÑ…Ñ¥…ÑÌ¡¹Ñ¥Ñå5…¹…•È•¹Ñ5…¸°¹Ñ¥ÑåU¥Õ¥°MÁ•¥…±5…Í­I½ÜÉ½Ü¤(€€€ì(€€€€€€€Ù…Èµ•Ñ„€ô•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñ5•Ñ……Ñ…½µÁ½¹•¹Ðø¡Õ¥¤ì(€€€€€€€Ù…È±½Ñ¡¥¹œ€ô•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñ±½Ñ¡¥¹½µÁ½¹•¹Ðø¡Õ¥¤ì(€€€€€€€Ù…È…Éµ½È€ô•¹Ñ5…¸¹•Ñ½µÁ½¹•¹Ðñ5Éµ½É½µÁ½¹•¹Ðø¡Õ¥¤ì((€€€€€€€ÍÍ•ÉÐ¹5Õ±Ñ¥Á±”  ¤€ôø(€€€€€€€ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ„¹¹Ñ¥Ñå9…µ”°%Ì¹ÅÕ…±Q¼¡É½Ü¹9…µ”¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡µ•Ñ„¹¹Ñ¥Ñå•ÍÉ¥ÁÑ¥½¸°%Ì¹ÅÕ…±Q¼¡É½Ü¹•ÍÉ¥ÁÑ¥½¸¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡±½Ñ¡¥¹œ¹M±½ÑÌ°%Ì¹ÅÕ…±Q¼¡M±½Ñ±…Ì¹5M,ðM±½Ñ±…Ì¹MU%QMQ=I¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹!…Í½µÁ½¹•¹Ðñe…ÕÑ©…5…Í­½µÁ½¹•¹Ðø¡Õ¥¤°%Ì¹QÉÕ”°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹!…Í½µÁ½¹•¹Ðñe…ÕÑ©…5…Í­•ÍÍ½Éå!½±‘•É½µÁ½¹•¹Ðø¡Õ¥¤°%Ì¹QÉÕ”°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹!…Í½µÁ½¹•¹Ðñe…ÕÑ©…Q•¡%Ñ•µ½µÁ½¹•¹Ðø¡Õ¥¤°%Ì¹QÉÕ”°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹QÉå•Ñ½µÁ½¹•¹Ðñ½ÉÉ½‘¥‰±•½µÁ½¹•¹Ðø¡Õ¥°½ÕÐÙ…È½ÉÉ½‘¥‰±”¤°%Ì¹QÉÕ”°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡½ÉÉ½‘¥‰±”„¹%Í½ÉÉ½‘¥‰±”°%Ì¹…±Í”°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹5•±•”°%Ì¹ÅÕ…±Q¼¡É½Ü¹5•±•”¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹	Õ±±•Ð°%Ì¹ÅÕ…±Q¼¡É½Ü¹	Õ±±•Ð¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹	¥¼°%Ì¹ÅÕ…±Q¼¡É½Ü¹	¥¼¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡…Éµ½È¹áÁ±½Í¥½¹Éµ½È°%Ì¹ÅÕ…±Q¼¡É½Ü¹áÁ±½Í¥½¸¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹•Ñ½µÁ½¹•¹ÐñA…É…Í¥Ñ•I•Í¥ÍÑ…¹•½µÁ½¹•¹Ðø¡Õ¥¤¹5…á½Õ¹Ð°%Ì¹ÅÕ…±Q¼¡É½Ü¹¹Ñ¥!Õœ¤°É½Ü¹%¤ì(€€€€€€€€€€€ÍÍ•ÉÐ¹Q¡…Ð¡•¹Ñ5…¸¹•Ñ½µÁ½¹•¹ÐñI5%µµÕ¹•Q½%¹¥Ñ¥½¹½µÁ½¹•¹Ðø¡Õ¥¤¹%¹Ñ•¹Í¥ÑåI•Í¥ÍÑ…¹”°%Ì¹ÅÕ…±Q¼ ÄÀ¤°(€€€€€€€€€€€€€€€€‰íÉ½Ü¹%‘ôµ…ÁÌ5MLÄÌ™¥É•}¥¹Ñ•¹Í¥Ñå}É•Í¥ÍÑ…¹”€ô€ÄÀ¸ˆ¤ì(€€€€€€€ô¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ%¹Õµ•É…‰±”ñAÉ½™¥±•5…Í­I½ÜøAÉ½™¥±•5…Í­I½ÝÌ ¤(€€€ì(€€€€€€€™½É•… €¡Ù…Èµ…Ñ•É¥…°¥¸¹•Ýmtì€‰	½¹”ˆ°€‰	É½¹é”ˆ°€‰É¥µÍ½¸ˆ°€‰‰½¹äˆ°€‰M¥±Ù•Èˆô¤(€€€€€€€ì(€€€€€€€€€€€Ù…ÈÍÑ…Ñ•5…Ñ•É¥…°€ôµ…Ñ•É¥…°¹Q½1½Ý•É%¹Ù…É¥…¹Ð ¤ì(€€€€€€€€€€€™½È€¡Ù…ÈÍÑå±”€ô€ÄìÍÑå±”€ðô€ÈÀìÍÑå±”¬¬¤(€€€€€€€€€€€ì(€€€€€€€€€€€€€€€Ù…ÈÍÑ…Ñ”€ô€‰ÁÉ•‘}µ…Í­íÍÑå±•õ}íÍÑ…Ñ•5…Ñ•É¥…±ôˆì(€€€€€€€€€€€€€€€å¥•±É•ÑÕÉ¸¹•ÜAÉ½™¥±•5…Í­I½Ü (€€€€€€€€€€€€€€€€€€€€‰5Ue…ÕÑ©…5…Í­AÉ•‘íÍÑå±”èÀÁõíµ…Ñ•É¥…±ôˆ°(€€€€€€€€€€€€€€€€€€€€‰±…¸µ…Í¬ˆ°(€€€€€€€€€€€€€€€€€€€€‰‰•…ÕÑ¥™Õ±±ä‘•Í¥¹•µ•Ñ…±±¥Œ™…”µ…Í¬°‰½Ñ ½É¹…Ñ”…¹™Õ¹Ñ¥½¹…°¸ˆ°(€€€€€€€€€€€€€€€€€€€ÍÑ…Ñ”¤ì(€€€€€€€€€€€ô(€€€€€€€ô(€€€ô((€€€ÁÉ¥Ù…Ñ”Í•…±•É•½ÉAÉ½™¥±•5…Í­I½Ü¡ÍÑÉ¥¹œ%°ÍÑÉ¥¹œ9…µ”°ÍÑÉ¥¹œ•ÍÉ¥ÁÑ¥½¸°ÍÑÉ¥¹œMÑ…Ñ”¤ì((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥Œ%¹Õµ•É…‰±”ñMÁ•¥…±5…Í­I½ÜøMÁ•¥…±5…Í­I½ÝÌ ¤(€€€ì(€€€€€€€½¹ÍÐÍÑÉ¥¹œ¡Õ¹Ñ•É9…µ”€ô€‰±…¸µ…Í¬ˆì(€€€€€€€½¹ÍÐÍÑÉ¥¹œ¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸€ô€‰‰•…ÕÑ¥™Õ±±ä‘•Í¥¹•µ•Ñ…±±¥Œ™…”µ…Í¬°‰½Ñ ½É¹…Ñ”…¹™Õ¹Ñ¥½¹…°¸ˆì(€€€€€€€½¹ÍÐÍÑÉ¥¹œ…¹¥•¹Ñ9…µ”€ô€‰½É¹…Ñ”…¹¥•¹Ð…±¥•¸µ…Í¬ˆì(€€€€€€€½¹ÍÐÍÑÉ¥¹œ…¹¥•¹Ñ•ÍÉ¥ÁÑ¥½¸€ô€‰¸½É¹…Ñ”…¹¥•¹Ð™…•Á±…Ñ”½˜…¸…•…±±½ä°½¹”Ý½É¸‰ä„É•Ù•É•¡Õ¹Ñ•È¸Q¡½Õ Ñ…É¹¥Í¡•‰äÑ¥µ”°¥ÑÌÉ…™ÑÍµ…¹Í¡¥ÀÉ•µ…¥¹Ì•áÅÕ¥Í¥Ñ”€´„™ÕÍ¥½¸½˜…ÉÑ¥ÍÑÉä…¹‘•…‘±ä™Õ¹Ñ¥½¸¸ˆì(€€€€€€€½¹ÍÐÍÑÉ¥¹œÑ¡É…±±9…µ”€ô€‰…±¥•¸µ…Í¬ˆì(€€€€€€€½¹ÍÐÍÑÉ¥¹œÑ¡É…±±•ÍÉ¥ÁÑ¥½¸€ô€‰Í¥µÁ±¥ÍÑ¥Œµ•Ñ…±±¥Œ™…”µ…Í¬Ý¥Ñ …‘Ù…¹•…Á…‰¥±¥Ñ¥•Ì¸ˆì((€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­¹¥•¹Ðˆ°…¹¥•¹Ñ9…µ”°…¹¥•¹Ñ•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}…¹¥•¹Ðˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­¹¥•¹ÑI•‘±½Üˆ°…¹¥•¹Ñ9…µ”°…¹¥•¹Ñ•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}…¹¥•¹Ñ}É•‘±½Üˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­¹¥•¹Ñ]¡¥Ñ”ˆ°…¹¥•¹Ñ9…µ”°…¹¥•¹Ñ•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}…¹¥•¹Ñ}Ý¡¥Ñ”ˆ¤ì((€€€€€€€™½É•… €¡Ù…È±•…ä¥¸¹•Ýmtì€‰½±±•Ñ½Èˆ°€‰É…½¸ˆ°€‰¹™½É•Èˆ°€‰MÝ…µÀˆô¤(€€€€€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­1•…åí±•…åôˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}±•…å}í±•…ä¹Q½1½Ý•É%¹Ù…É¥…¹Ð ¥ôˆ¤ì((€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­±¥Ñ•±•½Á…ÑÉ„ˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}•±¥Ñ•}±•½Á…ÑÉ„ˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­±¥Ñ•A±…Ñ•ˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}•±¥Ñ•}Á±…Ñ•ˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­U¹¥ÅÕ•¹Õ‰åÌˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}•±¥Ñ•}…¹Õ‰åÌˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­U¹¥ÅÕ•±•½Á…ÑÉ„ˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}•±¥Ñ•}±•½Á…ÑÉ„ˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­U¹¥ÅÕ•A±…Ñ•ˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}•±¥Ñ•}Á±…Ñ•ˆ¤ì(€€€€€€€å¥•±É•ÑÕÉ¸!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ ‰5Ue…ÕÑ©…5…Í­U¹¥ÅÕ•I½¹¥¸ˆ°¡Õ¹Ñ•É9…µ”°¡Õ¹Ñ•É•ÍÉ¥ÁÑ¥½¸°€‰ÁÉ•‘}µ…Í­}•±¥Ñ•}É½¹¥¸ˆ¤ì((€€€€€€€™½É•… €¡Ù…Èµ…Ñ•É¥…°¥¸¹•Ýmtì€‰	½¹”ˆ°€‰É¥µÍ½¸ˆ°€‰‰½¹äˆ°€‰½±ˆ°€‰M¥±Ù•Èˆô¤(€€€€€€€€€€€å¥•±É•ÑÕÉ¸¹•ÜMÁ•¥…±5…Í­I½Ü (€€€€€€€€€€€€€€€€‰5Ue…ÕÑ©…5…Í­Q¡É…±±íµ…Ñ•É¥…±ôˆ°(€€€€€€€€€€€€€€€Ñ¡É…±±9…µ”°(€€€€€€€€€€€€€€€Ñ¡É…±±•ÍÉ¥ÁÑ¥½¸°(€€€€€€€€€€€€€€€€‰Ñ¡É…±±µ…Í­}íµ…Ñ•É¥…°¹Q½1½Ý•É%¹Ù…É¥…¹Ð ¥ôˆ°(€€€€€€€€€€€€€€€¹•ÜY•Ñ½ÈÉ¤ ÌÈ°€ÌÈ¤°(€€€€€€€€€€€€€€€€ÐÀ°(€€€€€€€€€€€€€€€€ÐÔ°(€€€€€€€€€€€€€€€€ÐÀ°(€€€€€€€€€€€€€€€€ÐÔ°(€€€€€€€€€€€€€€€€Ô¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”ÍÑ…Ñ¥ŒMÁ•¥…±5…Í­I½Ü!Õ¹Ñ•ÉMÁ•¥…±5…Í¬ (€€€€€€€ÍÑÉ¥¹œ¥°(€€€€€€€ÍÑÉ¥¹œ¹…µ”°(€€€€€€€ÍÑÉ¥¹œ‘•ÍÉ¥ÁÑ¥½¸°(€€€€€€€ÍÑÉ¥¹œÉÍ¤°(€€€€€€€Y•Ñ½ÈÉ¤üÉÍ¥M¥é”€ô¹Õ±°¤(€€€ì(€€€€€€€É•ÑÕÉ¸¹•ÜMÁ•¥…±5…Í­I½Ü (€€€€€€€€€€€¥°(€€€€€€€€€€€¹…µ”°(€€€€€€€€€€€‘•ÍÉ¥ÁÑ¥½¸°(€€€€€€€€€€€ÉÍ¤°(€€€€€€€€€€€ÉÍ¥M¥é”€üü¹•ÜY•Ñ½ÈÉ¤ ÌÈ°€ÌÈ¤°(€€€€€€€€€€€€ÐÀ°(€€€€€€€€€€€€ÔÀ°(€€€€€€€€€€€€ÐÔ°(€€€€€€€€€€€€ÔÀ°(€€€€€€€€€€€€ÄÀÀ¤ì(€€€ô((€€€ÁÉ¥Ù…Ñ”Í•…±•É•½ÉMÁ•¥…±5…Í­I½Ü (€€€€€€€ÍÑÉ¥¹œ%°(€€€€€€€ÍÑÉ¥¹œ9…µ”°(€€€€€€€ÍÑÉ¥¹œ•ÍÉ¥ÁÑ¥½¸°(€€€€€€€ÍÑÉ¥¹œIÍ¤°(€€€€€€€Y•Ñ½ÈÉ¤IÍ¥M¥é”°(€€€€€€€¥¹Ð5•±•”°(€€€€€€€¥¹Ð	Õ±±•Ð°(€€€€€€€¥¹Ð	¥¼°(€€€€€€€¥¹ÐáÁ±½Í¥½¸°(€€€€€€€¥¹Ð¹Ñ¥!Õœ¤ì)ô(