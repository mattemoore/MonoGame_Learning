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

    private static readonly SpriteSheetAsset Asset = new(
        "adventurer", "images/adventurer",
        new SpriteAnimationDef(AnimationIdle, "adventurer-idle", 4, true),
        new SpriteAnimationDef(AnimationRun, "adventurer-run", 6, true),
        new SpriteAnimationDef(AnimationAttack1, "adventurer-attack1", 4, false),
        new SpriteAnimationDef(AnimationHurt, "adventurer-hurt", 3, false),
        new SpriteAnimationDef(AnimationDie, "adventurer-die", 7, false),
        new SpriteAnimationDef(AnimationFall, "adventurer-fall", 2, false),
        new SpriteAnimationDef(AnimationGetUp, "adventurer-stand", 3, false));

    public static void Load(ContentManager content) => Asset.Load(content);

    public static AnimatedSprite Create() => Asset.Create(AnimationIdle);
}