using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Levels;

#pragma warning disable CS8524 // Owned enums SpawnSide/SpawnVertical — switch is exhaustive without discard

public class LevelDirector
{
    private readonly EntityService _entityManager;
    private readonly Level _level;
    private readonly Entity _player;
    private readonly AudioService _audio;
    private readonly Func<PropSpawnDef, PropBase> _createProp;
    private readonly Func<PickupSpawnDef, Entity> _createPickup;
    private readonly Func<string, MeleeWeaponDef> _getWeapon;
    private readonly Func<string, int, Func<WorldSnapshot>, EnemyEntity> _createEnemy;
    private readonly Func<RectangleF> _getCameraView;

    protected EnemyPool EnemyPool { get; set; }

    private readonly List<ActorSnapshot> _enemyBuf = [];
    private readonly List<ActorSnapshot> _propBuf = [];
    private WorldSnapshot _currentSnapshot;

    private int _currentWaveIndex;
    private readonly List<EnemyEntity> _activeEnemies = [];
    private bool _isScrollLocked;
    private bool _waveCleared;
    private bool _waveTriggered;
    private bool _goBellPlayed;

    public event Action LevelCompleted;
    public bool ShowGoPrompt => _waveCleared;
    public int CurrentWaveIndex => _currentWaveIndex;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public bool IsScrollLocked => _isScrollLocked;
    public float? WaveEndX { get; private set; }
    public float? WaveTriggerX { get; private set; }
    public IReadOnlyList<EnemyEntity> ActiveEnemies => _activeEnemies;

    public ref readonly WorldSnapshot CurrentWorld => ref _currentSnapshot;

    public LevelDirector(
        EntityService entityManager,
        Level level,
        Entity player,
        AudioService audio,
        Func<PropSpawnDef, PropBase> createProp,
        Func<PickupSpawnDef, Entity> createPickup,
        Func<string, MeleeWeaponDef> getWeapon,
        Func<string, int, Func<WorldSnapshot>, EnemyEntity> createEnemy,
        Func<RectangleF> getCameraView)
    {
        _entityManager = entityManager;
        _level = level;
        _player = player;
        _audio = audio;
        _createProp = createProp;
        _createPickup = createPickup;
        _getWeapon = getWeapon;
        _createEnemy = createEnemy;
        _getCameraView = getCameraView;

        _enemyBuf.Capacity = 16;
        _propBuf.Capacity = 16;

        InitializePool();
    }

    protected virtual void InitializePool()
    {
        EnemyPool = new EnemyPool(_entityManager, () => CurrentWorld, _createEnemy);
        EnemyPool.Build(_level);
    }

    public void SpawnProps(List<PropSpawnDef> propDefs)
    {
        foreach (var prop in propDefs)
        {
            var entity = _createProp(prop);
            entity.Destroyed += OnPropDestroyed;
            _entityManager.Register(entity);
        }
    }

    protected void OnPropDestroyed(PropBase prop)
    {
        prop.Destroyed -= OnPropDestroyed;
        SpawnDrops(prop);
        _entityManager.Destroy(prop);
    }

    public void SpawnPickups(IReadOnlyList<PickupSpawnDef> pickupDefs)
    {
        foreach (var def in pickupDefs)
            _entityManager.Register(_createPickup(def));
    }

    private void SpawnDrops<T>(T source) where T : Entity, IPickupDropper
    {
        foreach (var def in source.CreateDrops())
        {
            var pickup = _createPickup(def);
            pickup.Position = new Vector2(source.Frame.Center.X, source.Frame.Bottom - pickup.Height / 2f);
            _entityManager.Register(pickup);
        }
    }

    public void PopulateSnapshots(RectangleF walkableBounds)
    {
        _enemyBuf.Clear();
        for (int i = 0; i < _activeEnemies.Count; i++)
        {
            var enemy = _activeEnemies[i];
            _enemyBuf.Add(new ActorSnapshot(enemy.Position, enemy.Width * 0.5f, enemy.Height * 0.5f));
        }

        _propBuf.Clear();
        var props = _entityManager.Props;
        for (int i = 0; i < props.Count; i++)
        {
            var prop = props[i];
            _propBuf.Add(new ActorSnapshot(prop.Position, prop.Width * 0.5f, prop.Height * 0.5f));
        }

        _currentSnapshot = new WorldSnapshot(
            _player.Position,
            walkableBounds,
            _enemyBuf,
            _propBuf);
    }

    public void Update(GameTime gameTime)
    {
        var waves = _level.WaveDefs;

        if (_currentWaveIndex >= waves.Count)
        {
            if (_player.Position.X >= _level.EndTriggerX)
                LevelCompleted?.Invoke();
            return;
        }

        if (!_isScrollLocked && !_waveTriggered)
        {
            if (_player.Position.X >= waves[_currentWaveIndex].TriggerX)
                SpawnWave();
            return;
        }

        if (_activeEnemies.Count == 0 && _isScrollLocked)
        {
            _waveCleared = true;
            if (!_goBellPlayed && _audio is not null)
            {
                _audio.PlaySfx(SfxId.GoPromptBell);
                _goBellPlayed = true;
            }
            _isScrollLocked = false;
            _waveTriggered = false;
            WaveEndX = null;
            WaveTriggerX = null;
            _currentWaveIndex++;
        }

        Debug.Assert(!(_isScrollLocked && (WaveEndX is null || WaveTriggerX is null)),
            "Scroll locked but WaveEndX or WaveTriggerX is null — state inconsistency.");
    }

    protected virtual void SpawnWave()
    {
        var wave = _level.WaveDefs[_currentWaveIndex];
        Debug.Assert(wave.TriggerX > 0, $"Wave TriggerX must be at a screen boundary; got {wave.TriggerX}.");
        Debug.Assert(wave.EndX > wave.TriggerX, $"Wave EndX ({wave.EndX}) must be > TriggerX ({wave.TriggerX}).");

        _waveTriggered = true;
        _isScrollLocked = true;
        _waveCleared = false;
        _goBellPlayed = false;

        WaveTriggerX = wave.TriggerX;
        WaveEndX = wave.EndX;

        var (cameraLeftEdge, gameWidth, _, walkableTop, walkableBottom) = GetSpawnContext();

        foreach (var def in wave.Enemies)
        {
            float halfW = 24f;
            float halfH = 30f;
            Vector2 pos = ComputeSpawnPosition(def.Side, def.Vertical, cameraLeftEdge, gameWidth, walkableTop, walkableBottom, halfW, halfH);

            FacingDirection initialFacing = def.Side switch
            {
                SpawnSide.Left => FacingDirection.Right,
                SpawnSide.Right => FacingDirection.Left,
            };

            var enemy = EnemyPool.Rent(def.Type, pos, _player);
            enemy.Drops = def.Drops;   // after Rent — Rent→Reset clears Drops
            if (def.Weapon is not null)
                enemy.EquipWeapon(_getWeapon(def.Weapon));
            // SpriteRenderer without an attached sprite (test enemies) → skip visual setup.
            if (enemy.SpriteRenderer.Sprite is not null)
            {
                enemy.Direction = initialFacing;
                enemy.SpriteRenderer.SetEffect(initialFacing == FacingDirection.Left
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None);

                Vector2 walkDir = initialFacing == FacingDirection.Left ? new Vector2(-1, 0) : new Vector2(1, 0);
                float targetX = initialFacing == FacingDirection.Left
                    ? cameraLeftEdge + gameWidth - halfW - 50f
                    : cameraLeftEdge + halfW + 50f;
                enemy.SetSpawnWalkData(walkDir, targetX);
            }

            enemy.Died += OnDiedHandler;
            _activeEnemies.Add(enemy);
        }
    }

    private (float CameraLeftEdge, float GameWidth, float ViewportHeight, float WalkableTop, float WalkableBottom) GetSpawnContext()
    {
        var view = _getCameraView();
        var walkableBounds = _level.MovementBounds;
        return (view.X, view.Width, view.Height, walkableBounds.Y, walkableBounds.Bottom);
    }

    private static Vector2 ComputeSpawnPosition(
        SpawnSide side,
        SpawnVertical vertical,
        float cameraLeftEdge,
        float gameWidth,
        float walkableTopY,
        float walkableBottomY,
        float entityHalfWidth,
        float entityHalfHeight)
    {
        float x = side switch
        {
            SpawnSide.Left => cameraLeftEdge - entityHalfWidth - 100f,
            SpawnSide.Right => cameraLeftEdge + gameWidth + entityHalfWidth + 100f,
        };

        float y = vertical switch
        {
            SpawnVertical.Top => walkableTopY + entityHalfHeight + 10f,
            SpawnVertical.Middle => (walkableTopY + walkableBottomY) * 0.5f,
            SpawnVertical.Bottom => walkableBottomY - entityHalfHeight - 10f,
        };

        y = MathHelper.Clamp(y, walkableTopY, walkableBottomY);

        return new Vector2(x, y);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        var waves = _level.WaveDefs;
        var (cameraLeftEdge, gameWidth, viewportHeight, walkableTop, walkableBottom) = GetSpawnContext();

        foreach (var wave in waves)
        {
            context.SpriteBatch.DrawLine(wave.TriggerX, 0, wave.TriggerX, viewportHeight, Color.Cyan * 0.4f, 2f);
            context.SpriteBatch.DrawLine(wave.EndX, 0, wave.EndX, viewportHeight, Color.Yellow * 0.4f, 2f);
        }

        context.SpriteBatch.DrawLine(_level.EndTriggerX, 0, _level.EndTriggerX, viewportHeight, Color.Orange * 0.4f, 2f);

        float levelRight = _level.MovementBounds.Right;
        context.SpriteBatch.DrawLine(0, _level.WalkableTopY, levelRight, _level.WalkableTopY, Color.Lime * 0.5f, 2f);

        if (_isScrollLocked && WaveTriggerX.HasValue && WaveEndX.HasValue)
        {
            context.SpriteBatch.DrawLine(WaveTriggerX.Value, 0, WaveTriggerX.Value, viewportHeight, Color.Cyan * 0.7f, 2f);
            context.SpriteBatch.DrawLine(WaveEndX.Value, 0, WaveEndX.Value, viewportHeight, Color.Yellow * 0.7f, 2f);
        }

        if (_currentWaveIndex >= waves.Count) return;
        var nextWave = waves[_currentWaveIndex];
        if (_waveTriggered) return;

        foreach (var def in nextWave.Enemies)
        {
            float halfW = 24f;
            float halfH = 30f;
            Vector2 pos = ComputeSpawnPosition(def.Side, def.Vertical, cameraLeftEdge, gameWidth, walkableTop, walkableBottom, halfW, halfH);

            Color color = def.Side == SpawnSide.Left ? Color.Cyan : Color.Magenta;
            context.SpriteBatch.DrawCircle(pos, 8f, 12, color, 2f);
            context.SpriteBatch.DrawString(context.Font, def.Type, new Vector2(pos.X + 12f, pos.Y - 8f), color);
        }
    }

    private void OnDiedHandler(object sender, EventArgs e)
    {
        if (sender is EnemyEntity enemy)
            OnEnemyDied(enemy);
    }

    protected virtual void OnEnemyDied(EnemyEntity enemy)
    {
        enemy.Died -= OnDiedHandler;
        _activeEnemies.Remove(enemy);
        SpawnDrops(enemy);   // before Return — position is still real
        EnemyPool.Return(enemy);
    }
}