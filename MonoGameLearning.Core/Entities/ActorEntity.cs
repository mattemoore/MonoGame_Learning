using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity(Vector2 position,
                                 int width,
                                 int height,
                                 AnimatedSprite sprite,
                                 float rotation = 0f) : LogicalEntity(position, width, height, rotation)
{
    public AnimatedSprite Sprite { get; private set; } = sprite;

    private static Vector2 GetUniformScaleFactorToFitBounds(int boundsWidth, int boundsHeight, int spriteWidth, int spriteHeight) =>
        new(MathHelper.Min(boundsWidth / (float)spriteWidth, boundsHeight / (float)spriteHeight));

    public override void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
        base.Update(gameTime);
    }

    public virtual void Draw(SpriteBatch spriteBatch) =>
        spriteBatch.Draw(Sprite,
                        Position,
                        MathHelper.ToRadians(Rotation),
                        GetUniformScaleFactorToFitBounds(Width, Height, Sprite.Size.X, Sprite.Size.Y));

}