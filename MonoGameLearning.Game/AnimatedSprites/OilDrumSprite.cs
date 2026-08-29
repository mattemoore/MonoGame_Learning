using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

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

        _spriteSheet.DefineFrames(AnimationIdle, "oildrum", 1, true);
        _spriteSheet.DefineFrames(AnimationDamaged, "oildrum", 1, true, firstFrame: 1);
        _spriteSheet.DefineFrames(AnimationCritical, "oildrum", 1, true, firstFrame: 2);
    }

    public static AnimatedSprite Create()
    {
        var sprite = new AnimatedSprite(_spriteSheet, AnimationIdle);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }
}