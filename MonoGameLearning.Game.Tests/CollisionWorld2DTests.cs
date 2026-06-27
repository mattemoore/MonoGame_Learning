using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class CollisionWorld2DTests
{
    private const int EntitySize = 50;
    private static RectangleF Bounds => new(0, 0, 2000, 2000);

    private static CollisionWorld2D CreateWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(Bounds.X, Bounds.Y), new Vector2(Bounds.Right, Bounds.Bottom));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers("actors", "actors");
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers("props", "props");
        world.EnableCollisionBetweenLayers("actors", "props");
        return world;
    }

    private static TestActorEntity MakeActor(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    private static TestProp MakeProp(float x, float y) =>
        new("prop", new Vector2(x, y), EntitySize, EntitySize);

    [Test]
    public void LayerFiltering_ActorsAndPropsInSeparateLayers_OnlyCrossLayerCollisionsReported()
    {
        var world = CreateWorld();
        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(actor, "actors");
        world.Insert(prop, "props");

        world.RebuildDynamicLayers();

        // Cross-layer query should find the collision
        var crossPairs = world.QueryCollisionPairs("actors", "props").ToList();
        Assert.That(crossPairs, Has.Count.EqualTo(1));
        Assert.That(crossPairs[0].First, Is.EqualTo(actor));
        Assert.That(crossPairs[0].Second, Is.EqualTo(prop));

        // Same-layer queries should be empty (self-collision disabled)
        var actorPairs = world.QueryCollisionPairs("actors", "actors").ToList();
        var propPairs = world.QueryCollisionPairs("props", "props").ToList();
        Assert.That(actorPairs, Is.Empty);
        Assert.That(propPairs, Is.Empty);
    }

    [Test]
    public void LayerFiltering_SeparatedEntities_NoCollisionsReported()
    {
        var world = CreateWorld();
        var actor = MakeActor(100, 100);
        var prop = MakeProp(500, 500);

        world.Insert(actor, "actors");
        world.Insert(prop, "props");

        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs("actors", "props").ToList();
        Assert.That(pairs, Is.Empty);
    }

    [Test]
    public void CollisionShape2D_OverlappingBoxes_ProducesMinimumTranslationVector()
    {
        // Two overlapping boxes: left box at (0,0)-(50,50), right box at (30,0)-(80,50)
        var leftBox = new BoundingBox2D(new Vector2(0, 0), new Vector2(50, 50));
        var rightBox = new BoundingBox2D(new Vector2(30, 0), new Vector2(80, 50));
        var leftShape = new CollisionShape2D(leftBox);
        var rightShape = new CollisionShape2D(rightBox);

        bool collides = leftShape.TryGetCollision(rightShape, out var result);

        Assert.That(collides, Is.True);
        Assert.That(result.Intersects, Is.True);
        Assert.That(result.MinimumTranslationVector.X, Is.EqualTo(-20f)); // Move left box left by 20
        Assert.That(result.PenetrationDepth, Is.EqualTo(20f)); // 20 units of overlap
    }

    [Test]
    public void CollisionShape2D_NonOverlappingBoxes_NoCollision()
    {
        var leftBox = new BoundingBox2D(new Vector2(0, 0), new Vector2(50, 50));
        var farBox = new BoundingBox2D(new Vector2(100, 0), new Vector2(150, 50));
        var leftShape = new CollisionShape2D(leftBox);
        var farShape = new CollisionShape2D(farBox);

        bool collides = leftShape.TryGetCollision(farShape, out var result);

        Assert.That(collides, Is.False);
        Assert.That(result.Intersects, Is.False);
        Assert.That(result.MinimumTranslationVector, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void CollisionWorld2D_DisableLayerInteraction_SuppressesCollisions()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 2000));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        // Enable self-collision so actors within the same layer detect each other
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers("props", "props");
        // Intentionally NOT enabling actors<->props — they should NOT collide

        var actor = MakeActor(100, 100);
        var prop = MakeProp(110, 100);

        world.Insert(actor, "actors");
        world.Insert(prop, "props");

        world.RebuildDynamicLayers();

        // actors<->props collision is not enabled; query should return nothing
        var pairs = world.QueryCollisionPairs("actors", "props").ToList();
        Assert.That(pairs, Is.Empty);

        // Actors with self-collision disabled at layer creation also report nothing
    }

    [Test]
    public void CollisionWorld2D_MultipleActorsAndProps_OnlyOverlappingCrossLayerPairsReported()
    {
        var world = CreateWorld();
        var actor1 = MakeActor(100, 100); // Overlaps prop1
        var actor2 = MakeActor(500, 100); // Overlaps prop2
        var prop1 = MakeProp(110, 100);   // Overlaps actor1
        var prop2 = MakeProp(600, 500);   // Far from actor2 (no overlap)
        var prop3 = MakeProp(510, 100);   // Overlaps actor2

        world.Insert(actor1, "actors");
        world.Insert(actor2, "actors");
        world.Insert(prop1, "props");
        world.Insert(prop2, "props");
        world.Insert(prop3, "props");

        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs("actors", "props").ToList();

        // actor1-prop1, actor2-prop3 should collide
        // prop2 is far from both actors
        Assert.That(pairs, Has.Count.EqualTo(2));

        var collisions = pairs.Select(p => (p.First, p.Second)).ToHashSet();
        Assert.That(collisions, Does.Contain((actor1, prop1)));
        Assert.That(collisions, Does.Contain((actor2, prop3)));
        Assert.That(collisions, Does.Not.Contain((actor2, prop2)));
    }
}