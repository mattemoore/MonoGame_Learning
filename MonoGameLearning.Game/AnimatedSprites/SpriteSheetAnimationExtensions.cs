using System;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Game.AnimatedSprites;

internal static class SpriteSheetAnimationExtensions
{
    public static void DefineFrames(this SpriteSheet sheet, string animationName, string prefix, int frameCount, bool isLooping, double frameDuration = 0.1)
    {
        sheet.DefineAnimation(animationName, builder =>
        {
            builder.IsLooping(isLooping);
            for (int i = 0; i < frameCount; i++)
                builder.AddFrame($"{prefix}-{i:00}", TimeSpan.FromSeconds(frameDuration));
        });
    }
}