using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;

namespace MonoGameLearning.Core.Entities;

public static class CollisionWorldFactory
{
    public static CollisionWorld2D Create(RectangleF bounds)
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Bottom));
        var actorSpace = new QuadTreeSpace(bb);
        world.AddLayer(CollisionLayers.Actors, new Layer(actorSpace));
        world.DisableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Actors);
        var propSpace = new QuadTreeSpace(bb);
        world.AddLayer(CollisionLayers.Props, new Layer(propSpace));
        world.DisableCollisionBetweenLayers(CollisionLayers.Props, CollisionLayers.Props);
        world.EnableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Props);
        var pickupSpace = new QuadTreeSpace(bb);
        world.AddLayer(CollisionLayers.Pickups, new Layer(pickupSpace));
        world.DisableCollisionBetweenLayers(CollisionLayers.Pickups, CollisionLayers.Pickups);
        world.EnableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Pickups);
        return world;
    }

    public static void ResolveActorPropCollisions(CollisionWorld2D world)
    {
        world.RebuildDynamicLayers();

        foreach (var pair in world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props))
        {
            var actor = pair.First;
            var result = pair.FirstResult;
            if (!result.Intersects) continue;
            if (actor is not ISpatial positionable) continue;
            positionable.Position += result.MinimumTranslationVector;
        }
    }
}