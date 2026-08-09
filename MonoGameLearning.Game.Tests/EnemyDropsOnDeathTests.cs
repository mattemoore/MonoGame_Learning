using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Pickups;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class EnemyDropsOnDeathTests
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

    private static (TestLevelDirector Director, EntityService Manager, Entity Player) Setup(List<EnemySpawnDef> enemies)
    {
        var world = CreateTestWorld();
        var mgr = new EntityService(world);
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies: enemies)
        ], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);
        player.Position = new Vector2(300, 0);
        director.Update(new GameTime());
        return (director, mgr, player);
    }

    [Test]
    public void SpawnWave_EnemyDefWithDrops_AssignsDropsToRentedEnemy()
    {
        var (director, _, _) = Setup(
        [
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle, Drops:
            [
                new PickupSpawnDef("Food", default),
            ]),
        ]);

        var enemy = (EnemyEntity)director.SpawnedEnemies[0];
        Assert.That(enemy.Drops, Is.Not.Null);
        Assert.That(enemy.Drops!.Count, Is.EqualTo(1));
    }

    [Test]
    public void OnEnemyDied_WithDrops_SpawnsFoodAtEnemyFeet()
    {
        var (director, mgr, _) = Setup(
        [
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle, Drops:
            [
                new PickupSpawnDef("Food", default),
            ]),
        ]);

        var enemy = (EnemyEntity)director.SpawnedEnemies[0];
        var frame = enemy.Frame;
        director.SimulateEnemyDied(enemy);

        var food = mgr.All.OfType<FoodPickupEntity>().Single();
        Assert.That(food.Position.X, Is.EqualTo(frame.Center.X));
        Assert.That(food.Frame.Bottom, Is.EqualTo(frame.Bottom));
    }

    [Test]
    public void OnEnemyDied_WithoutDrops_SpawnsNothing()
    {
        var (director, mgr, _) = Setup(
        [
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
        ]);

        var enemy = (EnemyEntity)director.SpawnedEnemies[0];
        director.SimulateEnemyDied(enemy);

        Assert.That(mgr.All.OfType<FoodPickupEntity>(), Is.Empty);
    }

    [Test]
    public void Reset_ClearsDrops_PreventingStalePoolDrops()
    {
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var enemy = new TestEnemyEntity("enemy", Vector2.Zero);
        enemy.Drops = [new PickupSpawnDef("Food", default)];

        enemy.Reset(Vector2.Zero, player);

        Assert.That(enemy.Drops, Is.Null);
    }
}