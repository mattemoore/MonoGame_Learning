using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class PropEntity : SpatialEntity, ICollisionActor, IDamageable
{
    public IShapeF Bounds => Frame;

    protected PropEntity(string name, Vector2 position, int width, int height, float rotation = 0f)
        : base(name, position, width, height, rotation) { }

    public virtual void OnCollision(CollisionEventArgs collisionInfo)
    {
        Position -= collisionInfo.PenetrationVector;
    }

    public virtual void TakeDamage(int amount, bool knockdown = false) { }

    public virtual void Draw(SpriteBatch spriteBatch) { }
}