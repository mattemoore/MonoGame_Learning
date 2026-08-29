using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

public static class EnemySprite
{
    public const string AnimationIdle = "idle";
    public const string AnimationRun = "run";
    public const string AnimationAttack1 = "attack1";
    public const string AnimationHurt = "hurt";
    public const string AnimationDie = "die";
    public const string AnimationFall = "fall";
    public const string AnimationGetUp = "getup";

    private static SpriteSheet _spriteSheet;
    private static bool _loaded;

    public static void Load(ContentManager content)
    {
        if (_loaded) return;
        _loaded = true;

        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/adventurer");
        _spriteSheet = new("adventurer", atlas);

        _spriteSheet.DefineFrames(AnimationIdle, "adventurer-idle", 4, true);
        _spriteSheet.DefineFrames(AnimationRun, "adventurer-run", 6, true);
        _spriteSheet.DefineFrames(AnimationAttack1, "adventurer-attack1", 4, false);
        _spriteSheet.DefineFrames(AnimationHurt, "adventurer-hurt", 3, false);
        _spriteSheet.DefineFrames(AnimationDie, "adventurer-die", 7, false);
        _spriteSheet.DefineFrames(AnimationFall, "adventurer-fall", 2, false);
        _spriteSheet.DefineFrames(AnimationGetUp, "adventurer-stand", 3, false);
    }

    public static AnimatedSprite Create()
    {
        var sprite = new AnimatedSprite(_spriteSheet, AnimationIdle);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }
}
