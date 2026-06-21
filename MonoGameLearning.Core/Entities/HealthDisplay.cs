using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Entities;

public static class HealthDisplay
{
    public static string Format(int health, int maxHealth) => $"{health}/{maxHealth}";

    public static void Draw(SpriteBatch spriteBatch, SpriteFont font, RectangleF frame, int health, int maxHealth)
    {
        var text = Format(health, maxHealth);
        var size = font.MeasureString(text);
        spriteBatch.DrawString(font, text,
            new Vector2(frame.Center.X - size.X / 2, frame.Top - size.Y - 2), Color.White);
    }
}