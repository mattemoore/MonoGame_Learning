using System;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Game.AnimatedSprites;

internal static class SpriteSheetAnimationExtensions
{
    /// <summary>
    /// Defines an animation from a run of atlas regions named <c>{prefix}-{index:00}</c>.
    /// </summary>
    /// <param name="firstFrame">
    /// Index of the first atlas region in the run — the animation uses regions
    /// <c>{prefix}-{firstFrame:00}</c> through <c>{prefix}-{firstFrame + frameCount - 1:00}</c>.
    /// Defaults to 0 for contiguous runs; pass a non-zero value when the regions are offset
    /// (e.g. OilDrumSprite's single-frame states live at oildrum-00/01/02).
    /// </param>
    public static void DefineFrames(this SpriteSheet sheet, string animationName, string prefix, int frameCount, bool isLooping, int firstFrame = 0, double frameDuration = 0.1)
    {
        sheet.DefineAnimation(animationName, builder =>
        {
            builder.IsLooping(isLooping);
            for (int i = 0; i < frameCount; i++)
                builder.AddFrame($"{prefix}-{firstFrame + i:00}", TimeSpan.FromSeconds(frameDuration));
        });
    }
}