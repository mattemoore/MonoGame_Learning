using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity(string name,
                                 Vector2 position,
                                 float scale,
                                 AnimatedSprite sprite,
                                 float rotation = 0f)
                                 : SpatialEntity(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale), rotation)
                                 , ICollisionActor
{
    public AnimatedSprite Sprite { get; private set; } = sprite;
    public float Scale { get; private set; } = scale;
    public IShapeF Bounds => Frame;

    public RectangleF MovementBounds { get; set; }
    public HitboxService HitboxService { get; set; }
    public MoveData CurrentMove { get; set; }
    public FacingDirection Direction { get; set; } = FacingDirection.Right;

    private int _animationFrameIndex;
    // Sprite.Controller.CurrentFrame returns the global texture atlas region
    // index, NOT the 0-based position within the current animation's frame
    // sequence. We track _animationFrameIndex manually by detecting atlas frame
    // changes in Update(). ResetAnimationFrameIndex() must be called after every
    // SetAnimation() to reset this counter.
    private int _lastRegisteredAnimationFrame = -1;

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
        int oldAtlasFrame = Sprite.Controller.CurrentFrame;
        Sprite.Update(gameTime);
        if (Sprite.Controller.CurrentFrame != oldAtlasFrame)
            _animationFrameIndex++;
        base.Update(gameTime);

        if (CurrentMove is not null && _animationFrameIndex != _lastRegisteredAnimationFrame)
        {
            // Animation frame changed — clear this entity's old hitboxes and
            // register the new frame's. Clear() is owner-scoped so other
            // entities' hitboxes are not affected.
            _lastRegisteredAnimationFrame = _animationFrameIndex;
            HitboxService?.Clear(this);
            HitboxService?.RegisterFrameHitboxes(this, CurrentMove, _animationFrameIndex, Direction);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
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

    public void ResetAnimationFrameIndex()
    {
        _animationFrameIndex = 0;
        _lastRegisteredAnimationFrame = -1;
    }

    public virtual void TakeDamage(int amount)
    {
    }
}