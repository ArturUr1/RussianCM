using System.Collections.Generic;
using Content.Shared._CMU14.Yautja;

namespace Content.Client._CMU14.Yautja.Lobby;

public enum YautjaProfileEditorCategory
{
    Appearance,
    Equipment,
    Sets,
    Technology,
    Description,
}

public sealed record YautjaProfileEditorCategoryInfo(
    YautjaProfileEditorCategory Id,
    string LocalizationKey);

public static class YautjaProfileEditorLayout
{
    public static IReadOnlyList<YautjaProfileEditorCategoryInfo> Categories { get; } =
    [
        new(YautjaProfileEditorCategory.Appearance, "cmu-yautja-lobby-category-appearance"),
        new(YautjaProfileEditorCategory.Equipment, "cmu-yautja-lobby-category-equipment"),
        new(YautjaProfileEditorCategory.Sets, "cmu-yautja-lobby-category-sets"),
        new(YautjaProfileEditorCategory.Technology, "cmu-yautja-lobby-category-technology"),
        new(YautjaProfileEditorCategory.Description, "cmu-yautja-lobby-category-description"),
    ];

    public static bool IsUniqueSetLocked(YautjaCharacterProfile profile, YautjaUniqueSet unique)
    {
        return unique != YautjaUniqueSet.None && !YautjaRankResolver.CanUseUnique(profile);
    }

    public static bool IsCategoryActive(
        YautjaProfileEditorCategory active,
        YautjaProfileEditorCategory candidate)
    {
        return active == candidate;
    }
}
