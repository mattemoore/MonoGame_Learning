using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Entities;

public abstract class PickupBase(string name, Vector2 position, Texture2D texture)
    : Entity(name, position, texture.Width, texture.Height), IRenderable, IDebugDrawable, ICollisionActor, IPickup
{
    public int Id => GetHashCode();
    protected Texture2D Texture { get; } = texture;

    public CollisionShape2D Shape => new(new BoundingBox2D(
        new Vector2(Frame.X, Frame.Y),
        new Vector2(Frame.Right, Frame.Bottom)));

    public void Render(RenderContext context)
    {
        context.SpriteBatch.Draw(Texture, Position, null, Color.White,
            0f, new Vector2(Texture.Width / 2f, Texture.Height / 2f), 1f, SpriteEffects.None, 0f);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(Frame, Color.Yellow);
    }

    public abstract void OnPickup(IDamageable target);
}
