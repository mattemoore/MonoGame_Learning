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

    public void OnCollision(CollisionEventArgs collisionInfo)
    {
        Position -= collisionInfo.PenetrationVector;
    }
}