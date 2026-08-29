using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

public class TestProp(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
}

public class PassThroughActor(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
}

[TestFixture]
public class CollisionLayerTests
{
    private const int EntitySize = 50;

    private static RectangleF Bounds => new(0, 0, 2000, 2000);

    private static CollisionWorld2D CreateCollisionWorld() => CollisionWorldFactory.Create(Bounds);

    private static TestActorEntity MakeActor(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    private static TestProp MakeProp(float x, float y) =>
        new("prop", new Vector2(x, y), EntitySize, EntitySize);

    [Test]
    public void ActorActor_SameLayer_PassThrough()
    {
        var world = CreateCollisionWorld();
        var a1 = new PassThroughActor("actor", new Vector2(100, 100), EntitySize, EntitySize);
        var a2 = new PassThroughActor("actor", new Vector2(110, 100), EntitySize, EntitySize);

        world.Insert(a1, CollisionLayers.Actors);
        world.Insert(a2, CollisionLayers.Actors);

        var pos1 = a1.Position;
        var pos2 = a2.Position;

        world.RebuildDynamicLayers();
        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Actors).ToList();

        Assert.That(a1.Position, Is.EqualTo(pos1));
        Assert.That(a2.Position, Is.EqualTo(pos2));
    }

    [Test]
    public void ActorProp_ActorPushedOutOfProp()
    {
        var world = CreateCollisionWorld();
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(prop, CollisionLayers.Props);

        var propPos = prop.Position;

        world.RebuildDynamicLayers();
        foreach (var pair in world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props))
        {
            if (pair.First is Entity entity)
                entity.Position += pair.FirstResult.MinimumTranslationVector;
        }

        Assert.That(actor.Position, Is.Not.EqualTo(new Vector2(100, 100)));
        Assert.That(prop.Position, Is.EqualTo(propPos));
    }

    [Test]
    public void EnemyProp_EnemyBlockedByProp()
    {
        var world = CreateCollisionWorld();
        var enemy = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(enemy, CollisionLayers.Actors);
        world.Insert(prop, CollisionLayers.Props);

        var propPos = prop.Position;

        world.RebuildDynamicLayers();
        foreach (var pair in world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props))
        {
            if (pair.First is Entity entity)
                entity.Position += pair.FirstResult.MinimumTranslationVector;
        }

        Assert.That(enemy.Position, Is.Not.EqualTo(new Vector2(100, 100)));
        Assert.That(prop.Position, Is.EqualTo(propPos));
    }

    [Test]
    public void Prop_Prop_NoMovement()
    {
        var world = CreateCollisionWorld();
        var p1 = MakeProp(100, 100);
        var p2 = MakeProp(110, 100);

        world.Insert(p1, CollisionLayers.Props);
        world.Insert(p2, CollisionLayers.Props);

        var pos1 = p1.Position;
        var pos2 = p2.Position;

        world.RebuildDynamicLayers();
        var pairs = world.QueryCollisionPairs(CollisionLayers.Props, CollisionLayers.Props).ToList();

        Assert.That(p1.Position, Is.EqualTo(pos1));
        Assert.That(p2.Position, Is.EqualTo(pos2));
    }

    [Test]
    public void ActorProp_ActorFullySeparated()
    {
        var world = CreateCollisionWorld();
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(prop, CollisionLayers.Props);

        world.RebuildDynamicLayers();
        foreach (var pair in world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Props))
        {
            if (pair.First is Entity entity)
                entity.Position += pair.FirstResult.MinimumTranslationVector;
        }

        Assert.That(actor.Frame.Intersects(prop.Frame), Is.False);
        Assert.That(prop.Position, Is.EqualTo(new Vector2(110, 100)));
    }
}
