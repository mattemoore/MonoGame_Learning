using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class EnemyPoolTests
{
    private static readonly RectangleF Bounds = new(0, 0, 2000, 600);
    private EntityService _entityManager;
    private Entity _player;
    private Level _level;

    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(Bounds.X, Bounds.Y), new Vector2(Bounds.Right, Bounds.Bottom));
        world.AddLayer(CollisionLayers.Actors, new Layer(new QuadTreeSpace(bb)));
        world.AddLayer(CollisionLayers.Props, new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Props);
        return world;
    }

    [SetUp]
    public void Setup()
    {
        _entityManager = new EntityService(CreateTestWorld());
        _player = new EntityStub("player", Vector2.Zero, 10, 10);

        _level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
            ]),
            new WaveDef(TriggerX: 900f, EndX: 1700f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
            ])
        ], endTriggerX: 1500f);
    }

    [Test]
    public void Rent_EmptyPoolForType_Throws()
    {
        var pool = new EnemyPool(_entityManager, () => default, MockFactory);
        pool.Build(_level);

        Assert.That(() => pool.Rent("UnknownType", Vector2.Zero, _player),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Rent_ReturnsAndRegistersInstance()
    {
        var pool = new TestEnemyPool(_entityManager);
        pool.Build(_level);

        var pos = new Vector2(500, 300);
        var enemy = pool.Rent("Grunt", pos, _player);

        Assert.That(enemy.Position, Is.EqualTo(pos));
        Assert.That(_entityManager.All, Does.Contain(enemy));
    }

    [Test]
    public void Return_SetsPositionToSentinel()
    {
        var pool = new TestEnemyPool(_entityManager);
        pool.Build(_level);

        var pos = new Vector2(500, 300);
        var enemy = pool.Rent("Grunt", pos, _player);
        Assert.That(enemy.Position, Is.EqualTo(pos));

        pool.Return(enemy);
        var sentinel = new Vector2(-99999, -99999);
        Assert.That(enemy.Position, Is.EqualTo(sentinel));
        _entityManager.ProcessPending();
        Assert.That(enemy.Position, Is.EqualTo(sentinel), "Sentinel must survive ProcessPending — no ClampToBounds should move it.");
    }

    [Test]
    public void Return_ThenRent_GivesBackSameInstance()
    {
        var pool = new TestEnemyPool(_entityManager);
        pool.Build(_level);

        var enemy = pool.Rent("Grunt", new Vector2(500, 300), _player);
        var firstId = enemy.GetHashCode();

        pool.Return(enemy);
        _entityManager.ProcessPending();

        var enemy2 = pool.Rent("Grunt", new Vector2(600, 400), _player);
        Assert.That(enemy2.GetHashCode(), Is.EqualTo(firstId));
    }

    [Test]
    public void Build_PassesInjectedWorldGetterToFactory()
    {
        var captured = new List<Func<WorldSnapshot>>();
        var pool = new EnemyPool(_entityManager, () => default, (type, index, getWorld) =>
        {
            captured.Add(getWorld);
            return new TestEnemyEntity($"test_enemy_{index}", Vector2.Zero);
        });

        pool.Build(_level);

        Assert.That(captured, Is.Not.Empty);
        foreach (var getWorld in captured)
            Assert.That(getWorld(), Is.EqualTo(default(WorldSnapshot)));
    }

    [Test]
    public void GenericCorePool_UsesRentReturnHooks_AndStaysTypeAgnostic()
    {
        var pool = new GenericHookPool(_entityManager);
        pool.Build(_level);

        var entity = pool.Rent("Grunt", new Vector2(500, 300), _player);
        Assert.That(pool.RentCalls, Is.EqualTo(1));
        Assert.That(entity.Position, Is.EqualTo(new Vector2(500, 300)), "Rent hook positions the entity.");
        Assert.That(_entityManager.All, Does.Contain(entity));

        pool.Return(entity);
        Assert.That(pool.ReturnCalls, Is.EqualTo(1));
        Assert.That(entity.Position, Is.EqualTo(new Vector2(-99999, -99999)), "Return parks the entity at the sentinel.");
    }

    private static int _mockCounter;

    private static EnemyEntity MockFactory(string type, int index, Func<WorldSnapshot> getWorld)
    {
        _mockCounter++;
        return new TestEnemyEntity($"test_enemy_{_mockCounter}", Vector2.Zero);
    }

    private class TestEnemyPool(EntityService entityManager)
        : EnemyPool(entityManager, () => default, (type, index, getWorld) =>
        {
            _mockCounter++;
            return new TestEnemyEntity($"test_enemy_{_mockCounter}", Vector2.Zero);
        })
    {
    }

    private class GenericHookPool(EntityService entityManager)
        : EntityPool<EntityStub>(entityManager, () => default, (type, index, getWorld) =>
            new EntityStub($"generic_{index}", Vector2.Zero, 10, 10))
    {
        public int RentCalls;
        public int ReturnCalls;

        protected override void OnRentEnemy(EntityStub enemy, Vector2 position, Entity target)
        {
            RentCalls++;
            enemy.Position = position;
        }

        protected override void OnReturnEnemy(EntityStub enemy) => ReturnCalls++;
    }

    private class EntityStub(string name, Vector2 position, int width, int height)
        : Entity(name, position, width, height)
    {
    }
}