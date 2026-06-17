using System;
using Microsoft.Xna.Framework;
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
    public const string AnimationHurt = "hurt";
    public const string AnimationDie = "die";
    public const string AnimationFall = "fall";
    public const string AnimationGetUp = "getup";
    private const double FrameDuration = 0.1;

    private static SpriteSheet _spriteSheet;
    private static bool _loaded;

    public static void Load(ContentManager content)
    {
        if (_loaded) return;
        _loaded = true;

        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/adventurer");
        _spriteSheet = new("adventurer", atlas);

        DefineAnimation(AnimationIdle, "adventurer-idle", 4, true);
        DefineAnimation(AnimationAttack1, "adventurer-attack1", 4, false);
        DefineAnimation(AnimationAttack2, "adventurer-attack2", 4, false);
        DefineAnimation(AnimationAttack3, "adventurer-attack3", 4, false);
        DefineAnimation(AnimationRun, "adventurer-run", 6, true);
        DefineAnimation(AnimationHurt, "adventurer-hurt", 3, false);
        DefineAnimation(AnimationDie, "adventurer-die", 7, false);
        DefineAnimation(AnimationFall, "adventurer-fall", 2, false);
        DefineAnimation(AnimationGetUp, "adventurer-stand", 3, false);
    }

    public static AnimatedSprite Create()
    {
        var sprite = new AnimatedSprite(_spriteSheet, AnimationIdle);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }

    private static void DefineAnimation(string animationName, string prefix, int frameCount, bool isLooping)
    {
        _spriteSheet.DefineAnimation(animationName, builder =>
        {
            builder.IsLooping(isLooping);
            for (int i = 0; i < frameCount; i++)
            {
                builder.AddFrame($"{prefix}-{i:00}", TimeSpan.FromSeconds(FrameDuration));
            }
        });
    }
}