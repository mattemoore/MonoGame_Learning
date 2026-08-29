using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class CollisionWorldFactoryTests
{
    private const int EntitySize = 50;
    private static RectangleF Bounds => new(0, 0, 2000, 2000);

    private static TestActorEntity MakeActor(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    private static TestProp MakeProp(float x, float y) =>
        new("prop", new Vector2(x, y), EntitySize, EntitySize);

    [Test]
    public void Create_ActorsAndPropsInSeparateLayers_OnlyCrossLayerCollisionsReported()
    {
        var world = CollisionWorldFactory.Create(Bounds);
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(prop, CollisionLayers.Props);

        world.RebuildDynamicLayers();

        var crossPairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props).ToList();
        Assert.That(crossPairs, Has.Count.EqualTo(1));

        var actorPairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Actors).ToList();
        var propPairs = world.QueryCollisionPairs(CollisionLayers.Props, CollisionLayers.Props).ToList();
        var pickupPairs = world.QueryCollisionPairs(CollisionLayers.Pickups, CollisionLayers.Pickups).ToList();
        Assert.That(actorPairs, Is.Empty);
        Assert.That(propPairs, Is.Empty);
        Assert.That(pickupPairs, Is.Empty);
    }

    [Test]
    public void Create_IncludesPickupLayer_CollidesWithActors()
    {
        var world = CollisionWorldFactory.Create(Bounds);
        var actor = MakeActor(100, 100);
        var pickup = new TestPickupActor("pickup", new Vector2(100, 100), EntitySize, EntitySize);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(pickup, CollisionLayers.Pickups);

        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Pickups).ToList();
        Assert.That(pairs, Has.Count.EqualTo(1));
    }

    [Test]
    public void ResolveActorPropCollisions_ActorOverlappingProp_IsPushedOut()
    {
        var world = CollisionWorldFactory.Create(Bounds);
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(prop, CollisionLayers.Props);

        CollisionWorldFactory.ResolveActorPropCollisions(world);

        Assert.That(actor.Frame.Intersects(prop.Frame), Is.False);
        Assert.That(prop.Position, Is.EqualTo(new Vector2(110, 100)));
    }

    [Test]
    public void ResolveActorPropCollisions_SeparatedEntities_DoesNotMove()
    {
        var world = CollisionWorldFactory.Create(Bounds);
        var actor = MakeActor(100, 100);
        var prop = MakeProp(500, 500);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(prop, CollisionLayers.Props);

        var actorPos = actor.Position;
        var propPos = prop.Position;

        CollisionWorldFactory.ResolveActorPropCollisions(world);

        Assert.That(actor.Position, Is.EqualTo(actorPos));
        Assert.That(prop.Position, Is.EqualTo(propPos));
    }
}