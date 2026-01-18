using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Entities;

public abstract class LogicalEntity(string name, Vector2 position, int width, int height, float rotation = 0.0f)
    : Entity(name, position, width, height, rotation)
{
    public RectangleF Frame => new(
        Position.X - (Width / 2f),
        Position.Y - (Height / 2f),
        Width,
        Height
    );

    public override void Update(GameTime gameTime)
    {
    }

    public override void DrawDebug(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawRectangle(Frame, Color.AntiqueWhite);
    }
}