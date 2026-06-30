# Enemy Pool Preload Plan

## Goal

Eliminate first-wave gameplay hitches caused by lazy `EnemyEntity`, `AnimatedSprite`, and `Stateless.StateMachine` allocations. Pre-allocate and warm-up enemies + sprite animation controllers at level load; rent/return them across waves.

## Locked Design Decisions

1. **Reusable per-type pool** (`EnemyPool`) owned by `LevelDirector`. Pool size = max enemies of each type in any single wave def (waves run sequentially, so this is the upper bound of concurrent demand).
2. **Warm-up at pool construction**: each instance's `Sprite.SetAnimation()` is called for every animation key in its `AnimationSet` to pre-build the per-instance `IAnimationController`s (addresses the AGENTS.md controller-replacement pitfall).
3. **`EnemyStateController.ResetToRoot()`** reconstructs the `Stateless.StateMachine` by re-running the configuration logic. Trade-off accepted: one allocation per Rent (cheap; happens per-wave, not per-frame). Replaces an earlier external-state-storage idea that added shadow-state plumbing and drift risk.
4. **Factory is an explicit switch** in `EnemyPool` keyed by type string. No reflection. Adding a 10th enemy type = one switch case + one Sprite loader.

## Files to Change

### New: `MonoGameLearning.Game/Levels/EnemyPool.cs`

- Generic key: enemy type string.
- Holds `Dictionary<string, Stack<EnemyEntity>> _free`, plus a master list of all pool instances (for warm-up + clean teardown on `ReinitLevel`).
- Ctor signature: `EnemyPool(EntityManager entityManager, Level level, ContentManager content)` (Content may not be needed if sprite sheets are static-loaded — verify during implementation; if already loaded by `GameLoop.LoadContent`, drop the parameter).
- `Build()` (called from `LevelDirector` ctor): scans `level.WaveDefs`, computes max count per type, calls factory for each, walks every animation key on each instance's `Sprite`, parks instance at sentinel `(-99999, -99999)`, pushes to free stack.
- `Rent(string type, Vector2 position, Entity target)`: pops from free stack, calls `enemy.Reset(position, target)`, registers with `EntityManager`, returns instance. Throws `InvalidOperationException` if pool empty for that type (caller mis-sized).
- `Return(EnemyEntity enemy)`: removes from `EntityManager` (via `_entityManager.Destroy`), unsubscribes any leftover events, calls `HitboxService.Clear(this)` + `ClearAttackDedup(this)`, repositions to sentinel, pushes back to free stack.
- Type → ctor switch lives in `EnemyPool.Build()` (or extracted private factory). Initial: `"Grunt" => new EnemyEntity($"grunt_pool_{i}", sentinel, 2.0f, EnemySprite.Create())`.

### Edit: `MonoGameLearning.Game/Levels/LevelDirector.cs`

- Add `private readonly EnemyPool _enemyPool;` field.
- Ctor: after `_player` is assigned, build pool: `_enemyPool = new EnemyPool(_entityManager, _level); _enemyPool.Build();`
- `SpawnWave()`: replace `_entityManager.Register(enemy)` + `_activeEnemies.Add(enemy)` with `var enemy = _enemyPool.Rent(def.Type, def.Position, _player); _activeEnemies.Add(enemy); enemy.Died += OnEnemyDied;`
- `OnEnemyDied`: after `_entityManager.Destroy(enemy)`, call `_enemyPool.Return(enemy)` before the unsubscription (or restructure so `Return` handles unsubscription).
- Remove `CreateEnemy(EnemySpawnDef def)` — pool owns construction.

### Edit: `MonoGameLearning.Game/Entities/Enemy/EnemyStateController.cs`

- Extract existing constructor body (after `StateMachine = new StateMachine<...>(EnemyState.Dummy);`) into private `ConfigureStateMachine(EnemyStateEntryCallbacks callbacks)`.
- Ctor: `StateMachine = new(...); ConfigureStateMachine(callbacks); StateMachine.Activate();`
- New `public void ResetToRoot()`:
  ```csharp
  public void ResetToRoot()
  {
      ConfigureStateMachine(_callbacks); // need to retain callback reference; store as field
      StateMachine.Activate();
  }
  ```
- Add `private readonly EnemyStateEntryCallbacks _callbacks;` field set in ctor and passed to `ConfigureStateMachine`.

### Edit: `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs`

- Extend `Reset(Vector2 position, Entity target)` to call `_stateController.ResetToRoot()` after `ResetActor(position)`.
- Order: `ResetActor(position)` → `_stateController.ResetToRoot()` → `_ai.Reset()` → `Target = target` → `Sprite.Color = Color.Red`. (Visual reset before state reset ensures sprite animations don't fire on stale state.)

### No change to: `MonoGameLearning.Game/GameLoop/GameLoop.cs`

The existing `EnemySprite.Load(Content)` call in `LoadContent` already loads sprite sheets at startup. `EnemyPool.Build()` only needs to call `EnemySprite.Create()` per instance.

## New Tests: `MonoGameLearning.Game.Tests/`

If `MonoGameLearning.Game.Tests` doesn't exist, create the project (xUnit, references `MonoGameLearning.Game` + `MonoGameLearning.Core` + `Stateless`).

- `EnemyPoolTests`:
  - `Rent_EmptyPoolForType_Throws`
  - `Rent_ReturnsInstanceResetToIdleAndFullHealth`
  - `Return_ThenRent_GivesBackSameInstance`
  - `Build_PrewarmsAllAnimationControllers` — assertion: after `Build`, calling `Sprite.SetAnimation(key)` for each known key does NOT replace `Sprite.Controller` (i.e., controller identity is stable across calls). Use a per-key controller reference taken during warm-up and compare.
- `EnemyStateControllerTests`:
  - `ResetToRoot_FromDead_ReturnsToIdle` — fire `Die` → `DeathCompleted` → state == Dead → call `ResetToRoot()` → state == Idle, Idle `OnEntry` was fired (verify via callback spy).
  - `ResetToRoot_AllowsActivateToFireAgain` — confirms `Fire(Activate)` from reset `Dummy` succeeds.

## Validation

1. `dotnet build` — must compile.
2. `dotnet test` — all new + existing tests pass.
3. Manual gameplay check: run `dotnet run --project MonoGameLearning.Game`, enable debug overlay (F3 or `InputAction.Debug`), cross first wave `TriggerX` — observe `FPSCounter.FramesPerSecond` in debug window. Pre-fix shows a dip; post-fix should stay at vsync (typically 60).
4. Visual sanity: pool instances parked at sentinel must never appear on screen (camera bounds check in `GameLoop.Draw` filters them since `(-99999, -99999)` is outside any camera frustum).

## Risks

- **`PlayAnimation` subscription pattern**: `CombatActorBase.PlayAnimation()` does the unsubscribe→Set→subscribe dance (per AGENTS.md pitfall). Enemy state entry callbacks already use `PlayAnimation()` where subscription is needed (Attacking) and `Sprite.SetAnimation()` where it's not (Idle, Chasing, etc.). No changes needed; verify with a test that Idle→Attack→Idle does not orphan event handlers.
- **Order of operations on Rent**: `ResetToRoot()` must happen before `EntityManager.Register()` so the collision world never sees a stale state.
- **Pool size under-estimation**: if a level ever has overlapping waves or scripted spawns beyond the wave defs, `Rent` will throw. Document that pool size comes from wave defs only; future scripted-spawn support will need a different sizing mechanism.
- **`HitboxService` registration**: `HitboxService` is currently injected into actors by `GameLoop.AssignHitboxService()` after pool construction. Pool instances must also receive a `HitboxService` reference so `Return()` can clear hitboxes. Either pass `HitboxService` into `EnemyPool.Build()`, or have pool instances inherit it via the `EntityManager`/assignment pass. Verify during implementation.

## Out of Scope

- Object pooling for `OilDrumEntity` (already eager in `ReinitLevel`; no gameplay-time allocation).
- Object pooling for `PlayerEntity` (single instance).
- Generic reflection-based factory for enemy types.
- Cross-level pool persistence (pools are rebuilt per `ReinitLevel` via `LevelDirector` reconstruction).
- Animation-controller identity caching beyond the warm-up pass (could be added later if profiling shows repeat warm-ups are still costly after Rent).