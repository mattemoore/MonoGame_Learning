using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace MonoGameLearning.Core.Entities;

public class TriggerEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
}
