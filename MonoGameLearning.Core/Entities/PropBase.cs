using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class PropBase(string name, Vector2 position, AnimatedSprite sprite, float scale, int maxHealth) : Entity(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale)), IRenderable, IDebugDrawable, ICollisionActor, IDamageable, IHasHealth
{
    public IShapeF Bounds => Frame;
    public event Action<Entity> Destroyed;

    protected readonly SpriteRenderer SpriteRenderer = new(sprite, scale);
    protected readonly Health HealthComponent = new(maxHealth);

    public AnimatedSprite Sprite => SpriteRenderer.Sprite;

    int IHasHealth.Health => HealthComponent.Value;
    int IHasHealth.MaxHealth => HealthComponent.MaxHealth;

    public void Render(RenderContext context)
    {
        if (Sprite is null) return;
        context.SpriteBatch.Draw(Sprite, Position, MathHelper.ToRadians(Rotation), new Vector2(SpriteRenderer.Scale));
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