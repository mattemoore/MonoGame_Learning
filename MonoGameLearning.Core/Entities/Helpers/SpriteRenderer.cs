using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities.Helpers;

public class SpriteRenderer
{
    public AnimatedSprite Sprite { get; set; }
    public float Scale { get; set; }

    public SpriteRenderer(AnimatedSprite sprite, float scale)
    {
        Sprite = sprite;
        Scale = scale;
    }

    public void Render(SpriteBatch spriteBatch, Vector2 position, float rotation)
    {
        Debug.Assert(Sprite is not null, "SpriteRenderer has no Sprite assigned");
        if (Sprite is null) return;
        spriteBatch.Draw(Sprite, position, MathHelper.ToRadians(rotation), new Vector2(Scale));
    }
}