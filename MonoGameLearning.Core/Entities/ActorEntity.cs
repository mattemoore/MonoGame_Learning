using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity : SpatialEntity, ICollisionActor, IAnimated, IHitboxProvider, IMoveableEntity, IDamageable
{
    public AnimatedSprite Sprite { get; private set; }
    public float Scale { get; private set; }
    public IShapeF Bounds => Frame;

    public RectangleF MovementBounds { get; set; }
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; } = 200f;
    public HitboxService HitboxService { get; set; }
    public MoveData CurrentMove { get; set; }
    public FacingDirection Direction { get; set; } = FacingDirection.Right;

    private readonly AnimationFrameTracker _frameTracker = new();

    protected ActorEntity(string name, Vector2 position, float scale, AnimatedSprite sprite, float rotation = 0f)
        : base(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale), rotation)
    {
        Sprite = sprite;
        Scale = scale;
    }

    protected ActorEntity(string name, Vector2 position, int width, int height, float rotation = 0f)
        : base(name, position, width, height, rotation)
    {
        Sprite = null!;
        Scale = 1f;
    }

    public virtual void ClampToBounds()
    {
        if (MovementBounds.IsEmpty) return;

        float halfWidth = Width / 2f;
        float halfHeight = Height / 2f;

        Position = new Vector2(
            MathHelper.Clamp(Position.X, MovementBounds.Left + halfWidth, MovementBounds.Right - halfWidth),
            MathHelper.Clamp(Position.Y, MovementBounds.Top + halfHeight, MovementBounds.Bottom - halfHeight)
        );
    }

    public override void Update(GameTime gameTime)
    {
        Debug.Assert(Sprite is not null, $"ActorEntity [{Name}] has no Sprite assigned");
        if (Sprite is null) return;
        _frameTracker.AdvanceOnFrameChange(Sprite, gameTime);
        base.Update(gameTime);

        if (CurrentMove is not null && _frameTracker.TryGetNewFrame(out var newFrameIndex))
        {
            HitboxService?.Clear(this);
            HitboxService?.RegisterFrameHitboxes(this, CurrentMove, newFrameIndex, Direction);
        }
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        Debug.Assert(Sprite is not null, $"ActorEntity [{Name}] has no Sprite assigned");
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

        if (HitboxService is not null)
        {
            foreach (var bounds in HitboxService.GetActiveHitboxBounds(this))
                spriteBatch.DrawRectangle(bounds, Color.Red);
        }
    }

    public virtual void OnCollision(CollisionEventArgs collisionInfo)
    {
        Position -= collisionInfo.PenetrationVector;
    }

    public void ResetAnimationFrameIndex() => _frameTracker.Reset();

    public abstract void TakeDamage(int amount, bool knockdown = false);
}