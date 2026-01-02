

using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

public abstract class LogicalEntity : Entity
{
    public LogicalEntity(Vector2 position, float width, float height) : base(position, width, height)
    {
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: Does nothing for now
    }
}