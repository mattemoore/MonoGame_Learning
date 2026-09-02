using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

public static class BatSprite
{
    public const string AnimationSwing = "swing";
    private const int FrameCount = 4;

    private static readonly SpriteSheetAsset Asset = new(
        "bat", "images/bat",
        new SpriteAnimationDef(AnimationSwing, "bat", FrameCount, false));

    public static SpriteSheet Sheet => Asset.Sheet;

    public static void Load(ContentManager content) => Asset.Load(content);

    public static AnimatedSprite Create() => Asset.Create(AnimationSwing);
}