using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Game.Sprites;

public static class OilDrumSprite
{
    public const string AnimationIdle = "idle";
    public const string AnimationDamaged = "damaged";
    public const string AnimationCritical = "critical";

    private static SpriteSheet _spriteSheet;
    private static bool _loaded;

    public static void Load(ContentManager content)
    {
        if (_loaded) return;
        _loaded = true;
        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/oilcan");
        _spriteSheet = new("oilcan", atlas);

        _spriteSheet.DefineAnimation(AnimationIdle, builder =>
        {
            builder.IsLooping(true);
            builder.AddFrame("oildrum-00", TimeSpan.FromSeconds(0.1));
        });

        _spriteSheet.DefineAnimation(AnimationDamaged, builder =>
        {
            builder.IsLooping(true);
            builder.AddFrame("oildrum-01", TimeSpan.FromSeconds(0.1));
        });

        _spriteSheet.DefineAnimation(AnimationCritical, builder =>
        {
            builder.IsLooping(true);
            builder.AddFrame("oildrum-02", TimeSpan.FromSeconds(0.1));
        });
    }

    public static AnimatedSprite Create()
    {
        var sprite = new AnimatedSprite(_spriteSheet, AnimationIdle);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }
}