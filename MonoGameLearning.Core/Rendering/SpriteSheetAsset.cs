using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Rendering;

public readonly record struct SpriteAnimationDef(string Name, string Prefix, int FrameCount, bool Loop, int FirstFrame = 0);

public sealed class SpriteSheetAsset(string sheetName, string assetPath, params SpriteAnimationDef[] defs)
{
    private SpriteSheet? _sheet;
    private bool _loaded;

    public SpriteSheet? Sheet => _sheet;

    public void Load(ContentManager content)
    {
        if (_loaded) return;
        _loaded = true;

        Texture2DAtlas atlas = content.Load<Texture2DAtlas>(assetPath);
        var sheet = new SpriteSheet(sheetName, atlas);
        foreach (var def in defs)
            sheet.DefineFrames(def.Name, def.Prefix, def.FrameCount, def.Loop, def.FirstFrame);
        _sheet = sheet;
    }

    public AnimatedSprite Create(string defaultAnimation)
    {
        if (_sheet is null)
            throw new InvalidOperationException($"{sheetName} used before Load(content).");
        var sprite = new AnimatedSprite(_sheet, defaultAnimation);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }
}