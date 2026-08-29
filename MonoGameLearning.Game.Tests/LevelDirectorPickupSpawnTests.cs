using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Levels;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class LevelDirectorPickupSpawnTests
{
    private static CollisionWorld2D CreateTestWorld() =>
        CollisionWorldFactory.Create(new RectangleF(0, 0, 2000, 600));

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
        var mgr = new EntityService(world);
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
        var mgr = new EntityService(world);
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = CreateTestLevel();
        var director = new TestLevelDirector(mgr, level, player);

        Assert.Throws<ArgumentException>(() =>
            director.SpawnPickups([new PickupSpawnDef("Unknown", Vector2.Zero)]));
    }
}