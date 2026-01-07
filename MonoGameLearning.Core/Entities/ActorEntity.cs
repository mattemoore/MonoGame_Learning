using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity : LogicalEntity
{
    public AnimatedSprite Sprite { get; private set; }
    public bool ScaleSpriteToBounds { get; set; }

    private float _spriteScaleFactorToFitBounds;

    protected ActorEntity(Vector2 position,
                          int width,
                          int height,
                          AnimatedSprite sprite,
                          bool scaleSpriteToBounds = true,
                          float rotation = 0f) : base(position, width, height)
    {
        Sprite = sprite;
        ScaleSpriteToBounds = scaleSpriteToBounds;
        Rotation = rotation;
        _spriteScaleFactorToFitBounds = GetUniformScaleFactorToFitBounds(width, height, Sprite.Size.X, Sprite.Size.Y);
    }

    private static float GetUniformScaleFactorToFitBounds(int boundsWidth, int boundsHeight, int spriteWidth, int spriteHeight)
    {
        float scaleX = boundsWidth / (float)spriteWidth;
        float scaleY = boundsHeight / (float)spriteHeight;
        return MathHelper.Min(scaleX, scaleY);
    }

    public override void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
        base.Update(gameTime);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(Sprite,
                        Position,
                        MathHelper.ToRadians(Rotation),
                        ScaleSpriteToBounds ? new Vector2(_spriteScaleFactorToFitBounds, _spriteScaleFactorToFitBounds) : Scale);
    }
}