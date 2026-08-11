namespace Content.Client._RMC14.DonorCapes;

public readonly record struct DonorCapePreviewFrameRange(int Start, int Count);

public static class DonorCapePreviewAnimation
{
    private const int TransitionFrameCount = 2;

    public static DonorCapePreviewFrameRange GetBackViewFrameRange(int frameCount)
    {
        if (frameCount <= 0)
            return new(0, 1);

        if (frameCount <= TransitionFrameCount * 2)
            return new(0, frameCount);

        return new(TransitionFrameCount, frameCount - TransitionFrameCount * 2);
    }
}
