# Level Director & Wave Spawn System

## Summary

Create a `LevelDirector` system that `GameLoop` uses alongside the current `Level`
to orchestrate level progression: background loading (existing), wave-based enemy
spawning (new), fight area scroll locking (new), wave clearance + "GO ->" prompt
(new), and a level end trigger (new). This covers the remaining Milestone 3 item
("Enemy Wave/Spawner Trigger") and all of Milestone 4
("Scroll Locking & Level Progression"). Milestone 5 (HUD) is **out of scope**.

---

## Design Decisions (confirmed via interview)

| Decision | Choice |
| --- | --- |
| Wave trigger mechanism | Simple player X threshold (player.Position.X >= TriggerX). No physics TriggerEntity. Guarded by `_waveTriggered` to fire once. |
| WaveDef location | Abstract members on `Level` base class |
| FightAreaWidth | **Required** field on `WaveDef` (not optional), default 800f (one screen width) |
| Scroll lock implementation | Split: `CameraController` clamps camera X to `RightBound`. `GameLoop` overrides player `MovementBounds` width when scroll is locked. |
| Player bounds during scroll lock | `LevelDirector` exposes `float? CurrentFightAreaRight`. `GameLoop` sets `movable.MovementBounds.Width = CurrentFightAreaRight - bounds.Left` when non-null. |
| GO prompt rendering | Separate `SpriteBatch.Begin()` without transform matrix in screen space (after entity draw, before `GumService.Draw()`). Keeps door open for more screen-space sprites / HUD conversion from Gum. |
| LevelDirector reset | **Recreated** in `GameLoop.ResetGame()` via `new LevelDirector(...)`. No `Reset()` method. |
| LevelCompleted signaling | `event Action LevelCompleted` on `LevelDirector`. `GameLoop` subscribes to fire `GameTrigger.CompleteLevel`. Keeps `LevelDirector` agnostic of state machine. |
| Enemy type identifier | `string` key (e.g. `"Grunt"`), not enum. |
| Enemy factory | Inline `switch` expression in `LevelDirector.SpawnWave()`. No separate factory class. |
| Enemy sprite loading | Already eager at startup via `GameLoop.LoadContent()`. Accept for now — optimize if levels grow and startup load becomes a problem. |
| Wave progression | Advance wave index **immediately** on clear (no "clear distance" magic number). Next wave triggers when player reaches its `TriggerX`. |
| CameraController.RightBound | Simple `float? RightBound { get; set; }` property. Checked as additional clamp on `maxX` in `Update()`. |
| LevelDirector constructor | Takes `EntityManager`, `CameraController`, `Level`, `PlayerEntity`. Stores `Level` reference for future extensibility (time-of-day, difficulty, etc.). |
| EndTriggerX guard | Only fires `LevelCompleted` when `_currentWaveIndex >= _waves.Count` (all waves cleared). Prevents skipping fights. |
| LevelDirector Update flow | 1) All waves done → check end trigger. 2) No active wave + not triggered → check player X. 3) Wave active + enemies alive → idle. 4) `_activeEnemies.Count == 0 && _isScrollLocked` → clear, unlock, advance. |
| IsWaveCleared for GO prompt | Expose `bool IsWaveCleared { get; }` on `LevelDirector`. Polled by `GameLoop.Draw()` each frame. |
| Progression pacing | Not part of this plan. Future concern: pause between waves, introduction animations, etc. |
| Props (oil drums) | Keep hardcoded in `GameLoop` for now. Future concern: define and position props in level files. |

---

## New Files

### `MonoGameLearning.Game/Levels/WaveDef.cs`

```csharp
namespace MonoGameLearning.Game.Levels;

public record EnemySpawnDef(string Type, Vector2 Position);

public record WaveDef(float TriggerX, float FightAreaWidth, List<EnemySpawnDef> Enemies);
```

`FightAreaWidth` is **required**. The scroll-locked fight zone starts at `TriggerX`
and extends right by `FightAreaWidth`. Right bound = `TriggerX + FightAreaWidth`.

### `MonoGameLearning.Game/Levels/LevelDirector.cs`

A new class that owns the wave progression lifecycle. It is **not** an Entity — it is a controller used by `GameLoop`.

**Constructor:**

```csharp
public LevelDirector(EntityManager entityManager, CameraController cameraController,
    Level level, PlayerEntity player)
```

**State:**

- `int _currentWaveIndex`
- `List<EnemyEntity> _activeEnemies`
- `bool _isScrollLocked`
- `bool _waveCleared` (set true when active enemies count hits zero while locked)
- `bool _waveTriggered` (per-wave guard against re-triggering; could be a `List<bool>` or
  a simple bool since waves are sequential)

**Public API:**

- `void Update(GameTime gameTime)` — state machine driver
- `event Action LevelCompleted` — fired when end trigger X reached and all waves done
- `bool IsWaveCleared { get; }` — polled by `GameLoop.Draw()` for GO prompt
- `float? CurrentFightAreaRight { get; }` — non-null when scroll is locked, used by `GameLoop` to clamp player bounds

**Update flow:**

```text
Update(gameTime):
  1. If _currentWaveIndex >= _waves.Count (all waves done)
       if player.X >= level.EndTriggerX → fire LevelCompleted
       return
  2. If no wave active and !_waveTriggered
       if player.X >= waves[_currentWaveIndex].TriggerX → SpawnWave()
       return
  3. If wave active (_isScrollLocked && _activeEnemies.Count > 0)
       return (player fighting)
  4. If _activeEnemies.Count == 0 && _isScrollLocked
       → mark wave cleared, unlock scroll, advance _currentWaveIndex
```

**SpawnWave():**

```csharp
private void SpawnWave()
{
    _waveTriggered = true;
    _isScrollLocked = true;
    _waveCleared = false;
    var wave = _level.WaveDefs[_currentWaveIndex];
    CurrentFightAreaRight = wave.TriggerX + wave.FightAreaWidth;
    _cameraController.RightBound = CurrentFightAreaRight;

    foreach (var def in wave.Enemies)
    {
        EnemyEntity enemy = def.Type switch
        {
            "Grunt" => new EnemyEntity($"enemy_{Guid.NewGuid()}", def.Position, 2.0f, EnemySprite.Create()),
            _ => throw new ArgumentOutOfRangeException(nameof(def.Type), def.Type, null)
        };
        enemy.Target = _player;
        enemy.Died += OnEnemyDied;
        _activeEnemies.Add(enemy);
        _entityManager.Register(enemy);
    }
}
```

**OnEnemyDied:**

```csharp
private void OnEnemyDied(object sender, EventArgs e)
{
    if (sender is not EnemyEntity enemy) return;
    enemy.Died -= OnEnemyDied;
    _activeEnemies.Remove(enemy);
    _entityManager.Destroy(enemy);
}
```

### No separate `LevelEndTrigger` file — logic is inline in `LevelDirector`

---

## Modified Files

### `MonoGameLearning.Core/GameLoop/CameraController.cs`

- Add `public float? RightBound { get; set; }` property, default `null`.
- In `Update()`, add it as an additional upper clamp on `maxX`:

  ```csharp
  float maxX = _totalLevelWidth - (_gameWidth / 2f);
  if (RightBound.HasValue)
      maxX = Math.Min(maxX, RightBound.Value);
  float clampedX = Math.Clamp(_player.Position.X, minX, maxX);
  ```

### `MonoGameLearning.Game/Levels/Level.cs`

- `Backgrounds`: Keep constructor-injected via parameter (static factory in subclass) since ContentManager and game dimensions must be available before base constructor runs. This achieves the same lazy/deferred goal — the subclass does the heavy creation in a static method, not in the constructor body.
- Add abstract member: `public abstract List<WaveDef> WaveDefs { get; }`
- Add abstract member: `public abstract float EndTriggerX { get; }`

### `MonoGameLearning.Game/Levels/Level1.cs`

- Override `WaveDefs` — lazy-init backing field:

  ```csharp
  private List<WaveDef>? _waveDefs;
  public override List<WaveDef> WaveDefs => _waveDefs ??= CreateWaveDefs();

  private List<WaveDef> CreateWaveDefs()
  {
      // Example: 2 waves
      return
      [
          new WaveDef(TriggerX: 600f, FightAreaWidth: 800f, Enemies:
          [
              new EnemySpawnDef("Grunt", new Vector2(650, 550)),
              new EnemySpawnDef("Grunt", new Vector2(700, 550))
          ]),
          new WaveDef(TriggerX: 1300f, FightAreaWidth: 800f, Enemies:
          [
              new EnemySpawnDef("Grunt", new Vector2(1350, 550)),
              new EnemySpawnDef("Grunt", new Vector2(1400, 550))
          ])
      ];
  }
  ```

- Override `EndTriggerX` — set to e.g. `GAME_WIDTH * 2 - 100` (near end of the 1600px level).

### `MonoGameLearning.Game/GameLoop/GameLoop.cs`

**In `Initialize()` / `OnTransitioned`:**

- Already calls `ResetGame()` when transitioning to Playing from non-Paused. No change needed.

**In `LoadContent()`:**

- After creating `_cameraController`, create `LevelDirector`:

  ```csharp
  _levelDirector = new LevelDirector(_entityManager, _cameraController, _currentLevel, _player);
  _levelDirector.LevelCompleted += () => _gameState.Fire(GameTrigger.CompleteLevel);
  ```

- Remove hardcoded `RegisterEnemy(...)` calls.
- Keep oil drum/prop registration.

**In `Update()` (Playing block):**

- After `_input.Update(gameTime)` and before entity updates:

  ```csharp
  if (_gameState.State == GameState.Playing)
      _levelDirector.Update(gameTime);
  ```

- In the movement-bounds loop, add scroll-lock override:

  ```csharp
  foreach (var movable in _entityManager.Movables)
  {
      var bounds = _currentLevel.MovementBounds;
      if (_levelDirector.CurrentFightAreaRight.HasValue)
          bounds.Width = _levelDirector.CurrentFightAreaRight.Value - bounds.Left;
      movable.MovementBounds = bounds;
  }
  ```

**In `Draw()`:**

- After entity draw `SpriteBatch.End()` and before `GumService.Draw()`:

  ```csharp
  if (_levelDirector.IsWaveCleared)
  {
      SpriteBatch.Begin();
      var goText = "GO ->";
      var textSize = _debugFont.MeasureString(goText);
      SpriteBatch.DrawString(_debugFont, goText,
          new Vector2(GAME_WIDTH / 2f - textSize.X, GAME_HEIGHT / 2f - 50),
          Color.LimeGreen);
      SpriteBatch.End();
  }
  ```

**In `ResetGame()`:**

- Remove hardcoded `RegisterEnemy(...)` calls.
- After creating `_currentLevel`, `_cameraController`, and re-registering props:

  ```csharp
  _levelDirector = new LevelDirector(_entityManager, _cameraController, _currentLevel, _player);
  _levelDirector.LevelCompleted += () => _gameState.Fire(GameTrigger.CompleteLevel);
  ```

**Remove:**

- `RegisterEnemy()` method
- `OnEnemyDied()` method (enemy death handled by `LevelDirector`)

**Debug-mode drawing (`if (IsDebug)` block, after entity debug draws, inside the camera-transformed `SpriteBatch.Begin()` pass):**

```csharp
if (IsDebug)
{
    // Wave trigger X positions
    foreach (var wave in _currentLevel.WaveDefs)
    {
        var triggerLine = new Vector2(wave.TriggerX - Camera.Position.X + GAME_WIDTH / 2f, 0);
        SpriteBatch.DrawString(_debugFont, $"Wave X={wave.TriggerX}", new Vector2(triggerLine.X, 10), Color.Cyan);
    }

    // End trigger X
    var endLine = new Vector2(_currentLevel.EndTriggerX - Camera.Position.X + GAME_WIDTH / 2f, 0);
    SpriteBatch.DrawString(_debugFont, $"End X={_currentLevel.EndTriggerX}", new Vector2(endLine.X, 25), Color.Orange);

    // Active fight area (when scroll locked)
    if (_levelDirector.CurrentFightAreaRight.HasValue)
    {
        var fightLeft = _currentLevel.MovementBounds.Left;
        var fightRight = _levelDirector.CurrentFightAreaRight.Value;
        var screenLeft = fightLeft - Camera.Position.X + GAME_WIDTH / 2f;
        var screenRight = fightRight - Camera.Position.X + GAME_WIDTH / 2f;
        var rect = new RectangleF(screenLeft, 0, screenRight - screenLeft, GAME_HEIGHT);
        SpriteBatch.DrawRectangle(rect, Color.Yellow * 0.3f, 2f);
    }
}

// In the debug text window (_debugWindow1), append wave info:
var waveStatus = _levelDirector.CurrentWaveIndex < _currentLevel.WaveDefs.Count
    ? $"Wave: {_levelDirector.CurrentWaveIndex + 1}/{_currentLevel.WaveDefs.Count}"
    : "All waves done";
var waveInfo = $"{waveStatus} | Active: {_levelDirector.ActiveEnemyCount} | Locked: {_levelDirector.IsScrollLocked}";
```

**Add to `LevelDirector` public API for debug:**

- `int CurrentWaveIndex { get; }`
- `int ActiveEnemyCount { get; }` (returns `_activeEnemies.Count`)
- `bool IsScrollLocked { get; }`

---

## Data Flow

```text
GameLoop.Update()
  └─ _levelDirector.Update(gameTime)
       ├─ Check trigger X vs player X ─► SpawnWave() ─► entityManager.Register(enemies)
       │                                        └► camera.RightBound = fightAreaRight
       │                                        └► CurrentFightAreaRight = fightAreaRight
       ├─ Check _activeEnemies.Count == 0 ─► mark cleared, camera.RightBound = null,
       │                                     CurrentFightAreaRight = null, advance index
       └─ Check all waves done + endTriggerX ─► fire LevelCompleted event
GameLoop.Draw()
  └─ _levelDirector.IsWaveCleared ? draw "GO ->" in screen space
```

---

## Risks & Edge Cases

1. **Player backtracks past trigger X**: Guarded by `_waveTriggered` — fires once per wave.
2. **Enemies die during scroll lock, player hasn't reached clear zone**: Wave clears immediately, scroll unlocks. Expected beat 'em up behavior.
3. **Player is already past trigger X on ResetGame**: Player starts at X=100, before first trigger. Fine.
4. **Multiple waves simultaneously**: Not supported in Phase 1. Sequential only.
5. **Player overshoots trigger X in one frame**: Check is `>=`. Fight area right bound includes trigger position, so player won't be locked behind a scroll they already passed.
6. **Stale subscriptions during ResetGame**: Old `LevelDirector` is discarded and garbage collected. New one is constructed fresh. Single-frame boundary at state transition — acceptable risk.
7. **Abstract method call from Level constructor (Backgrounds)**: Not an issue — backgrounds use constructor parameter injection with static factory, not abstract method. `WaveDefs` uses lazy abstract property but is not called from constructor.

---

## Validation

1. `dotnet build` compiles cleanly
2. `dotnet test` passes all existing tests
3. Manual play-test: game starts, player scrolls right, at trigger X enemies spawn, scroll locks, player cannot advance past fight area, killing all enemies unlocks scroll and shows "GO ->", player reaches end of level → LevelComplete screen appears
4. No regression on existing combat, player movement, or state transitions

---

## Implementation Order

1. `WaveDef.cs` — data records
2. `CameraController.cs` — add `RightBound` property
3. `Level.cs` — add abstract `WaveDefs` and `EndTriggerX`
4. `Level1.cs` — implement wave defs
5. `LevelDirector.cs` — full class
6. `GameLoop.cs` — integrate LevelDirector, remove hardcoded enemy registration, add GO prompt draw, add scroll-lock player bounds override
7. Build & test
