using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Levels;

public class LevelDirector(EntityManager entityManager, Level level, Entity player, int gameWidth, int gameHeight)
{
    private readonly EntityManager _entityManager = entityManager;
    private readonly Level _level = level;
    private readonly Entity _player = player;
    private readonly int _gameWidth = gameWidth;
    private readonly int _gameHeight = gameHeight;

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

        foreach (var def in wave.Enemies)
        {
            var enemy = CreateEnemy(def);
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