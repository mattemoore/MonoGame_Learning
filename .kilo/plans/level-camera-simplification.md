# Level, Camera, and Combat Simplification Plan

## Goal

Adopt the simplifying assumption (from `TODO.md` item 1): **every background is exactly `GAME_WIDTH` wide and every fight area is exactly `GAME_WIDTH` wide**. This cascades into dropping per-background `MovementBounds`, `ValidateConnectivity`, per-wave `FightAreaWidth`, `CameraController.LeftBound`/`RightBound`, and the 3-branch entity-bounds override in `GameLoop.Update`. Combat zoning (fight area) becomes a single screen-width tile per wave.

Out of scope: LoadContent/Reset unification, resolution-independence audit, Stateless pattern cleanup, props-as-level-data beyond a minimal `PropSpawnDef` (limited to support the level data cleanup; full prop migration is deferred to a follow-up plan per the user's 'levels + camera + combat only' scope).

---

## Core Simplifying Assumptions

| # | Assumption | Simplification it enables | Trade-off / constraint |
| --- | --- | --- | --- |
| A1 | Every `BackgroundEntity` is exactly `GAME_WIDTH` × `GAME_HEIGHT`, positioned at `y = GAME_HEIGHT/2`, tiled at `x = i * GAME_WIDTH + GAME_WIDTH/2`. | `Level.MovementBounds` becomes `(0, 0, count * GAME_WIDTH, GAME_HEIGHT)` — no `RectangleF.Union` over per-background bounds. `ValidateConnectivity` becomes a single count ≥ 1 check (or vanishes). `BackgroundEntity.MovementBounds` (with its 0.6/0.4 ground-band formula) is no longer needed. | All levels must use a uniform repeating background. Loses per-background ground-y variation. Acceptable for a skeleton. |
| A2 | `WaveDef.FightAreaWidth` is always `GAME_WIDTH`. | Drops `FightAreaWidth` from `WaveDef` record; drops `viewportWidth` parameter from validation; drops the width > viewport check. Fight area = single screen tile aligned to the wave's `TriggerX`. | Cannot have narrow or oversized arenas. Acceptable for a beat 'em up skeleton. |
| A3 | A wave's fight area is centered on `wave.TriggerX`. While locked, camera centers on `TriggerX`. | `CameraController` needs only one nullable `Vector2? LockedCenter`. No `LeftBound`/`RightBound`, no `Left==Right` pinned-camera branch, no `minX > maxX` fallback. | Cannot scroll-lock the camera to anything other than a fixed center point. |
| A4 | Level width is exactly `backgroundCount * GAME_WIDTH`. `EndTriggerX` derives from that. | Drops `Level.MovementBounds` as a `RectangleF` property computation; collapses to a derived int count. `EndTriggerX` becomes a computed property in `Level1` (still abstract on `Level` so other levels can choose). | Levels must be composed of whole screens. |
| A5 | All entity movement bounds during a wave are: (a) `level bounds` when no wave active, (b) `fight area bounds` (single screen tile) when scroll-locked, (c) `level bounds` once wave cleared (player walks to next trigger). | Removes the `PersistentCameraCenter` carry-over logic, the `_clearedFightAreaRightEdge` field, the `ShowGoPrompt` X-comparison in `LevelDirector`. Bounds override collapses from 3 branches to a single derived value. | Cannot have intermediate camera positions between fights; camera must snap to next wave center when fight cleared. Matches beat 'em up expectations. |
| A6 | `WaveDef` is a free record; the only contract is: strictly increasing `TriggerX`, each `TriggerX` lands at a screen boundary (`TriggerX = (i+1) * GAME_WIDTH`). | `ValidateWaveDefs` is removed entirely. Documentation in `WaveDef.cs` states the contract. `Debug.Assert` in `LevelDirector.SpawnWave` checks the boundary contract at runtime in debug builds. | Shifts correctness to convention + debug assert. Acceptable for a single-author learning project. |
| A7 | Backgrounds are rendering-only — they have no gameplay-relevant collision or movement bounds. | `BackgroundEntity` becomes a thin `IRenderable` (sprite + position); `MovementBounds` is dropped from the entity. Background drawing is extracted into a `BackgroundRenderer` (new class) that owns the list of background sprites and renders them. `Level` no longer holds `BackgroundEntity` references. | Pushes background management out of `Level`. Requires a `BackgroundRenderer` registered with `EntityManager` as a renderable (or called directly from `GameLoop.Draw`). |

---

## Architecture Changes

### Before

```
LevelDirector -> owns wave state, CameraController.RightBound, PersistentCameraCenter
Level         -> owns BackgroundEntities, MovementBounds (RectangleF.Union), ValidateConnectivity
GameLoop      -> 3-branch bounds override for entity MovementBounds
CameraController -> LeftBound, RightBound, dual-clamp with edge cases
BackgroundEntity -> owns MovementBounds (0.6/0.4 ground band) + draws self
```

### After

```
LevelDirector        -> owns wave state, drives CameraController.LockedCenter + EntityMovementBounds
Level                -> owns BackgroundCount + WaveDefs + EndTriggerX (abstract); no entity list
BackgroundRenderer   -> new class; owns background sprites; registered as IRenderable
CameraController     -> single LockedCenter; free-scroll clamps to level bounds only
GameLoop             -> single call: _cameraController.ApplyMovementBounds(_entityManager.Movables)
BackgroundEntity     -> slim sprite-only IRenderable; no MovementBounds
```

---

## New / Modified Files

### New: `MonoGameLearning.Game/Levels/PropSpawnDef.cs`

```csharp
namespace MonoGameLearning.Game.Levels;

public record PropSpawnDef(string Type, Vector2 Position);
```

### New: `MonoGameLearning.Game/Rendering/BackgroundRenderer.cs`

Owns background sprites. Replaces the in-`Level` background storage.

```csharp
public class BackgroundRenderer(List<Sprite> sprites, int gameWidth, int gameHeight) : IRenderable
{
    public void Render(RenderContext context)
    {
        float bgY = gameHeight / 2f;
        for (int i = 0; i < sprites.Count; i++)
        {
            float x = i * gameWidth + gameWidth / 2f;
            context.SpriteBatch.Draw(sprites[i], new Vector2(x - gameWidth / 2f, bgY - gameHeight / 2f));
        }
    }
}
```

Static factory takes `ContentManager` for parity with current `Level1.CreateBackgrounds`:

```csharp
public static BackgroundRenderer Create(ContentManager content, int gameWidth, int gameHeight, int backgroundCount)
{
    var sprites = new List<Sprite>(backgroundCount);
    for (int i = 0; i < backgroundCount; i++)
        sprites.Add(new(content.Load<Texture2D>("backgrounds/background1")));
    return new(sprites, gameWidth, gameHeight);
}
```

### Modified: `MonoGameLearning.Core/Entities/BackgroundEntity.cs`

**Drops** `MovementBounds` property and `DrawDebug` override. Becomes a thin sprite holder (kept for parity with current entity patterns; could be inlined, but keeping the type avoids ripple changes).

```csharp
public class BackgroundEntity(string name, Sprite sprite, Vector2 position, int width, int height)
    : Entity(name, position, width, height), IRenderable
{
    Sprite Sprite { get; } = sprite;
    public void Render(RenderContext context)
    {
        if (Sprite is not null) context.SpriteBatch.Draw(Sprite, Frame.Position);
    }
}
```

`BackgroundEntity` is **no longer constructed by `Level1`**. It may still exist in case other code references it (none after refactor — safe to delete in a follow-up if unused).

### Modified: `MonoGameLearning.Game/Levels/WaveDef.cs`

```csharp
public record EnemySpawnDef(string Type, Vector2 Position);

public record WaveDef(float TriggerX, List<EnemySpawnDef> Enemies)
{
    // Contract: TriggerX must be a screen boundary: TriggerX = (i+1) * GAME_WIDTH.
    // TriggerX values must be strictly increasing.
    // FightAreaWidth is implicit (always GAME_WIDTH) and centered on TriggerX.
}
```

`ValidateWaveDefs` method **removed**. All call sites and test coverage removed (see Tests section).

### Modified: `MonoGameLearning.Game/Levels/Level.cs`

```csharp
public abstract class Level
{
    public List<WaveDef> WaveDefs { get; }
    public abstract int BackgroundCount { get; }
    public abstract float EndTriggerX { get; }

    public RectangleF MovementBounds { get; }

    protected Level(List<WaveDef> waveDefs, int gameWidth, int gameHeight)
    {
        WaveDefs = waveDefs;
        MovementBounds = new RectangleF(0, 0, BackgroundCount * gameWidth, gameHeight);
        Debug.Assert(BackgroundCount >= 1, "Level must have at least one background.");
    }

    // No Backgrounds list. No Draw/DrawDebug aggregating backgrounds. No ValidateConnectivity.

    public virtual void DrawDebug(DebugDrawContext context) { }
}
```

### Modified: `MonoGameLearning.Game/Levels/Level1.cs`

```csharp
public class Level1(ContentManager content, int gameWidth, int gameHeight)
    : Level(CreateWaveDefs(), gameWidth, gameHeight)
{
    public override int BackgroundCount => 2;
    public override float EndTriggerX => BackgroundCount * gameWidth - 100f;

    public BackgroundRenderer CreateBackgroundRenderer() =>
        BackgroundRenderer.Create(content, gameWidth, gameHeight, BackgroundCount);

    private static List<WaveDef> CreateWaveDefs() =>
    [
        new WaveDef(TriggerX: 800f, Enemies:
        [
            new EnemySpawnDef("Grunt", new Vector2(850, 550)),
            new EnemySpawnDef("Grunt", new Vector2(900, 550))
        ]),
        new WaveDef(TriggerX: 1600f, Enemies:
        [
            new EnemySpawnDef("Grunt", new Vector2(1650, 550)),
            new EnemySpawnDef("Grunt", new Vector2(1700, 550))
        ])
    ];
}
```

Note: triggers moved to screen boundaries (`800f`, `1600f` instead of `600f`, `1200f`). `EndTriggerX` becomes `2 * 800 - 100 = 1500f` — same value as before.

### Modified: `MonoGameLearning.Game/Levels/LevelDirector.cs`

```csharp
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

    public event Action? LevelCompleted;
    public bool ShowGoPrompt => _waveCleared;
    public int CurrentWaveIndex => _currentWaveIndex;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public bool IsScrollLocked => _isScrollLocked;
    public Vector2? LockedCameraCenter { get; private set; } // null = free scroll
    public RectangleF FightAreaBounds { get; private set; }

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
            LockedCameraCenter = null;
            FightAreaBounds = default;
            _currentWaveIndex++;
        }
    }

    protected virtual void SpawnWave()
    {
        var wave = _level.WaveDefs[_currentWaveIndex];
        Debug.Assert(wave.TriggerX > 0, $"Wave TriggerX must be at a screen boundary; got {wave.TriggerX}.");

        _waveTriggered = true;
        _isScrollLocked = true;
        _waveCleared = false;

        float fightLeft = wave.TriggerX - GameLoop.GAME_WIDTH / 2f;
        FightAreaBounds = new RectangleF(fightLeft, 0, GameLoop.GAME_WIDTH, GameLoop.GAME_HEIGHT);
        LockedCameraCenter = new Vector2(wave.TriggerX, GameLoop.GAME_HEIGHT / 2f);

        foreach (var def in wave.Enemies)
        {
            var enemy = CreateEnemy(def);
            _activeEnemies.Add(enemy);
            _entityManager.Register(enemy);
        }
    }

    protected virtual EnemyEntity CreateEnemy(EnemySpawnDef def) { /* unchanged */ }
    protected virtual void OnEnemyDied(object sender, EventArgs e) { /* unchanged */ }
}
```

**Drops**: `_clearedFightAreaRightEdge`, `PersistentCameraCenter`, `CurrentFightArea` (RectangleF), `IsWaveCleared` (replaced by `ShowGoPrompt`). The `ShowGoPrompt` now only checks `_waveCleared` because the player's position relative to the fight area is no longer needed — the player must be inside the cleared fight area to have triggered the clear in the first place.

### Modified: `MonoGameLearning.Game/GameLoop/CameraController.cs`

```csharp
public class CameraController(PlayerEntity player, int gameWidth, int gameHeight, RectangleF levelBounds)
{
    private readonly PlayerEntity _player = player;
    private readonly int _gameWidth = gameWidth;
    private readonly int _gameHeight = gameHeight;
    private readonly RectangleF _levelBounds = levelBounds;

    private const float SMOOTH_FACTOR = 0.04f;

    public Vector2? LockedCenter { get; set; }

    public void Update(OrthographicCamera camera)
    {
        float targetX;
        float targetY = _gameHeight / 2f;

        if (LockedCenter.HasValue)
        {
            targetX = LockedCenter.Value.X;
        }
        else
        {
            float minX = _levelBounds.Left + (_gameWidth / 2f);
            float maxX = _levelBounds.Right - (_gameWidth / 2f);
            Debug.Assert(minX <= maxX, $"Level width ({_levelBounds.Width}) is smaller than viewport width ({_gameWidth}).");
            targetX = Math.Clamp(_player.Position.X, minX, maxX);
        }

        float halfWidth = _gameWidth / 2f;
        float desiredPos = targetX - halfWidth;
        float newPos = MathHelper.Lerp(camera.Position.X, desiredPos, SMOOTH_FACTOR);
        camera.LookAt(new Vector2(newPos + halfWidth, targetY));
    }

    public void ApplyMovementBounds(IReadOnlyList<IMoveableEntity> movables, RectangleF? fightArea)
    {
        RectangleF bounds = fightArea ?? _levelBounds;
        foreach (var movable in movables)
            movable.MovementBounds = bounds;
    }
}
```

**Drops**: `LeftBound`, `RightBound`, the `LeftBound == RightBound` pinned-camera branch, the `minX > maxX` mid-fallback. **Adds**: `ApplyMovementBounds` helper that replaces GameLoop's 3-branch foreach.

### Modified: `MonoGameLearning.Game/GameLoop/GameLoop.cs`

Key changes only:

```csharp
protected override void Update(GameTime gameTime)
{
    _input.Mode = _gameState.State == GameState.Playing ? InputMode.Gameplay : InputMode.Menu;
    _input.Update(gameTime);

    _entityManager.ProcessPending();

    if (_gameState.State == GameState.Playing)
    {
        _levelDirector.Update(gameTime);
        _cameraController.LockedCenter = _levelDirector.LockedCameraCenter;
        _cameraController.Update(Camera);
        _player.MovementDirection = _input.MovementDirection;

        foreach (var updatable in _entityManager.Updatables)
            updatable.Update(gameTime);

        var hitResults = _hitboxService.ResolveHits(_entityManager.All);
        foreach (var hit in hitResults)
            if (hit.Target is IDamageable damageable)
                damageable.TakeDamage(new DamageInfo { Amount = hit.Damage, Knockdown = hit.Knockdown, Strength = hit.Strength });

        _collision.Update(gameTime);

        // Single call replaces 15-line 3-branch foreach.
        _cameraController.ApplyMovementBounds(
            _entityManager.Movables,
            _levelDirector.IsScrollLocked ? _levelDirector.FightAreaBounds : (RectangleF?)null);
    }

    GumService.Update(gameTime);
    base.Update(gameTime);
}
```

`LoadContent` and `ResetGame`:

```csharp
protected override void LoadContent()
{
    base.LoadContent();
    _debugFont = Content.Load<SpriteFont>("fonts/DebugFont");

    _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);
    var bgRenderer = ((Level1)_currentLevel).CreateBackgroundRenderer();
    _entityManager.Register(/* BackgroundRenderer adapts to IRenderable */);

    PlayerSprite.Load(Content);
    _player = new PlayerEntity("player", new Vector2(100, 450), 2.0f, PlayerSprite.Create());

    EnemySprite.Load(Content);
    OilDrumSprite.Load(Content);

    _collision = CreateCollisionComponent(_currentLevel.MovementBounds);
    _entityManager = new EntityManager(_collision);
    _entityManager.Register(_player);
    _player.Died += OnPlayerDied;

    AssignHitboxService();
    InitLevelSystems();
}
```

`BackgroundRenderer` implements `IRenderable` (it already does above) so it's registered with `EntityManager.Renderables`. This means GameLoop.Draw no longer needs `_currentLevel.Draw(renderCtx)` — backgrounds render via the normal renderable loop.

`ResetGame` becomes a thin re-init:

```csharp
private void ResetGame()
{
    _hitboxService.ClearAll();
    _player.Reset(new Vector2(100, 450));
    _entityManager.Clear();

    _currentLevel = new Level1(Content, GAME_WIDTH, GAME_HEIGHT);
    _entityManager.Register(((Level1)_currentLevel).CreateBackgroundRenderer());

    _collision = CreateCollisionComponent(_currentLevel.MovementBounds);
    _entityManager.SetCollisionComponent(_collision);

    _entityManager.Register(_player);
    AssignHitboxService();
    InitLevelSystems();
    Camera.Position = Vector2.Zero;
}
```

Oil drum registration is **deferred** (see Open Questions). Hardcoded `RegisterOilDrum` calls in `LoadContent` and `ResetGame` are removed for now; a follow-up plan will add `Props` to `Level` and have `LevelDirector` register them.

`Draw()` changes:

```csharp
// In Draw, replace _currentLevel.Draw(renderCtx) call with the renderable loop (already present).
// _currentLevel.DrawDebug(debugCtx) call becomes a no-op (Level.DrawDebug is empty after refactor).
// Update the wave-trigger debug line: use wave.TriggerX instead of original x positions; only the rightmost wave trigger remains relevant when locked.

// Update ShowGoPrompt check: if (_levelDirector.ShowGoPrompt) — no change to drawing logic, only the predicate.
```

### Modified: `MonoGameLearning.Game.Tests/LevelDirectorTests.cs`

- All tests referencing `WaveDef(... FightAreaWidth: ...)` must drop `FightAreaWidth`.
- `TestLevel` ctor drops `viewportWidth` param.
- All `ValidateWaveDefs` test cases in `LevelValidationTests.cs` are **deleted** (method removed).
- The "fight area extends for player when past right edge" tests in `LevelDirectorTests.cs` (`MovementBounds_ExtendForPlayer_WhenPastFightAreaRightEdge` at line 613) are **deleted** — with single-screen fight areas, the player cannot exceed the fight area (clamped by `ApplyMovementBounds`).
- Tests for camera pinning at `LeftBound == RightBound` are **deleted**.
- Tests asserting that the GO prompt hides when player walks past fight area right edge are **deleted** (ShowGoPrompt no longer checks player position).
- New tests:
  - `CameraController_Update_LockedCenter_SnapsToCenter` — verify camera X = LockedCenter.X (after smoothing completes).
  - `LevelDirector_SpawnWave_SetsLockedCameraCenterToTriggerX`.
  - `Level_MovementBounds_EqualsCountTimesGameWidth`.
  - `BackgroundRenderer_Render_DrawsTiledBackgrounds`.

---

## Data Flow (After)

```
GameLoop.Update()
  └─ _levelDirector.Update(gameTime)
       ├─ Player at TriggerX → SpawnWave()
       │    ├─ LockedCameraCenter = (TriggerX, GAME_HEIGHT/2)
       │    ├─ FightAreaBounds = (TriggerX - GAME_WIDTH/2, 0, GAME_WIDTH, GAME_HEIGHT)
       │    └─ Register enemies
       ├─ Enemies dead → Clear: LockedCameraCenter = null, FightAreaBounds = default
       └─ All waves done + player at EndTriggerX → fire LevelCompleted

GameLoop.Draw() (camera space)
  └─ foreach renderable: BackgroundRenderer draws all backgrounds at tile offsets
  └─ foreach renderable: entities draw

GameLoop.Update() movement bounds
  └─ _cameraController.ApplyMovementBounds(_entityManager.Movables,
                                            IsScrollLocked ? FightAreaBounds : null)
```

---

## Combat Zoning Impact

With single-screen fight areas:

1. **Camera lock**: fight area exactly equals one screen. Player cannot scroll past fight right edge (clamped by movement bounds), and camera cannot pan (locked at center). Locked state is simpler to reason about.
2. **Enemy positioning**: enemies spawn within the single screen tile. No half-on-screen spawns.
3. **Death detection**: as soon as `_activeEnemies.Count == 0`, wave clears. Player is guaranteed to be inside the fight area (couldn't have left without dying or being clamped), so `_waveCleared` is sufficient — no need to compare `_player.Position.X` to fight right edge.
4. **GO prompt**: shown whenever `_waveCleared` is true; the player must walk right to reach the next trigger.
5. **Camera smoothing**: the `SMOOTH_FACTOR` lerp means the camera takes ~25 frames to fully snap to `LockedCenter`. During that snap, the player is locked inside the fight area (movement bounds already set), so no visual disconnect.

---

## Risks & Edge Cases

1. **Camera snap timing**: if `LockedCenter` is set and cleared in the same frame (impossible with current trigger check, but possible if all enemies die on the same frame they're spawned), `ApplyMovementBounds` and `Update` could see inconsistent state. `Debug.Assert` in `Update` if `_isScrollLocked && LockedCameraCenter == null`.
2. **Smoothing feels slow**: with `SMOOTH_FACTOR = 0.04f`, locking the camera takes ~25 frames (~0.4s at 60fps). For a beat 'em up this is desirable; not a regression.
3. **Background tiling seam**: standardized same-image tiles will show identical seams if the texture has no variation. Already the case in `Level1` (uses `background1` twice). Not a regression.
4. **`Level1` cast in `GameLoop`**: relying on the concrete type to call `CreateBackgroundRenderer` is mild coupling. Alternative: have `Level` expose `abstract BackgroundRenderer CreateBackgroundRenderer(ContentManager)`. Pick the latter to avoid the cast (cleaner).
5. **Single-screen fight areas may feel cramped**: deferred concern. Trivially relaxed by making `BackgroundCount` larger (e.g., a 3-screen fight = 3 backgrounds in a row, fight area centered on the middle one). Not required for the assumption to hold.
6. **`Level.EndTriggerX` abstract**: a level that wants to end at the literal edge (no margin) can return `BackgroundCount * gameWidth`. The 100f margin is a default, not enforced.
7. **Old `BackgroundEntity` references**: `Level.Draw` and `Level.DrawDebug` are gone (no backgrounds list). `LevelDirector` doesn't reference `BackgroundEntity`. `GameLoop.Draw` calls `_currentLevel.Draw(...)` and `_currentLevel.DrawDebug(...)` — these must be removed from GameLoop.Draw.

---

## Validation

1. `dotnet build` clean.
2. `dotnet test` clean (existing + new tests).
3. Manual play-test: walk right → first fight triggers → camera locks → enemies spawn → kill all → "GO ->" appears → walk right → second fight → kill all → walk to end → LevelComplete screen.
4. Debug overlay: wave trigger lines, end trigger line, active fight area rectangle all draw correctly.
5. Resolution change: at a different `RESOLUTION_WIDTH/HEIGHT`, the game still runs (virtual resolution unchanged). No new resolution dependencies introduced.

---

## Implementation Order

1. `WaveDef.cs` — drop `FightAreaWidth`, drop `ValidateWaveDefs`.
2. `BackgroundRenderer.cs` — new file.
3. `BackgroundEntity.cs` — drop `MovementBounds` and `DrawDebug`.
4. `Level.cs` — drop `Backgrounds`, `ValidateConnectivity`, `Draw`, `DrawDebug`; add `abstract CreateBackgroundRenderer`; simplify `MovementBounds`.
5. `Level1.cs` — implement new abstract members; update wave triggers; drop `CreateBackgrounds`.
6. `LevelDirector.cs` — replace `CurrentFightArea` + `PersistentCameraCenter` with `FightAreaBounds` + `LockedCameraCenter`; drop `IsWaveCleared`; simplify `ShowGoPrompt`; simplify clear logic.
7. `CameraController.cs` — drop `LeftBound`/`RightBound`; add `LockedCenter`; add `ApplyMovementBounds`; drop edge cases.
8. `GameLoop.cs` — register `BackgroundRenderer` in `LoadContent`/`ResetGame`; remove `_currentLevel.Draw/DrawDebug` calls; replace bounds-override foreach with single `ApplyMovementBounds` call; assign `_cameraController.LockedCenter` in Update.
9. Tests — update `LevelDirectorTests`, delete `LevelValidationTests`, add new tests for `CameraController` and `BackgroundRenderer`.
10. `dotnet build` → `dotnet test` → manual play.

---

## Open Questions (for implementation agent)

1. **Where do oil drums go?** The user scoped this plan to "levels + camera + combat only" but answered "Level-level Props list" when I raised the question. To honor that answer minimally without expanding scope, add `public abstract List<PropSpawnDef> Props { get; }` to `Level`, implement it in `Level1` (return the 3 hardcoded drum positions), and have `LoadContent`/`ResetGame` register them by looping over `Level1.Props` and calling `RegisterOilDrum`. This is the minimal touch that keeps the data-driven direction. Full prop lifecycle (destroyed-on-death, faction assignment) is out of scope.
2. **`SMOOTH_FACTOR` interaction with locked state**: should `LockedCenter` snap immediately (skip lerp) or smoothly transition? Current plan keeps lerp. If snap-immediately is preferred, the `Update` branch can call `camera.LookAt(LockedCenter.Value)` directly when locked.
3. **`BackgroundRenderer` as `IRenderable`**: registering it with `EntityManager` works but feels semantically off (backgrounds aren't entities). Alternative: call it directly from `GameLoop.Draw`. The plan assumes `IRenderable` registration for now; if cleanliness matters more, switch to direct call.