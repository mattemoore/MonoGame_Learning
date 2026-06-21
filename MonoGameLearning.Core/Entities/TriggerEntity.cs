using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace MonoGameLearning.Core.Entities;

public class TriggerEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor
{
    public IShapeF Bounds => Frame;

    public void OnCollision(CollisionEventArgs collisionInfo)
    {
    }
}