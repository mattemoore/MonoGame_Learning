using System;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Game.Sprites;

public static class PlayerSprite
{
    public const string AnimationIdle = "idle";
    public const string AnimationRun = "run";
    public const string AnimationAttack1 = "attack1";
    public const string AnimationAttack2 = "attack2";
    public const string AnimationAttack3 = "attack3";
    private const double FrameDuration = 0.1;

    public static AnimatedSprite GetPlayerSprite(ContentManager content)
    {
        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/adventurer");
        SpriteSheet spriteSheet = new("adventurer", atlas);

        DefineAnimation(spriteSheet, AnimationIdle, "adventurer-idle", 4, true);
        DefineAnimation(spriteSheet, AnimationAttack1, "adventurer-attack1", 4, false);
        DefineAnimation(spriteSheet, AnimationAttack2, "adventurer-attack2", 4, false);
        DefineAnimation(spriteSheet, AnimationAttack3, "adventurer-attack3", 4, false);
        DefineAnimation(spriteSheet, AnimationRun, "adventurer-run", 6, true);

        return new(spriteSheet, AnimationIdle);
    }

    private static void DefineAnimation(SpriteSheet spriteSheet, string animationName, string prefix, int frameCount, bool isLooping)
    {
        spriteSheet.DefineAnimation(animationName, builder =>
        {
            builder.IsLooping(isLooping);
            for (int i = 0; i < frameCount; i++)
            {
                builder.AddFrame($"{prefix}-{i:00}", TimeSpan.FromSeconds(FrameDuration));
            }
        });
    }
}