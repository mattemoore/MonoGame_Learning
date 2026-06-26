using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Levels;

public class LevelDirector(EntityManager entityManager, Level level, Entity player)
{
    private readonly EntityManager _entityManager = entityManager;
    private readonly Level _level = level;
    private readonly Entity _player = player;

    private int _currentWaveIndex;
    private readonly List<EnemyEntity> _activeEnemies = [];
    private bool _isScrollLocked;
    private bool _waveCleared;
    private bool _waveTriggered;
    private float? _clearedFightAreaRightEdge;

    public event Action LevelCompleted;
    public bool IsWaveCleared => _waveCleared;
    public bool ShowGoPrompt => _waveCleared && _clearedFightAreaRightEdge.HasValue && _player.Position.X < _clearedFightAreaRightEdge.Value;
    public int CurrentWaveIndex => _currentWaveIndex;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public bool IsScrollLocked => _isScrollLocked;
    public RectangleF? CurrentFightArea { get; private set; }
    public float? PersistentCameraCenter { get; private set; }

    public void Update(GameTime gameTime)
    {
        var waves = _level.WaveDefs;

        if (_currentWaveIndex >= waves.Count)
        {
            _clearedFightAreaRightEdge = _level.EndTriggerX;
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
            _clearedFightAreaRightEdge = CurrentFightArea?.Right;
            CurrentFightArea = null;
            _currentWaveIndex++;
        }
    }

    protected virtual void SpawnWave()
    {
        _waveTriggered = true;
        _isScrollLocked = true;
        _waveCleared = false;
        var wave = _level.WaveDefs[_currentWaveIndex];
        float halfWidth = wave.FightAreaWidth / 2f;
        CurrentFightArea = new RectangleF(wave.TriggerX - halfWidth, 0, wave.FightAreaWidth, 0);
        PersistentCameraCenter = wave.TriggerX;

        foreach (var def in wave.Enemies)
        {
            EnemyEntity enemy = CreateEnemy(def);
            _activeEnemies.Add(enemy);
            _entityManager.Register(enemy);
        }
    }

    protected virtual EnemyEntity CreateEnemy(EnemySpawnDef def)
    {
        EnemyEntity enemy = def.Type switch
        {
            "Grunt" => new EnemyEntity($"enemy_{Guid.NewGuid()}", def.Position, 2.0f, EnemySprite.Create()),
            _ => throw new ArgumentOutOfRangeException(nameof(def.Type), def.Type, null)
        };
        enemy.Target = _player;
        enemy.Died += OnEnemyDied;
        return enemy;
    }

    protected virtual void OnEnemyDied(object sender, EventArgs e)
    {
        if (sender is not EnemyEntity enemy) return;
        enemy.Died -= OnEnemyDied;
        _activeEnemies.Remove(enemy);
        _entityManager.Destroy(enemy);
    }
}