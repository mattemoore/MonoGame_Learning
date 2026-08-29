using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

public static class TestLevelContent
{
    public static readonly RectangleF CameraView = new(0, 0, 800, 600);

    public static PropBase CreateProp(PropSpawnDef def) =>
        throw new NotSupportedException("SpawnProps is not exercised in tests");

    public static Entity CreatePickup(PickupSpawnDef def) => def.Type switch
    {
        LevelContent.Food => new FoodPickupEntity(def.Type, def.Position, null),
        LevelContent.Bat => new WeaponPickupEntity(def.Type, def.Position, BatWeapon.Bat),
        _ => throw new ArgumentException($"Unknown pickup type: {def.Type}", nameof(def)),
    };

    public static MeleeWeaponDef GetWeapon(string key) => key switch
    {
        LevelContent.Bat => BatWeapon.Bat,
        _ => throw new ArgumentException($"Unknown weapon: {key}", nameof(key)),
    };

    public static EnemyEntity CreateEnemy(string type, int index, Func<WorldSnapshot> getWorld) =>
        new TestEnemyEntity($"test_enemy_{index}", Vector2.Zero);

    public static void OnEnemySpawned(EnemyEntity enemy, EnemySpawnDef def, FacingDirection facing, MeleeWeaponDef? weapon)
    {
        if (weapon is not null)
            enemy.EquipWeapon(weapon);
    }
}

public class TestPlayerEntity(string name, Vector2 position) : Entity(name, position, 10, 10)
{
}

public class TestLevel(List<WaveDef> waveDefs, float endTriggerX, int gameWidth = 800, int gameHeight = 600)
    : Level(waveDefs, gameWidth, gameHeight)
{
    public override int BackgroundCount => 3;
    public override float EndTriggerX { get; } = endTriggerX;
    public override float WalkableTopY => 0f;
    public override List<PropSpawnDef> Props => [];
    public override List<PickupSpawnDef> Pickups => [];
    public override BackgroundRenderer CreateBackgroundRenderer(ContentManager content) => null!;
}

#pragma warning disable CS9107 // Captured by base class — needed for InitializePool() called from base ctor
public class TestLevelDirector(EntityService entityManager, Level level, Entity player)
    : LevelDirector(entityManager, level, player, null!,
        TestLevelContent.CreateProp,
        TestLevelContent.CreatePickup,
        TestLevelContent.GetWeapon,
        TestLevelContent.CreateEnemy,
        TestLevelContent.OnEnemySpawned,
        () => TestLevelContent.CameraView)
#pragma warning restore CS9107
{
    public List<Entity> SpawnedEnemies { get; } = [];

    protected override void InitializePool()
    {
        EnemyPool = new TestEnemyPool(entityManager, SpawnedEnemies);
        EnemyPool.Build(level);
    }

    public void SimulateEnemyDied(EnemyEntity enemy)
    {
        OnEnemyDied(enemy);
    }

    public void SimulatePropDestroyed(PropBase prop)
    {
        OnPropDestroyed(prop);
    }
}

public class TestEnemyPool(EntityService entityManager, List<Entity> spawnedEnemies)
    : EnemyPool(entityManager, () => default, (type, i, getWorld) => new TestEnemyEntity($"test_enemy_{i}", Vector2.Zero))
{
    public override EnemyEntity Rent(string type, Vector2 position, Entity target)
    {
        var enemy = base.Rent(type, position, target);
        spawnedEnemies.Add(enemy);
        return enemy;
    }
}

#pragma warning disable CS9107 // Primary constructor params flow only to the base constructor
public class CapturingHookLevelDirector(EntityService entityManager, Level level, Entity player,
    List<(EnemyEntity Enemy, FacingDirection Facing, MeleeWeaponDef? Weapon)> captured)
    : LevelDirector(entityManager, level, player, null!,
        TestLevelContent.CreateProp,
        TestLevelContent.CreatePickup,
        TestLevelContent.GetWeapon,
        TestLevelContent.CreateEnemy,
        (enemy, def, facing, weapon) => captured.Add((enemy, facing, weapon)),
        () => TestLevelContent.CameraView)
{
}
#pragma warning restore CS9107

[TestFixture]
public class LevelDirectorTests
{
    private static readonly RectangleF Bounds = new(0, 0, 2000, 600);
    private EntityService _entityManager;
    private TestLevel _level;
    private Entity _player;
    private TestLevelDirector _director;

    private static CollisionWorld2D CreateTestWorld() => CollisionWorldFactory.Create(Bounds);

    [SetUp]
    public void Setup()
    {
        _entityManager = new EntityService(CreateTestWorld());
        _player = new TestPlayerEntity("player", Vector2.Zero);
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

        _director = new TestLevelDirector(_entityManager, _level, _player);
    }

    [Test]
    public void InitialState_NoWaveActive_NotLocked()
    {
        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.ShowGoPrompt, Is.False);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(0));
        Assert.That(_director.WaveEndX, Is.Null);
        Assert.That(_director.WaveTriggerX, Is.Null);
    }

    [Test]
    public void Update_BeforeTrigger_DoesNotSpawnWave()
    {
        _player.Position = new Vector2(100, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.WaveEndX, Is.Null);
    }

    [Test]
    public void Update_AtTrigger_SpawnsWaveAndLocksScroll()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.True);
        Assert.That(_director.ShowGoPrompt, Is.False);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(2));
        Assert.That(_director.WaveTriggerX, Is.EqualTo(300f));
        Assert.That(_director.WaveEndX, Is.EqualTo(1100f));
    }

    [Test]
    public void Update_AtTriggerTwice_DoesNotDoubleSpawn()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(2));

        _player.Position = new Vector2(500, 0);
        _director.Update(new GameTime());

        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(2));
    }

    [Test]
    public void Update_BacktrackPastTrigger_DoesNotSpawnAgain()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        _player.Position = new Vector2(100, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.True);
    }

    [Test]
    public void Update_AllEnemiesDead_ClearsWaveAndUnlocksScroll()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }

        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(1));
        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.ShowGoPrompt, Is.True);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(0));
        Assert.That(_director.WaveEndX, Is.Null);
        Assert.That(_director.WaveTriggerX, Is.Null);
    }

    [Test]
    public void Update_WaveCleared_PlayerMovesToNextTrigger_ProgressesToSecondWave()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(1));
        Assert.That(_director.IsScrollLocked, Is.True);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(1));
        Assert.That(_director.WaveTriggerX, Is.EqualTo(900f));
        Assert.That(_director.WaveEndX, Is.EqualTo(1700f));
    }

    [Test]
    public void Update_AllWavesDone_PlayerAtEndTrigger_FiresLevelCompleted()
    {
        bool completed = false;
        _director.LevelCompleted += () => completed = true;

        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1500, 0);
        _director.Update(new GameTime());

        Assert.That(completed, Is.True);
    }

    [Test]
    public void Update_AllWavesDone_BeforeEndTrigger_DoesNotFireLevelCompleted()
    {
        bool completed = false;
        _director.LevelCompleted += () => completed = true;

        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1400, 0);
        _director.Update(new GameTime());

        Assert.That(completed, Is.False);
    }

    [Test]
    public void Update_EnemyOutsideWave_DoesNotAffectCurrentWave()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        var spawned = _director.SpawnedEnemies.ToList();
        var extraEnemy = new TestEnemyEntity("outside", Vector2.Zero);
        _director.SimulateEnemyDied(extraEnemy);

        _director.Update(new GameTime());

        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(2));
        Assert.That(_director.IsScrollLocked, Is.True);
    }

    [Test]
    public void ResetViaNewDirector_ClearsPreviousState()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        var newDirector = new TestLevelDirector(_entityManager, _level, _player);
        Assert.That(newDirector.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(newDirector.IsScrollLocked, Is.False);
        Assert.That(newDirector.ShowGoPrompt, Is.False);
        Assert.That(newDirector.ActiveEnemyCount, Is.EqualTo(0));
    }

    [Test]
    public void ShowGoPrompt_FalseBeforeAnyWave()
    {
        Assert.That(_director.ShowGoPrompt, Is.False);
    }

    [Test]
    public void ShowGoPrompt_FalseDuringActiveWave()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.ShowGoPrompt, Is.False);
    }

    [Test]
    public void ShowGoPrompt_TrueAfterWaveCleared()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.ShowGoPrompt, Is.True);
    }

    [Test]
    public void WaveEndX_NullBeforeTrigger()
    {
        Assert.That(_director.WaveEndX, Is.Null);
    }

    [Test]
    public void WaveEndX_SetOnTrigger()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.WaveEndX, Is.EqualTo(1100f));
    }

    [Test]
    public void WaveEndX_ClearedOnWaveClear()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.WaveEndX, Is.EqualTo(1100f));

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.WaveEndX, Is.Null);
    }

    [Test]
    public void WaveEndX_UpdatesToNewWave()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.WaveEndX, Is.EqualTo(1100f));

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());

        Assert.That(_director.WaveEndX, Is.EqualTo(1700f));
    }

    [Test]
    public void WaveEndX_ClearedOnReset()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.WaveEndX, Is.EqualTo(1100f));

        var newDirector = new TestLevelDirector(_entityManager, _level, _player);
        Assert.That(newDirector.WaveEndX, Is.Null);
    }

    [Test]
    public void EnemiesRegistered_WithEntityManager()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_entityManager.All.Count(e => e is EnemyEntity), Is.EqualTo(2));
    }

    [Test]
    public void DiedEvent_FiresTypedHandler_RemovesEnemy()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        var enemy = (TestEnemyEntity)_director.SpawnedEnemies[0];
        enemy.TakeDamage(new DamageInfo { Amount = 9999 });
        enemy.StateController!.Fire(EnemyTrigger.DeathCompleted);
        _entityManager.ProcessPending();

        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(1));
        Assert.That(_entityManager.All.Count(e => e is EnemyEntity), Is.EqualTo(1));
    }

    [Test]
    public void DiedEvent_UnsubscribesAfterDeath_SecondDeathTriggerIsNoOp()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        var enemy = (TestEnemyEntity)_director.SpawnedEnemies[0];
        enemy.TakeDamage(new DamageInfo { Amount = 9999 });
        enemy.StateController!.Fire(EnemyTrigger.DeathCompleted);
        _entityManager.ProcessPending();

        Assert.DoesNotThrow(() => enemy.TakeDamage(new DamageInfo { Amount = 9999 }));
        Assert.DoesNotThrow(() => enemy.StateController!.Fire(EnemyTrigger.DeathCompleted));
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(1));
        Assert.That(_entityManager.All.Count(e => e is EnemyEntity), Is.EqualTo(1));
    }

    [Test]
    public void OnEnemyDied_RemovesFromEntityManager()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        _entityManager.ProcessPending();

        Assert.That(_entityManager.All.Count(e => e is EnemyEntity), Is.EqualTo(2));

        foreach (var entity in _director.SpawnedEnemies.ToList())
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
            Assert.That(enemy.Position, Is.EqualTo(new Vector2(-99999, -99999)),
                "Enemy position must be sentinel after death — prevents off-screen render.");
        }
        _entityManager.ProcessPending();

        Assert.That(_entityManager.All.Count(e => e is EnemyEntity), Is.EqualTo(0));
    }

    [Test]
    public void FullFlow_TriggerWave_KillEnemies_AdvanceToNext()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.IsScrollLocked, Is.True);
        Assert.That(_director.WaveEndX, Is.EqualTo(1100f));

        foreach (var entity in _director.SpawnedEnemies.ToList())
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.WaveEndX, Is.Null);

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(1));
        Assert.That(_director.IsScrollLocked, Is.True);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(1));

        foreach (var entity in _director.SpawnedEnemies.ToList())
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        bool completed = false;
        _director.LevelCompleted += () => completed = true;

        _player.Position = new Vector2(1500, 0);
        _director.Update(new GameTime());

        Assert.That(completed, Is.True);
    }

    [Test]
    public void SpawnWave_PositionsEnemyByItsOwnDimensions()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        var enemy = (EnemyEntity)_director.SpawnedEnemies[0]; // first wave def is SpawnSide.Left
        float halfW = enemy.Width * 0.5f;
        float expectedX = TestLevelContent.CameraView.X - halfW - 100f;
        Assert.That(enemy.Position.X, Is.EqualTo(expectedX));
        Assert.That(enemy.Position.Y, Is.EqualTo(300f)); // SpawnVertical.Middle → midline of walkable bounds (0..600)
    }

    [Test]
    public void SpawnWave_InvokesSpawnedHook_WithInitialFacingAndResolvedWeapon()
    {
        var captured = new List<(EnemyEntity Enemy, FacingDirection Facing, MeleeWeaponDef? Weapon)>();
        var level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
                new EnemySpawnDef("Grunt", SpawnSide.Right, SpawnVertical.Middle, Weapon: "Bat"),
            ])
        ], endTriggerX: 1500f);
        var director = new CapturingHookLevelDirector(_entityManager, level, _player, captured);

        _player.Position = new Vector2(300, 0);
        director.Update(new GameTime());

        Assert.That(captured, Has.Count.EqualTo(2));
        Assert.That(captured[0].Facing, Is.EqualTo(FacingDirection.Right), "Left-edge spawn must face right.");
        Assert.That(captured[1].Facing, Is.EqualTo(FacingDirection.Left), "Right-edge spawn must face left.");
        Assert.That(captured[1].Weapon, Is.SameAs(BatWeapon.Bat), "Weapon name must resolve through getWeapon.");
    }

    [Test]
    [Ignore("SpriteBatch is a sealed MonoGame type — requires a real GraphicsDevice or a mock wrapper. " +
        "The test documents the intent: DrawDebug should not throw for state-based logic errors " +
        "(as opposed to null SpriteBatch NRE which is expected).")]
    public void DrawDebug_DoesNotThrow_WhenGameNotStarted()
    {
        // Initial state: _currentWaveIndex == 0, not triggered, not scroll-locked.
        // DrawDebug will attempt DrawLine calls on the null SpriteBatch and throw NRE,
        // but that's expected — the test is for state-logic exceptions only.
        var ctx = new DebugDrawContext(null!, null!);
        Assert.DoesNotThrow(() => _director.DrawDebug(ctx));
    }

    [Test]
    [Ignore("SpriteBatch is a sealed MonoGame type — requires a real GraphicsDevice or a mock wrapper. " +
        "The test documents the intent: DrawDebug should not throw for state-based logic errors " +
        "(as opposed to null SpriteBatch NRE which is expected).")]
    public void DrawDebug_DoesNotThrow_WhenScrollLocked()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.IsScrollLocked, Is.True);

        var ctx = new DebugDrawContext(null!, null!);
        Assert.DoesNotThrow(() => _director.DrawDebug(ctx));
    }

    [Test]
    [Ignore("SpriteBatch is a sealed MonoGame type — requires a real GraphicsDevice or a mock wrapper. " +
        "The test documents the intent: DrawDebug should not throw for state-based logic errors " +
        "(as opposed to null SpriteBatch NRE which is expected).")]
    public void DrawDebug_DoesNotThrow_AfterAllWavesComplete()
    {
        // Drive both waves to completion
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        foreach (var entity in _director.SpawnedEnemies.ToList())
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());
        foreach (var entity in _director.SpawnedEnemies.ToList())
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        // Now _currentWaveIndex >= waves.Count — all waves done
        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(2));

        var ctx = new DebugDrawContext(null!, null!);
        Assert.DoesNotThrow(() => _director.DrawDebug(ctx));
    }
}
