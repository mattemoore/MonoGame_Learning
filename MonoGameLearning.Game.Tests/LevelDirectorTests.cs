using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.GameLoop;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.Rendering;

namespace MonoGameLearning.Game.Tests;

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
    public override BackgroundRenderer CreateBackgroundRenderer(ContentManager content) => null!;
}

public class TestLevelDirector(EntityManager entityManager, Level level, Entity player, int gameWidth, int gameHeight)
    : LevelDirector(entityManager, level, player, gameWidth, gameHeight)
{
    public List<Entity> SpawnedEnemies { get; } = [];

    protected override EnemyEntity CreateEnemy(EnemySpawnDef def)
    {
        #pragma warning disable SYSLIB0050
        var enemy = (EnemyEntity)FormatterServices.GetUninitializedObject(typeof(EnemyEntity));
#pragma warning restore SYSLIB0050
        enemy.Position = def.Position;
        SpawnedEnemies.Add(enemy);
        return enemy;
    }

    protected override void OnEnemyDied(object sender, EventArgs e)
    {
        if (sender is not EnemyEntity enemy) return;
        base.OnEnemyDied(sender, e);
    }

    public void SimulateEnemyDied(EnemyEntity enemy)
    {
        OnEnemyDied(enemy, EventArgs.Empty);
    }
}

[TestFixture]
public class LevelDirectorTests
{
    private static readonly RectangleF Bounds = new(0, 0, 2000, 600);
    private EntityManager _entityManager;
    private CameraController _cameraController;
    private TestLevel _level;
    private Entity _player;
    private TestLevelDirector _director;

    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(Bounds.X, Bounds.Y), new Vector2(Bounds.Right, Bounds.Bottom));
        var actorSpace = new QuadTreeSpace(bb);
        world.AddLayer("actors", new Layer(actorSpace));
        var propSpace = new QuadTreeSpace(bb);
        world.AddLayer("props", new Layer(propSpace));
        world.EnableCollisionBetweenLayers("actors", "props");
        return world;
    }

    [SetUp]
    public void Setup()
    {
        var world = CreateTestWorld();
        _entityManager = new EntityManager(world);
        _cameraController = new CameraController(null!, 800, 600, new RectangleF(0, 0, 2000, 600));
        _player = new TestPlayerEntity("player", Vector2.Zero);
        _level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", new Vector2(350, 500)),
                new EnemySpawnDef("Grunt", new Vector2(400, 500))
            ]),
            new WaveDef(TriggerX: 900f, EndX: 1700f, Enemies:
            [
                new EnemySpawnDef("Grunt", new Vector2(950, 500))
            ])
        ], endTriggerX: 1500f);

        _director = new TestLevelDirector(_entityManager, _level, _player, 800, 600);
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
        #pragma warning disable SYSLIB0050
        var extraEnemy = (EnemyEntity)FormatterServices.GetUninitializedObject(typeof(EnemyEntity));
#pragma warning restore SYSLIB0050
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

        var newDirector = new TestLevelDirector(_entityManager, _level, _player, 800, 600);
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

        var newDirector = new TestLevelDirector(_entityManager, _level, _player, 800, 600);
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
}