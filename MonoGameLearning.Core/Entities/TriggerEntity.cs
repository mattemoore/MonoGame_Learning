using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace MonoGameLearning.Core.Entities;

/// <summary>
/// SKELETON: Collision-triggered zone with overlap detection but no push-apart.
///
/// Intentionally does NOT declare ICollisionActor — consumer subclasses should
/// add the interface and implement custom overlap logic (e.g., events on enter/exit).
/// ICollisionActor is omitted here so the skeleton stays minimal and forces explicit
/// opt-in when adding collision registration.
///
/// Usage:
///   public class MyTriggerZone(...) : TriggerEntity(...), ICollisionActor
/// </summary>
public class TriggerEntity(string name, Vector2 position, int width, int height)
    : SpatialEntity(name, position, width, height)
{
    public IShapeF Bounds => Frame;

    public void OnCollision(CollisionEventArgs collisionInfo)
    {
        // Empty — detect but don't push
    }
}
