using MonoGame.Extended;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

public abstract class LogicalEntity(Vector2 position, int width, int height) : Entity(position, width, height)
{
    public RectangleF Bounds => new RectangleF(
        Position.X - (Width / 2f),
        Position.Y - (Height / 2f),
        Width,
        Height
    );

    public override void Update(GameTime gameTime)
    {
        // TODO: Does nothing for now
    }
}