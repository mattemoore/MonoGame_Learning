using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLearning.Game.AnimatedSprites;

public static class GoIndicatorSprite
{
    private static Texture2D _texture;
    private static bool _loaded;

    public static Texture2D Texture => _texture;

    public static void Load(ContentManager content)
    {
        if (_loaded) return;
        _loaded = true;
        _texture = content.Load<Texture2D>("images/arrow");
    }

    public static void Unload()
    {
        _loaded = false;
        _texture?.Dispose();
        _texture = null;
    }
}