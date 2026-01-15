using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity(Vector2 position,
                                 float scale,
                                 AnimatedSprite sprite,
                                 float rotation = 0f) : LogicalEntity(position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale), rotation)
{
    public AnimatedSprite Sprite { get; private set; } = sprite;
    public float Scale { get; private set; } = scale;

    public override void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
        base.Update(gameTime);
    }

    public virtual void Draw(SpriteBatch spriteBatch) =>
        spriteBatch.Draw(Sprite,
                        Position,
                        MathHelper.ToRadians(Rotation),
                        new Vector2(Scale));

}