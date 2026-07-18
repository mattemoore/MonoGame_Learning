using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Components;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Core.Rendering;


namespace MonoGameLearning.Core.Entities;

public abstract class PropBase(string name, Vector2 position, AnimatedSprite sprite, float scale, int maxHealth, CollisionAnchor anchor) : Entity(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale)), IRenderable, IDebugDrawable, ICollisionActor, IDamageable
{
    public int Id => GetHashCode();
    public CollisionAnchor Anchor { get; } = anchor;
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
    public event Action<Entity> Destroyed;

    protected readonly SpriteRenderer SpriteRenderer = new(sprite, scale);
    protected readonly Health HealthComponent = new(maxHealth);

    public AnimatedSprite Sprite => SpriteRenderer.Sprite;
    public Faction Faction => Faction.Neutral;
    public event EventHandler Died;

    int IDamageable.Health => HealthComponent.Value;
    int IDamageable.MaxHealth => HealthComponent.MaxHealth;
    bool IDamageable.IsAlive => HealthComponent.IsAlive;
    bool IDamageable.CanTakeDamage() => HealthComponent.IsAlive;
    void IDamageable.ReduceHealth(int amount) => HealthComponent.Subtract(amount);
    void IDamageable.OnDeath() => OnDestroyed();
    void IDamageable.OnKnockdown(DamageInfo info) { }
    void IDamageable.OnHit(DamageInfo info) { }

    public void Render(RenderContext context)
    {
        if (Sprite is null) return;
        context.SpriteBatch.Draw(Sprite, Position, 0f, new Vector2(SpriteRenderer.Scale));
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
