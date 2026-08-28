using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Rendering;


namespace MonoGameLearning.Core.Entities.Prop;

public abstract class PropBase : Entity, IRenderable, IDebugDrawable, ICollisionActor, ICollisionLayer, IDamageable, IDamageResponse, IPickupDropper
{
    public string LayerName => CollisionLayers.Props;
    protected PropBase(string name, Vector2 position, AnimatedSprite sprite, float scale, int maxHealth, CollisionAnchor anchor)
        : base(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale))
    {
        Anchor = anchor;
        SpriteRenderer = new(sprite, scale);
        HealthComponent = new(maxHealth);
    }

    // Sprite-less overload for test doubles — SpriteRenderer gets null sprite (ops are no-ops).
    protected PropBase(string name, Vector2 position, int width, int height, int maxHealth, CollisionAnchor anchor)
        : base(name, position, width, height)
    {
        Anchor = anchor;
        SpriteRenderer = new(null, 1f);
        HealthComponent = new(maxHealth);
    }

    public int Id => GetHashCode();
    public CollisionAnchor Anchor { get; }
    public virtual float CollisionHeightFraction => 1.0f;
    protected RectangleF CollisionBounds => ComputeCollisionBounds(Frame, CollisionHeightFraction, Anchor);
    internal static RectangleF ComputeCollisionBounds(RectangleF frame, float heightFraction, CollisionAnchor anchor)
    {
        Debug.Assert(heightFraction is > 0f and <= 1f, $"CollisionHeightFraction must be in (0,1], got {heightFraction}");
        float h = frame.Height * heightFraction;
        float y = anchor switch
        {
            CollisionAnchor.Top => frame.Y,
            CollisionAnchor.Center => frame.Y + (frame.Height - h) * 0.5f,
            CollisionAnchor.Bottom => frame.Bottom - h,
            _ => frame.Y,
        };
        return new RectangleF(frame.X, y, frame.Width, h);
    }

    public CollisionShape2D Shape => new(new BoundingBox2D(
        new Vector2(CollisionBounds.X, CollisionBounds.Y),
        new Vector2(CollisionBounds.Right, CollisionBounds.Bottom)));
    public event Action<PropBase> Destroyed = null!;

    public IReadOnlyList<PickupSpawnDef>? Drops { get; set; }

    public IReadOnlyList<PickupSpawnDef> CreateDrops() => Drops ?? [];

    protected readonly SpriteRenderer SpriteRenderer;
    protected readonly Health HealthComponent;

    public event EventHandler Died = null!;

    int IDamageable.Health => HealthComponent.Value;
    int IDamageable.MaxHealth => HealthComponent.MaxHealth;
    bool IDamageable.IsAlive => HealthComponent.IsAlive;

    bool IDamageResponse.IsAlive => HealthComponent.IsAlive;
    void IDamageResponse.ReduceHealth(int amount) => HealthComponent.Subtract(amount);
    void IDamageResponse.OnDeath() => OnDestroyed();

    void IDamageable.Heal(int amount) { }

    public void Render(RenderContext context)
    {
        SpriteRenderer.Render(context.SpriteBatch, Position, 0f);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(Frame, Color.Blue);
        context.SpriteBatch.DrawRectangle(CollisionBounds, Color.Blue);
        var text = HealthComponent.ToDisplayString();
        var size = context.Font.MeasureString(text);
        context.SpriteBatch.DrawString(context.Font, text,
            new Vector2(Frame.Center.X - size.X / 2, Frame.Top - size.Y - 2), Color.White);
    }

    public abstract void TakeDamage(DamageInfo info);

    protected void OnDestroyed()
    {
        Died?.Invoke(this, EventArgs.Empty);
        Destroyed?.Invoke(this);
    }
}
