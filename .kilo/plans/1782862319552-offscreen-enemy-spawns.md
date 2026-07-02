# Plan: Offscreen Enemy Spawns with Side/Vertical Control

## Goal

Enemies always spawn offscreen (outside the camera viewport) and approach the player. Wave definitions specify which screen side each enemy enters from and at what vertical position (top, middle, bottom of the walkable area). Spawn positions are computed dynamically at wave-trigger time using the current camera viewport.

## Design Decisions (Confirmed)

| Decision | Choice |
|---|---|
| `EnemySpawnDef.Position` | Replaced entirely with side + vertical |
| Offscreen horizontal margin | Fixed 100px past the visible edge |
| Enemy facing on spawn | Auto-face based on spawn side (right-side spawns face left, left-side spawns face right) |
| Who picks sides | Each `EnemySpawnDef` declares its own `SpawnSide` |
| Vertical position | Enum: `Top`, `Middle`, `Bottom` |
| Offscreen margin constant | 100px (enemy walks ~1 second before appearing) |

## Changes

### 1. Add `SpawnSide` and `SpawnVertical` enums

New file: `MonoGameLearning.Game/Levels/SpawnSide.cs`

```csharp
namespace MonoGameLearning.Game.Levels;

public enum SpawnSide { Left, Right }
```

New file: `MonoGameLearning.Game/Levels/SpawnVertical.cs`

```csharp
namespace MonoGameLearning.Game.Levels;

public enum SpawnVertical { Top, Middle, Bottom }
```

### 2. Replace `EnemySpawnDef.Position`

File: `MonoGameLearning.Game/Levels/WaveDef.cs`

Change from:
```csharp
public record EnemySpawnDef(string Type, Vector2 Position);
```
to:
```csharp
public record EnemySpawnDef(string Type, SpawnSide Side, SpawnVertical Vertical);
```

Delete the `using Microsoft.Xna.Framework;` import since `Vector2` is no longer used.

### 3. Add spawn-position resolver to `LevelDirector`

Add a method to `LevelDirector` (not `SpawnWave` directly — keep it testable):

```csharp
private static Vector2 ComputeSpawnPosition(
    SpawnSide side,
    SpawnVertical vertical,
    float cameraLeftEdge,
    int gameWidth,
    float walkableTopY,
    float walkableBottomY,
    float entityHalfWidth,
    float entityHalfHeight)
```

Logic:
- **Horizontal**: 
  - `Left` → `cameraLeftEdge - entityHalfWidth - 100`
  - `Right` → `cameraLeftEdge + gameWidth + entityHalfWidth + 100`
- **Vertical**:
  - `Top` → `walkableTopY + entityHalfHeight + 10`
  - `Middle` → `(walkableTopY + walkableBottomY) / 2`
  - `Bottom` → `walkableBottomY - entityHalfHeight - 10`
- Clamp vertical result to [walkableTopY, walkableBottomY] as safety net

Pass `gameWidth` via the `LevelDirector` constructor (store from `Level.MovementBounds.Width`? No — use `GameCore`'s constants or pass a `gameWidth` parameter).

Alternative: store `gameWidth` in `LevelDirector` at construction. The `GameCore.GAME_WIDTH` is on the subclass, but we can pass it down.

**Important**: `ComputeSpawnPosition` is `static` — takes all inputs explicitly. No dependency on `GameCore.Camera` static. The caller in `SpawnWave` provides the camera's current `X` from `GameCore.Camera.Position.X`.

### 4. Adjust `SpawnWave` in `LevelDirector`

Current flow:
```csharp
foreach (var def in wave.Enemies)
{
    var enemy = EnemyPool.Rent(def.Type, def.Position, _player);
    enemy.Died += OnEnemyDied;
    _activeEnemies.Add(enemy);
}
```

New flow:
```csharp
float cameraLeftEdge = GameCore.Camera?.Position.X ?? 0f;
float gameWidth = GameCore.Camera is not null 
    ? GameCore.ViewportAdapter.VirtualWidth 
    : 800;  // fallback for tests

var walkableBounds = _level.MovementBounds;
float walkableTop = walkableBounds.Y;
float walkableBottom = walkableBounds.Bottom;

foreach (var def in wave.Enemies)
{
    float halfW = 24f;  // default enemy half-width; could vary by type
    float halfH = 30f;  // default enemy half-height; could vary by type
    Vector2 pos = ComputeSpawnPosition(def.Side, def.Vertical, cameraLeftEdge, gameWidth, walkableTop, walkableBottom, halfW, halfH);
    
    // Set initial facing toward the player based on spawn side
    FacingDirection initialFacing = def.Side switch
    {
        SpawnSide.Left => FacingDirection.Right,
        SpawnSide.Right => FacingDirection.Left,
    };
    
    var enemy = EnemyPool.Rent(def.Type, pos, _player);
    enemy.Direction = initialFacing;
    enemy.Sprite.Effect = initialFacing == FacingDirection.Left
        ? SpriteEffects.FlipHorizontally
        : SpriteEffects.None;
    
    enemy.Died += OnEnemyDied;
    _activeEnemies.Add(enemy);
}
```

Alternative: Pass initial facing into `EnemyPool.Rent` or `OnRentEnemy` to avoid a two-step set. This would require signature changes in `EnemyPool`. Simpler path: just set `Direction` and `Sprite.Effect` after `Rent` in `SpawnWave`.

### 5. Spawn-entry walk before AI takes over

When an enemy spawns offscreen and walks on-screen, it should walk in a straight line at its spawn-defined position (e.g., bottom-left entry walks right along the bottom) for a short duration before AI steering engages. This gives a natural "enemies approaching" cinematic feel rather than enemies immediately swerving based on the player's current position.

**Approach**: Add an `EnemyState.Entering` state to the enemy state machine.

Changes needed:

#### 5a. Add `Entering` to `EnemyState` enum

```csharp
public enum EnemyState
{
    Dummy,
    Entering,  // NEW
    Idle,
    ...
}
```

#### 5b. Add `SpawnWalkCompleted` to `EnemyTrigger` enum

```csharp
public enum EnemyTrigger
{
    Activate,
    Reset,
    SpawnWalkCompleted,  // NEW
    StartChase,
    ...
}
```

#### 5c. Add transitions in `EnemyStateController`

```csharp
[(EnemyState.Entering, EnemyTrigger.SpawnWalkCompleted)] = EnemyState.Idle,
```

Also add to `IgnoredTriggers` for `Entering` state: ignore everything except `SpawnWalkCompleted` and `Die`.

#### 5d. Add entry/exit callbacks in `EnemyEntity.CreateStateController()`

- `OnEnteringEntry`: set movement direction toward visible area (right for left-side spawns, left for right-side spawns), play `Run` animation
- `OnEnteringExit`: stop fixed movement, clear direction

The direction should be computed from the spawn side and stored on the entity. Add a `Vector2 _spawnWalkDirection` field to `EnemyEntity`, set during `SpawnWave` alongside the initial facing.

#### 5e. Handle movement in `EnemyEntity.Update()`

During the `Entering` state, the enemy moves in `_spawnWalkDirection * Speed * deltaSeconds` without AI steering. `TryHandleIncapacitatedUpdate` currently covers `Dying`, `Hurt`, `KnockedDown`, `Dead` — `Entering` is NOT incapacitated, so it falls through to the AI block. The AI block needs a guard:

```csharp
if (_stateController.State == EnemyState.Entering)
{
    Position += _spawnWalkDirection * deltaSeconds * Speed;
    AdvanceFrameAndRegisterHitboxes(gameTime);
    return;
}
```

This runs before the AI code, so the enemy walks straight until the timer expires.

#### 5f. Timer mechanism

The `Entering` state is exited after a fixed duration (e.g. 1.5 seconds). Add a `float _spawnWalkTimer` to `EnemyEntity`, set in `OnEnteringEntry`. Decrement in the `Entering` update block. When it reaches 0, fire `SpawnWalkCompleted`.

```csharp
if (_stateController.State == EnemyState.Entering)
{
    _spawnWalkTimer -= deltaSeconds;
    Position += _spawnWalkDirection * deltaSeconds * Speed;
    if (_spawnWalkTimer <= 0f)
        _stateController.Fire(EnemyTrigger.SpawnWalkCompleted);
    AdvanceFrameAndRegisterHitboxes(gameTime);
    return;
}
```

#### 5g. Pass spawn side from SpawnWave into EnemyEntity

In `SpawnWave`, after renting the enemy, store the spawn walk direction:

```csharp
var enemy = EnemyPool.Rent(def.Type, pos, _player);
enemy.SetSpawnWalkDirection(def.Side switch
{
    SpawnSide.Left => new Vector2(1, 0),   // walk right
    SpawnSide.Right => new Vector2(-1, 0),  // walk left
});
```

Add `SetSpawnWalkDirection(Vector2 direction)` method to `EnemyEntity` that stores the direction and fires the `Entering` trigger (or sets a flag checked in Update).

#### 5h. Handle `Die` during `Entering`

The `Entering` state should permit the `Die` trigger so enemies can be killed during their entry walk. Add transition:
```csharp
[(EnemyState.Entering, EnemyTrigger.Die)] = EnemyState.Dying,
```

#### 5i. Reset on pool return

In `EnemyEntity.Reset()`, clear `_spawnWalkTimer` and `_spawnWalkDirection` so pooled enemies don't retain entry-walk state.

### 6. Update `Level1.CreateWaveDefs()`

Before:
```csharp
private static List<WaveDef> CreateWaveDefs() =>
[
    new WaveDef(TriggerX: 800f, EndX: 1200f, Enemies:
    [
        new EnemySpawnDef("Grunt", new Vector2(850, 480)),
        new EnemySpawnDef("Grunt", new Vector2(900, 480))
    ]),
    new WaveDef(TriggerX: 1600f, EndX: 2000f, Enemies:
    [
        new EnemySpawnDef("Grunt", new Vector2(1650, 200)),
        new EnemySpawnDef("Grunt", new Vector2(1700, 200))
    ])
];
```

After:
```csharp
private static List<WaveDef> CreateWaveDefs() =>
[
    new WaveDef(TriggerX: 800f, EndX: 1200f, Enemies:
    [
        new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Bottom),
        new EnemySpawnDef("Grunt", SpawnSide.Right, SpawnVertical.Bottom),
    ]),
    new WaveDef(TriggerX: 1600f, EndX: 2000f, Enemies:
    [
        new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Top),
        new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Bottom),
    ])
];
```

This demonstrates both sides and multiple vertical positions in the same wave.

### 7. Update tests

Every `new EnemySpawnDef("Grunt", new Vector2(...))` becomes `new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle)` (or appropriate side/vertical for the test scenario).

Files to update:
- `LevelDirectorTests.cs` — `TestLevel` creates `WaveDef` instances with the old `EnemySpawnDef` format
- `EnemyPoolTests.cs` — same pattern in `Setup()` and individual tests

The position assertions in tests will need adjustment since positions are now computed rather than hardcoded. Tests that check `SpawnedEnemies` positions should instead check the side/vertical fields of the `EnemySpawnDef` that was used.

### 8. Add debug drawing for spawn points

In `LevelDirector.DrawDebug` (or a new override), before a wave is triggered, draw markers at the computed spawn positions:

```
If next wave exists and not triggered:
   For each EnemySpawnDef in next wave:
      Compute spawn position (using current camera position)
      Draw a circle + triangle at that position, colored by side (cyan for left, magenta for right)
      Draw a text label with the enemy type
```

This requires `LevelDirector` to have access to camera position and game width at debug-draw time, which it can get from `GameCore.Camera` and `GameCore.ViewportAdapter`.

### 9. Validate edge cases

- **Camera at level start (X=0)**: Left-side spawn at `-100 - halfWidth` — outside movement bounds. The `BoundsForce` in `EnemyAI` pushes them right into view. Acceptable — enemies enter from the absolute left edge.
- **Scroll-locked waves**: Camera is clamped to `WaveEndX`. Offscreen-right spawns won't drift further right because the camera is locked. Offscreen-left spawns are relative to the locked camera position.
- **Multiple waves, same side**: Enemies pile in from the same direction — separation force handles crowding.
- **First frame facing flicker**: Setting `Direction` and `Sprite.Effect` immediately after `Rent` avoids any wrong-facing frame.
- **Entering state + scroll lock**: Enemies walk toward the play area during `Entering`. If the wave is scroll-locked, they'll walk until the timer expires, then transition to `Idle`. AI then computes steering based on player position within the locked bounds — natural behavior.
- **Enemy killed during entry walk**: `Die` trigger permitted from `Entering` state — enemies can be killed while walking in.

## Task List

1. Add `SpawnSide.cs` enum file
2. Add `SpawnVertical.cs` enum file
3. Modify `EnemySpawnDef` record — replace `Vector2 Position` with `SpawnSide Side, SpawnVertical Vertical`
4. Add `ComputeSpawnPosition` static method to LevelDirector
5. Modify `SpawnWave` to use `ComputeSpawnPosition` and set initial facing + spawn walk direction
6. Add `EnemyState.Entering` — new state, trigger, transitions, callbacks
7. Add spawn-walk timer and fixed-direction movement logic in `EnemyEntity.Update()`
8. Add `SetSpawnWalkDirection` method to `EnemyEntity`
9. Clear spawn-walk state in `EnemyEntity.Reset()`
10. Update `Level1.CreateWaveDefs()` to new format
11. Update `LevelDirectorTests` and `EnemyPoolTests` to use new `EnemySpawnDef` format
12. Add debug drawing for pending spawn points
13. Build and run all tests (add new tests for Entering state transitions and spawn-walk behavior)

## Files Affected

| File | Change |
|---|---|
| `Game/Levels/SpawnSide.cs` | NEW — enum |
| `Game/Levels/SpawnVertical.cs` | NEW — enum |
| `Game/Levels/WaveDef.cs` | Modify `EnemySpawnDef` record |
| `Game/Levels/LevelDirector.cs` | Add `ComputeSpawnPosition`, modify `SpawnWave` to pass spawn-side info to enemies |
| `Game/Levels/Level1.cs` | Update `CreateWaveDefs` |
| `Game/Entities/Enemy/EnemyEntity.cs` | Add `Entering` state handling, spawn-walk timer, `SetSpawnWalkDirection`, update `Reset` |
| `Game/Entities/Enemy/EnemyStateController.cs` | Add `Entering` to enum, `SpawnWalkCompleted` trigger, transitions, ignored triggers |
| `Game.Tests/LevelDirectorTests.cs` | Update `EnemySpawnDef` usage |
| `Game.Tests/EnemyPoolTests.cs` | Update `EnemySpawnDef` usage |
| `Game.Tests/EnemyStateTests.cs` | Add `Entering` → `SpawnWalkCompleted` → `Idle` transition test, `Die` from `Entering` test |
