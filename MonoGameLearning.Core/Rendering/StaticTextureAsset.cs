using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLearning.Core.Rendering;

public sealed class StaticTextureAsset(string assetPath)
{
    private readonly string _assetPath = assetPath;
    private Texture2D? _texture;
    private bool _loaded;

    public Texture2D? Texture => _texture;

    public void Load(ContentManager content)
    {
        if (_loaded) return;
        _texture = content.Load<Texture2D>(_assetPath);
        _loaded = true;
    }
}