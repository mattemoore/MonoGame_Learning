using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities;

public abstract class ActorEntity : LogicalEntity
{
    public AnimatedSprite Sprite { get; private set; }

    protected ActorEntity(Vector2 position,
                          float width,
                          float height,
                          AnimatedSprite sprite) : base(position, width, height)
    {
        Sprite = sprite;
    }

    public override void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
        base.Update(gameTime);
    }
}