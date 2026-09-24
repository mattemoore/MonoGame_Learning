using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Animation;
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
        new SpriteAnimationDef(AnimationGetUp, "adventurer-getup", 3, false));

    public static void Load(ContentManager content) => Asset.Load(content);

    public static AnimatedSprite Create() => Asset.Create(AnimationIdle);

    /// <summary>The animation definitions this actor declares, for tests to key hand data against.</summary>
    public static SpriteAnimationDef[] AnimationDefs => Asset.Defs;

    /// <summary>
    /// Per-frame hand points (actor units from Position) for the animations this actor uses.
    /// Authored as pivots on the source's Aseprite `hand` slice and pasted from
    /// `python3 Utils/aseprite_to_monogame_extended.py .../adventurer.json`. The enemy shares
    /// the placeholder Adventurer atlas, so it shares the same hand data until it gets its own
    /// art; keeping its own table means that swap is local to this file.
    /// </summary>
    public static readonly HandAnchorTable HandAnchors = new(
        (AnimationIdle, [new(-7, 5.5f), new(-7, 5.5f), new(-7, 5.5f), new(-7, 3.5f)]),
        (AnimationRun, [new(9, 2.5f), new(5, 3.5f), new(2, 3.5f), new(-3, -0.5f), new(1, 2.5f), new(5, 4.5f)]),
        (AnimationAttack1, [new(-9, 5.5f), new(-9, 5.5f), new(10, -4.5f), new(8, -4.5f)]),
        (AnimationHurt, [new(2, 4.5f), new(-1, 5.5f), new(-3, 5.5f)]),
        (AnimationDie, [new(1, 4.5f), new(-1, 5.5f), new(-1, 5.5f), new(1, 6.5f), new(-2, 12.5f), new(-1, 12.5f), new(-1, 13.5f)]),
        (AnimationFall, [new(-1, -10.5f), new(-1, -10.5f)]),
        (AnimationGetUp, [new(-7, 15.5f), new(-7, 15.5f), new(-7, 15.5f)]));
}