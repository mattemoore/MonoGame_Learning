using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

public static class OilDrumSprite
{
    public const string AnimationIdle = "idle";
    public const string AnimationDamaged = "damaged";
    public const string AnimationCritical = "critical";

    private static readonly SpriteSheetAsset Asset = new(
        "oilcan", "images/oilcan",
        new SpriteAnimationDef(AnimationIdle, "oildrum", 1, true),
        new SpriteAnimationDef(AnimationDamaged, "oildrum", 1, true, FirstFrame: 1),
        new SpriteAnimationDef(AnimationCritical, "oildrum", 1, true, FirstFrame: 2));

    public static void Load(ContentManager content) => Asset.Load(content);

    public static AnimatedSprite Create() => Asset.Create(AnimationIdle);
}