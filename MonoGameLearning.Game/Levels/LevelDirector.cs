using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.GameCore;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Props;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Levels;

#pragma warning disable CS8524 // Owned enums SpawnSide/SpawnVertical — switch is exhaustive without discard

public class LevelDirector
{
    private readonly EntityManager _entityManager;
    private readonly Level _level;
    private readonly Entity _player;

    protected EnemyPool EnemyPool { get; set; }

    private readonly List<ActorSnapshot> _enemyBuf = [];
    private readonly List<ActorSnapshot> _propBuf = [];
    private WorldSnapshot _currentSnapshot;

    private int _currentWaveIndex;
    private readonly List<EnemyEntity> _activeEnemies = [];
    private bool _isScrollLocked;
    private bool _waveCleared;
    private bool _waveTriggered;

    public event Action LevelCompleted;
    public bool ShowGoPrompt => _waveCleared;
    public int CurrentWaveIndex => _currentWaveIndex;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public bool IsScrollLocked => _isScrollLocked;
    public float? WaveEndX { get; private set; }
    public float? WaveTriggerX { get; private set; }
    public IReadOnlyList<EnemyEntity> ActiveEnemies => _activeEnemies;

    public ref readonly WorldSnapshot CurrentWorld => ref _currentSnapshot;

    public LevelDirector(EntityManager entityManager, Level level, Entity player)
    {
        _entityManager = entityManager;
        _level = level;
        _player = player;

        _enemyBuf.Capacity = 16;
        _propBuf.Capacity = 16;

        InitializePool();
    }

    protected virtual void InitializePool()
    {
        EnemyPool = new EnemyPool(_entityManager, this);
        EnemyPool.Build(_level);
    }

    public void SpawnProps(List<PropSpawnDef> propDefs)
    {
        foreach (var prop in propDefs)
        {
            var drum = new OilDrumEntity(prop.Type, prop.Position, 1.0f, OilDrumSprite.Create());
            drum.Destroyed += OnPropDestroyed;
            _entityManager.Register(drum);
        }
    }

    private void OnPropDestroyed(Entity prop)
    {
        if (prop is OilDrumEntity oilDrum)
            oilDrum.Destroyed -= OnPropDestroyed;
        _entityManager.Destroy(prop);
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
        var all = _entityManager.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] is PropBase prop)
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

        WaveTriggerX = wave.TriggerX;
        WaveEndX = wave.EndX;

        float cameraLeftEdge = GameCore.Camera?.Position.X ?? 0f;
        float gameWidth = GameCore.Camera is not null
            ? GameCore.ViewportAdapter.VirtualWidth
            : 800;

        var walkableBounds = _level.MovementBounds;
        float walkableTop = walkableBounds.Y;
        float walkableBottom = walkableBounds.Bottom;

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
            // FormatterServices-created test enemies have null Sprite — guard to avoid NPE.
            if (enemy.Sprite is not null)
            {
                enemy.Direction = initialFacing;
                enemy.Sprite.Effect = initialFacing == FacingDirection.Left
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None;

                Vector2 walkDir = initialFacing == FacingDirection.Left ? new Vector2(-1, 0) : new Vector2(1, 0);
                float targetX = initialFacing == FacingDirection.Left
                    ? cameraLeftEdge + gameWidth - halfW - 50f
                    : cameraLeftEdge + halfW + 50f;
                enemy.SetSpawnWalkData(walkDir, targetX);
            }

            enemy.Died += OnEnemyDied;
            _activeEnemies.Add(enemy);
        }
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
        if (_currentWaveIndex >= waves.Count) return;

        var nextWave = waves[_currentWaveIndex];
        if (_waveTriggered) return;

        float cameraLeftEdge = GameCore.Camera?.Position.X ?? 0f;
        float gameWidth = GameCore.Camera is not null
            ? GameCore.ViewportAdapter.VirtualWidth
            : 800;

        var walkableBounds = _level.MovementBounds;
        float walkableTop = walkableBounds.Y;
        float walkableBottom = walkableBounds.Bottom;

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

    protected virtual void OnEnemyDied(object sender, EventArgs e)
    {
        if (sender is not EnemyEntity enemy) return;
        enemy.Died -= OnEnemyDied;
        _activeEnemies.Remove(enemy);
        EnemyPool.Return(enemy);
    }
}