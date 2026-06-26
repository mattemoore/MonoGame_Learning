using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.GameLoop;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Tests;

public class TestPlayerEntity(string name, Vector2 position) : Entity(name, position, 10, 10)
{
}

public class TestLevel(List<WaveDef> waveDefs, float endTriggerX) : Level([], waveDefs, 800f)
{
    public override float EndTriggerX { get; } = endTriggerX;
}

public class TestLevelDirector(EntityManager entityManager, Level level, Entity player)
    : LevelDirector(entityManager, level, player)
{
    public List<Entity> SpawnedEnemies { get; } = [];

    protected override EnemyEntity CreateEnemy(EnemySpawnDef def)
    {
        #pragma warning disable SYSLIB0050 // FormatterServices is obsolete but needed for uninitialized object creation in tests
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

    [SetUp]
    public void Setup()
    {
        var cc = new CollisionComponent(Bounds);
        cc.Add("actors", new Layer(new QuadTreeSpace(Bounds)));
        _entityManager = new EntityManager(cc);
        _cameraController = new CameraController(null!, 800, 600, new RectangleF(0, 0, 2000, 600));
        _player = new TestPlayerEntity("player", Vector2.Zero);
        _level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, FightAreaWidth: 500f, Enemies:
            [
                new EnemySpawnDef("Grunt", new Vector2(350, 500)),
                new EnemySpawnDef("Grunt", new Vector2(400, 500))
            ]),
            new WaveDef(TriggerX: 900f, FightAreaWidth: 400f, Enemies:
            [
                new EnemySpawnDef("Grunt", new Vector2(950, 500))
            ])
        ], endTriggerX: 1500f);

        _director = new TestLevelDirector(_entityManager, _level, _player);
    }

    [Test]
    public void InitialState_NoWaveActive_NotLocked()
    {
        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.IsWaveCleared, Is.False);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(0));
        Assert.That(_director.CurrentFightArea, Is.Null);
        Assert.That(_cameraController.LeftBound, Is.Null);
        Assert.That(_cameraController.RightBound, Is.Null);
    }

    [Test]
    public void Update_BeforeTrigger_DoesNotSpawnWave()
    {
        _player.Position = new Vector2(100, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.CurrentFightArea, Is.Null);
        Assert.That(_cameraController.LeftBound, Is.Null);
        Assert.That(_cameraController.RightBound, Is.Null);
    }

    [Test]
    public void Update_AtTrigger_SpawnsWaveAndLocksScroll()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(_director.IsScrollLocked, Is.True);
        Assert.That(_director.IsWaveCleared, Is.False);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(2));
        Assert.That(_director.CurrentFightArea, Is.Not.Null);
        Assert.That(_director.CurrentFightArea.Value.X, Is.EqualTo(50f));
        Assert.That(_director.CurrentFightArea.Value.Width, Is.EqualTo(500f));
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
        Assert.That(_director.IsWaveCleared, Is.True);
        Assert.That(_director.ActiveEnemyCount, Is.EqualTo(0));
        Assert.That(_director.CurrentFightArea, Is.Null);
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
        Assert.That(_director.CurrentFightArea, Is.Not.Null);
        Assert.That(_director.CurrentFightArea.Value.X, Is.EqualTo(700f));
        Assert.That(_director.CurrentFightArea.Value.Width, Is.EqualTo(400f));
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

        var newDirector = new TestLevelDirector(_entityManager, _level, _player);
        Assert.That(newDirector.CurrentWaveIndex, Is.EqualTo(0));
        Assert.That(newDirector.IsScrollLocked, Is.False);
        Assert.That(newDirector.IsWaveCleared, Is.False);
        Assert.That(newDirector.ActiveEnemyCount, Is.EqualTo(0));
    }

    [Test]
    public void WaveCleared_IsFalse_WhenWaveActive()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.IsWaveCleared, Is.False);
    }

    [Test]
    public void WaveCleared_IsTrue_AfterEnemiesDead()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.IsWaveCleared, Is.True);
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
    public void PersistentCameraCenter_NullBeforeTrigger()
    {
        Assert.That(_director.PersistentCameraCenter, Is.Null);
    }

    [Test]
    public void PersistentCameraCenter_SetOnTrigger()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.PersistentCameraCenter, Is.EqualTo(300f));
    }

    [Test]
    public void PersistentCameraCenter_SurvivesWaveClear()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.CurrentFightArea, Is.Null);
        Assert.That(_director.PersistentCameraCenter, Is.EqualTo(300f));
    }

    [Test]
    public void PersistentCameraCenter_UpdatesToRightmostWave()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.PersistentCameraCenter, Is.EqualTo(300f));

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());

        Assert.That(_director.PersistentCameraCenter, Is.EqualTo(900f));
    }

    [Test]
    public void PersistentCameraCenter_ClearedOnReset()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        Assert.That(_director.PersistentCameraCenter, Is.EqualTo(300f));

        var newDirector = new TestLevelDirector(_entityManager, _level, _player);
        Assert.That(newDirector.PersistentCameraCenter, Is.Null);
    }

    [Test]
    public void CameraLeftBound_PersistsAfterWaveClear()
    {
        float? leftLock = null;

        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        var fa = _director.CurrentFightArea;
        if (fa.HasValue)
            leftLock = fa.Value.X + fa.Value.Width / 2f;
        _cameraController.LeftBound = leftLock;
        _cameraController.RightBound = fa.HasValue ? leftLock : null;

        Assert.That(_cameraController.LeftBound, Is.EqualTo(300f));
        Assert.That(_cameraController.RightBound, Is.EqualTo(300f));

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());
        fa = _director.CurrentFightArea;
        _cameraController.LeftBound = fa.HasValue ? fa.Value.X + fa.Value.Width / 2f : leftLock;
        _cameraController.RightBound = fa.HasValue ? fa.Value.X + fa.Value.Width / 2f : null;

        Assert.That(_cameraController.LeftBound, Is.EqualTo(300f));
        Assert.That(_cameraController.RightBound, Is.Null);
    }

    [Test]
    public void CameraLeftBound_PersistsAcrossMultipleWaves()
    {
        float? leftLock = null;

        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());
        var fa = _director.CurrentFightArea;
        if (fa.HasValue)
            leftLock = fa.Value.X + fa.Value.Width / 2f;

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1200, 0);
        _director.Update(new GameTime());
        fa = _director.CurrentFightArea;
        if (fa.HasValue)
            leftLock = fa.Value.X + fa.Value.Width / 2f;

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());
        fa = _director.CurrentFightArea;
        _cameraController.LeftBound = fa.HasValue ? fa.Value.X + fa.Value.Width / 2f : leftLock;
        _cameraController.RightBound = fa.HasValue ? fa.Value.X + fa.Value.Width / 2f : null;

        Assert.That(_cameraController.LeftBound, Is.EqualTo(900f));
        Assert.That(_cameraController.RightBound, Is.Null);
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

        Assert.That(_director.IsWaveCleared, Is.True);
        Assert.That(_director.ShowGoPrompt, Is.True);
    }

    [Test]
    public void ShowGoPrompt_FalseAfterPassingFightAreaRightEdge()
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

        _player.Position = new Vector2(550, 0);
        _director.Update(new GameTime());

        Assert.That(_director.ShowGoPrompt, Is.False);
    }

    [Test]
    public void ShowGoPrompt_TrueAfterBacktrackingLeftOfFightAreaRightEdge()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(600, 0);
        _director.Update(new GameTime());
        Assert.That(_director.ShowGoPrompt, Is.False);

        _player.Position = new Vector2(400, 0);
        _director.Update(new GameTime());
        Assert.That(_director.ShowGoPrompt, Is.True);
    }

    [Test]
    public void ShowGoPrompt_FalseAfterLastWaveClearedAndPlayerPassesRightEdge()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(900, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.IsWaveCleared, Is.True);
        Assert.That(_director.ShowGoPrompt, Is.True);

        _player.Position = new Vector2(1600, 0);
        _director.Update(new GameTime());

        Assert.That(_director.ShowGoPrompt, Is.False);
    }

    [Test]
    public void ShowGoPrompt_TrueAfterLastWaveCleared_BeforeEndTriggerX()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(900, 0);
        _director.Update(new GameTime());

        foreach (var entity in _director.SpawnedEnemies)
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        _player.Position = new Vector2(1400, 0);
        _director.Update(new GameTime());

        Assert.That(_director.ShowGoPrompt, Is.True);
    }

    [Test]
    public void MovementBounds_ExtendForPlayer_WhenPastFightAreaRightEdge()
    {
        _player.Position = new Vector2(800, 0);
        _director.Update(new GameTime());

        var fa = _director.CurrentFightArea;
        Assert.That(fa, Is.Not.Null);
        Assert.That(fa.Value.X, Is.EqualTo(50f));
        Assert.That(fa.Value.Width, Is.EqualTo(500f));

        float extendedRightEdge = fa.Value.X + Math.Max(fa.Value.Width, _player.Position.X - fa.Value.X);
        Assert.That(extendedRightEdge, Is.EqualTo(800f));

        float originalRightEdge = fa.Value.X + fa.Value.Width;
        Assert.That(extendedRightEdge, Is.GreaterThan(originalRightEdge));
    }

    [Test]
    public void MovementBounds_NoExtension_WhenPlayerInsideFightArea()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        var fa = _director.CurrentFightArea;
        Assert.That(fa, Is.Not.Null);
        Assert.That(fa.Value.X, Is.EqualTo(50f));
        Assert.That(fa.Value.Width, Is.EqualTo(500f));

        float extendedRightEdge = fa.Value.X + Math.Max(fa.Value.Width, _player.Position.X - fa.Value.X);
        Assert.That(extendedRightEdge, Is.EqualTo(550f));
        Assert.That(extendedRightEdge, Is.EqualTo(fa.Value.Right));
    }

    [Test]
    public void FullFlow_TriggerWave_KillEnemies_AdvanceToNext()
    {
        _player.Position = new Vector2(300, 0);
        _director.Update(new GameTime());

        Assert.That(_director.IsScrollLocked, Is.True);
        Assert.That(_director.CurrentFightArea, Is.Not.Null);

        foreach (var entity in _director.SpawnedEnemies.ToList())
        {
            var enemy = (EnemyEntity)entity;
            _director.SimulateEnemyDied(enemy);
        }
        _director.Update(new GameTime());

        Assert.That(_director.IsScrollLocked, Is.False);
        Assert.That(_director.CurrentFightArea, Is.Null);

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