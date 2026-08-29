using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

public static class BatSprite
{
    public const string AnimationSwing = "swing";
    private const int FrameCount = 4;

    private static SpriteSheet _sheet;
    private static bool _loaded;

    public static SpriteSheet Sheet => _sheet;

    public static void Load(ContentManager content)
    {
        if (_loaded) return;
        _loaded = true;

        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/bat");
        _sheet = new SpriteSheet("bat", atlas);
        _sheet.DefineFrames(AnimationSwing, "bat", FrameCount, false);
    }

    public static AnimatedSprite Create()
    {
        var sprite = new AnimatedSprite(_sheet, AnimationSwing);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }
}