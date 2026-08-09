using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;

namespace MonoGameLearning.Game.Tests;

internal sealed class StubPropDropperEntity(string name, Vector2 position, int width, int height)
    : PropBase(name, position, width, height, maxHealth: 1, CollisionAnchor.Top)
{
    public override void TakeDamage(DamageInfo info) => OnDestroyed();
    public void FireDestroyed() => OnDestroyed();
}

[TestFixture]
public class PropDropsOnDestroyTests
{
    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 600));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("pickups", new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers("actors", "props");
        world.EnableCollisionBetweenLayers("actors", "pickups");
        return world;
    }

    private static TestLevel CreateTestLevel()
    {
        return new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
            ])
        ], endTriggerX: 1500f);
    }

    [Test]
    public void CreateDrops_WhenDropsSet_ReturnsConfiguredList()
    {
        var entity = new StubPropDropperEntity("prop", Vector2.Zero, 64, 64)
        {
            Drops = [new PickupSpawnDef("Food", new Vector2(500, 560))]
        };

        var result = entity.CreateDrops();

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Type, Is.EqualTo("Food"));
        Assert.That(result[0].Position, Is.EqualTo(new Vector2(500, 560)));
    }

    [Test]
    public void CreateDrops_WhenDropsNull_ReturnsEmpty()
    {
        var entity = new StubPropDropperEntity("prop", Vector2.Zero, 64, 64)
        {
            Drops = null
        };

        Assert.That(entity.CreateDrops(), Is.Empty);
    }

    [Test]
    public void CreateDrops_WhenDropsEmpty_ReturnsEmpty()
    {
        var entity = new StubPropDropperEntity("prop", Vector2.Zero, 64, 64)
        {
            Drops = []
        };

        Assert.That(entity.CreateDrops(), Is.Empty);
    }

    [Test]
    public void CreateDrops_WhenDropsNull_DoesNotReturnNull()
    {
        var entity = new StubPropDropperEntity("prop", Vector2.Zero, 64, 64)
        {
            Drops = null
        };

        Assert.That(entity.CreateDrops(), Is.Not.Null);
    }

    [Test]
    public void OnPropDestroyed_WithoutDrops_DoesNotSpawn()
    {
        var world = CreateTestWorld();
        var mgr = new EntityService(world);
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = CreateTestLevel();
        var director = new TestLevelDirector(mgr, level, player);

        var prop = new StubPropDropperEntity("drum", new Vector2(500, 460), 64, 64)
        {
            Drops = null
        };
        mgr.Register(prop);
        int countBefore = mgr.All.Count;

        director.SimulatePropDestroyed(prop);
        mgr.ProcessPending();

        // Prop removed, no drops spawned (SpawnPickups not called)
        Assert.That(mgr.All.Count, Is.EqualTo(countBefore - 1));
    }

    [Test]
    public void OnPropDestroyed_WithEmptyDropsList_DoesNotSpawn()
    {
        var world = CreateTestWorld();
        var mgr = new EntityService(world);
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = CreateTestLevel();
        var director = new TestLevelDirector(mgr, level, player);

        var prop = new StubPropDropperEntity("drum", new Vector2(500, 460), 64, 64)
        {
            Drops = []
        };
        mgr.Register(prop);
        int countBefore = mgr.All.Count;

        director.SimulatePropDestroyed(prop);
        mgr.ProcessPending();

        Assert.That(mgr.All.Count, Is.EqualTo(countBefore - 1));
    }
}