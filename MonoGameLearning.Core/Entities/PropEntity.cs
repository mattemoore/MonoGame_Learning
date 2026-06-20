using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class PropEntity : SpatialEntity, ICollisionActor, IDamageable
{
    public AnimatedSprite Sprite { get; private set; }
    public float Scale { get; private set; }
    public IShapeF Bounds => Frame;

    public int Health { get; protected set; }
    public int MaxHealth { get; protected set; }
    public bool IsAlive { get; protected set; } = true;

    public event Action<PropEntity> Destroyed;

    protected void OnDestroyed() => Destroyed?.Invoke(this);

    protected PropEntity(string name, Vector2 position, float scale, AnimatedSprite sprite, float rotation = 0f)
        : base(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale), rotation)
    {
        Sprite = sprite;
        Scale = scale;
    }

    protected PropEntity(string name, Vector2 position, int width, int height, float rotation = 0f)
        : base(name, position, width, height, rotation)
    {
        Sprite = null!;
        Scale = 1f;
    }

    public override void Update(GameTime gameTime)
    {
        Sprite?.Update(gameTime);
        base.Update(gameTime);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        if (Sprite is null) return;
        spriteBatch.Draw(Sprite,
                        Position,
                        MathHelper.ToRadians(Rotation),
                        new Vector2(Scale));
    }

    public override void DrawDebug(SpriteBatch spriteBatch)
    {
        base.DrawDebug(spriteBatch);
        spriteBatch.DrawRectangle(Frame, Color.Blue);
    }

    public virtual void OnCollision(CollisionEventArgs collisionInfo) { }

    public virtual void TakeDamage(int amount, bool knockdown = false)
    {
        if (!IsAlive) return;

        Health = Math.Max(0, Health - amount);

        if (Health <= 0)
        {
            IsAlive = false;
            Destroyed?.Invoke(this);
        }
    }
}