using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class EnemyPoolTests
{
    private static readonly RectangleF Bounds = new(0, 0, 2000, 600);
    private EntityManager _entityManager;
    private Entity _player;
    private Level _level;

    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(Bounds.X, Bounds.Y), new Vector2(Bounds.Right, Bounds.Bottom));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers("actors", "props");
        return world;
    }

    [SetUp]
    public void Setup()
    {
        _entityManager = new EntityManager(CreateTestWorld());
        _player = new EntityStub("player", Vector2.Zero, 10, 10);

        _level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", new Vector2(350, 500)),
                new EnemySpawnDef("Grunt", new Vector2(400, 500)),
            ]),
            new WaveDef(TriggerX: 900f, EndX: 1700f, Enemies:
            [
                new EnemySpawnDef("Grunt", new Vector2(950, 500)),
            ])
        ], endTriggerX: 1500f);
    }

    [Test]
    public void Rent_EmptyPoolForType_Throws()
    {
        var director = new DirectorStub(_entityManager, _level, _player);
        var pool = new EnemyPool(_entityManager, director, MockFactory);
        pool.Build(_level);

        Assert.That(() => pool.Rent("UnknownType", Vector2.Zero, _player),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Rent_ReturnsAndRegistersInstance()
    {
        var director = new DirectorStub(_entityManager, _level, _player);
        var pool = new TestEnemyPool(_entityManager, director);
        pool.Build(_level);

        var pos = new Vector2(500, 300);
        var enemy = pool.Rent("Grunt", pos, _player);

        Assert.That(enemy.Position, Is.EqualTo(pos));
        Assert.That(_entityManager.All, Does.Contain(enemy));
    }

    [Test]
    public void Return_SetsPositionToSentinel()
    {
        var director = new DirectorStub(_entityManager, _level, _player);
        var pool = new TestEnemyPool(_entityManager, director);
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
        var director = new DirectorStub(_entityManager, _level, _player);
        var pool = new TestEnemyPool(_entityManager, director);
        pool.Build(_level);

        var enemy = pool.Rent("Grunt", new Vector2(500, 300), _player);
        var firstId = enemy.GetHashCode();

        pool.Return(enemy);
        _entityManager.ProcessPending();

        var enemy2 = pool.Rent("Grunt", new Vector2(600, 400), _player);
        Assert.That(enemy2.GetHashCode(), Is.EqualTo(firstId));
    }

    private static int _mockCounter;

    private static EnemyEntity MockFactory(string type, int index)
    {
        _mockCounter++;
#pragma warning disable SYSLIB0050
        var enemy = (EnemyEntity)FormatterServices.GetUninitializedObject(typeof(EnemyEntity));
#pragma warning restore SYSLIB0050
        return enemy;
    }

    /// <summary>
    /// Test pool that overrides OnRentEnemy to skip Reset (which NPEs on mock enemies with null Sprite,
    /// _stateController, and _ai — all private readonly fields that FormatterServices can't initialize).
    /// Sets Position and Target directly instead. The full Reset() path is exercised only in integration
    /// tests with a real GraphicsDevice that can construct AnimatedSprite instances.
    /// </summary>
    private class TestEnemyPool(EntityManager entityManager, LevelDirector director)
        : EnemyPool(entityManager, director, MockFactory)
    {
        protected override void OnRentEnemy(EnemyEntity enemy, Vector2 position, Entity target)
        {
            enemy.Position = position;
        }
    }

    private class EntityStub(string name, Vector2 position, int width, int height)
        : Entity(name, position, width, height)
    {
    }

    private class DirectorStub(EntityManager entityManager, Level level, Entity player)
        : LevelDirector(entityManager, level, player)
    {
        protected override void InitializePool()
        {
            // No-op — these tests create and manage the pool directly without going through the director.
        }
    }
}