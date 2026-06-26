using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities.Helpers;

public class SpriteRenderer(AnimatedSprite sprite, float scale)
{
    public AnimatedSprite Sprite { get; set; } = sprite;
    public float Scale { get; set; } = scale;

    public void Render(SpriteBatch spriteBatch, Vector2 position, float rotation)
    {
        Debug.Assert(Sprite is not null, "SpriteRenderer has no Sprite assigned");
        if (Sprite is null) return;
        spriteBatch.Draw(Sprite, position, MathHelper.ToRadians(rotation), new Vector2(Scale));
    }
}