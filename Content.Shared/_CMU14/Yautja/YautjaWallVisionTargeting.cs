using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared._CMU14.Yautja;

public static class YautjaWallVisionTargeting
{
    public static bool IsEligible(
        EntityUid viewer,
        EntityUid target,
        MapId viewerMap,
        MapId targetMap,
        bool targetIsMob,
        bool targetSpriteVisible,
        bool targetInContainer)
    {
        return viewer != target &&
               viewerMap == targetMap &&
               targetIsMob &&
               targetSpriteVisible &&
               !targetInContainer;
    }
}
