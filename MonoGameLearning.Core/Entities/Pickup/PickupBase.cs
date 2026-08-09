using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Entities.Pickup;

public abstract class PickupBase : Entity, IRenderable, IDebugDrawable, ICollisionActor, ICollisionLayer, IPickup
{
    public string LayerName => "pickups";

    private const int DefaultTextureSize = 32;

    // Texture-based ctor — derives size from the texture (may be null in headless tests).
    protected PickupBase(string name, Vector2 position, Texture2D? texture)
        : this(name, position, texture?.Width ?? DefaultTextureSize, texture?.Height ?? DefaultTextureSize, texture) { }

    // Size-bearing overload for test doubles without a GraphicsDevice — Texture may be null (Render guards).
    protected PickupBase(string name, Vector2 position, int width, int height, Texture2D? texture)
        : base(name, position, width, height)
    {
        Texture = texture;
    }

    public int Id => GetHashCode();
    protected Texture2D? Texture { get; }

    public CollisionShape2D Shape => new(new BoundingBox2D(
        new Vector2(Frame.X, Frame.Y),
        new Vector2(Frame.Right, Frame.Bottom)));

    public void Render(RenderContext context)
    {
        if (Texture is null) return;
        context.SpriteBatch.Draw(Texture, Position, null, Color.White,
            0f, new Vector2(Texture.Width / 2f, Texture.Height / 2f), 1f, SpriteEffects.None, 0f);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(Frame, Color.Yellow);
    }

    public abstract void OnPickup(IDamageable target);
}
