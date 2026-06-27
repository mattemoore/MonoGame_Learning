using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class PropBase(string name, Vector2 position, AnimatedSprite sprite, float scale, int maxHealth) : Entity(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale)), IRenderable, IDebugDrawable, ICollisionActor, IDamageable
{
    public IShapeF Bounds => Frame;
    public event Action<Entity> Destroyed;

    protected readonly SpriteRenderer SpriteRenderer = new(sprite, scale);
    protected readonly Health HealthComponent = new(maxHealth);

    public AnimatedSprite Sprite => SpriteRenderer.Sprite;
    public Faction Faction => Faction.Neutral;
    #pragma warning disable CS0067
    public event EventHandler Died;
#pragma warning restore CS0067

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
        context.SpriteBatch.DrawRectangle(Frame, Color.AntiqueWhite);
        context.SpriteBatch.DrawRectangle(Frame, Color.Blue);
        HealthDisplay.Draw(context.SpriteBatch, context.Font, Frame, HealthComponent.Value, HealthComponent.MaxHealth);
    }

    public void OnCollision(CollisionEventArgs collisionInfo) { }

    public abstract void TakeDamage(DamageInfo info);

    protected void OnDestroyed() => Destroyed?.Invoke(this);
}