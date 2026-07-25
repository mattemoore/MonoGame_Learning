using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Levels;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class LevelDirectorPickupSpawnTests
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
    public void SpawnPickups_Stub_RegistersInEntityManager()
    {
        var world = CreateTestWorld();
        var mgr = new EntityManager(world);
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = CreateTestLevel();
        var pickup = new StubPickupEntity("Food", new Vector2(500, 300), 32, 32);

        mgr.Register(pickup);

        Assert.That(mgr.All, Does.Contain(pickup));
        Assert.That(mgr.PickupCollidables, Does.Contain(pickup));
    }

    [Test]
    public void SpawnPickups_UnknownType_Throws()
    {
        var world = CreateTestWorld();
        var mgr = new EntityManager(world);
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = CreateTestLevel();
        var director = new TestLevelDirector(mgr, level, player);

        Assert.Throws<ArgumentException>(() =>
            director.SpawnPickups([new PickupSpawnDef("Unknown", Vector2.Zero)]));
    }
}