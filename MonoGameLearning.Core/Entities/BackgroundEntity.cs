using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public class BackgroundEntity(string name, Sprite sprite, Vector2 position, int width, int height)
    : Entity(name, position, width, height), IRenderable, IDebugDrawable
{
    public RectangleF MovementBounds { get; set; } = new(position.X - (width / 2f), position.Y - (height / 2f) + (height * 0.6f), width, height * 0.4f);
    Sprite Sprite { get; } = sprite;

    public void Render(RenderContext context)
    {
        if (Sprite is not null)
            context.SpriteBatch.Draw(Sprite, Frame.Position);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(MovementBounds, Color.Yellow);
        context.SpriteBatch.DrawRectangle(Frame, Color.AntiqueWhite);
    }
}