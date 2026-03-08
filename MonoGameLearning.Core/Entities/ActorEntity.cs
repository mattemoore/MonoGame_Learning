using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity(string name,
                                 Vector2 position,
                                 float scale,
                                 AnimatedSprite sprite,
                                 float rotation = 0f)
                                 : LogicalEntity(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale), rotation)
                                 , ICollisionActor
{
    public AnimatedSprite Sprite { get; private set; } = sprite;
    public float Scale { get; private set; } = scale;
    public IShapeF Bounds => Frame;

    public RectangleF MovementBounds { get; set; }

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
        Sprite.Update(gameTime);
        base.Update(gameTime);
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
        spriteBatch.DrawRectangle((RectangleF)Bounds, Color.Brown);
    }

    public virtual void OnCollision(CollisionEventArgs collisionInfo)
    {
        Position -= collisionInfo.PenetrationVector;
    }
}