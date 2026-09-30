# Architecture Audit Fixes (Refined)

## 1. Leaky Abstraction — `EntityPool<TEnemy>` exposes internal pool data structures as `protected`

**Location:** `MonoGameLearning.Core/Levels/EntityPool.cs:20-21`

**Problem:** `protected readonly Dictionary<string, Stack<TEnemy>> Free` and
`protected readonly Dictionary<TEnemy, string> EntityType` expose mutable internal pool state to
subclasses. The only subclass `EnemyPool` never reads or writes these fields directly, so every
future subclass receives accidental write access to pool internals (sentinel-position invariant,
stack ordering, type registry).

**Suggestion:** Make both fields `private`. If a future subclass genuinely needs read access, add a
targeted `protected` accessor (e.g., `protected IEnumerable<TEnemy> GetFreeEnemies(string type)`).

---

## 2. Leaky Abstraction — `CombatActorBase` exposes implementation details as `public`/`protected readonly`

**Location:** `MonoGameLearning.Core/Entities/Actor/CombatActorBase.cs:30-34`

**Problem:** `SpriteRenderer` is `public readonly` (any external code can replace the sprite or
change its scale). `HealthComponent`, `FrameTracker`, `Animations`, `Audio` are `protected
readonly` — every subclass can call `HealthComponent.SetToMax()` directly (bypassing
`ResetActor`), mutate `FrameTracker` internals, or use `Audio` to bypass the base class's event
subscriber lifecycle.

**Impact:** Subclasses can corrupt invariants that the base class depends on (e.g., resetting the
frame tracker without resetting the current move, or mutating health directly without going
through the damage pipeline). Adding a new actor type requires auditing that it doesn't misuse
these fields.

**Suggestion:** Make all five fields `private readonly`. Add targeted `protected` accessors only
where subclasses demonstrably need them:

- `FrameTracker.Reset()` → `protected void ResetFrameTracker()`
- `HealthComponent.{Value,IsAlive,SetToMax}` → already exposed via `Health`, `IsAlive`,
  `ResetActor`
- `Animations` string constants → `protected string IdleAnimation => Animations.Idle` (or inline
  the constants in the subclass)
- `Audio.PlaySfx(sfx)` → `protected void PlaySound(SfxId id)`
- `SpriteRenderer` → add `protected` read-only property or keep public with `{ get; private set; }`

---

## 3. Excessive Coupling — `HitboxService` and `EntityService` cast to `CombatActorBase` for faction checking

**Location:**

- `MonoGameLearning.Core/Combat/HitboxService.cs:79`
- `MonoGameLearning.Core/Entities/EntityService.cs:85`

**Problem:** `HitboxService.ResolveHits` checks `tgt is CombatActorBase { Faction: var
targetFaction }` and `EntityService.FindNearestAliveEnemy` checks `_all[i] is CombatActorBase {
IsAlive: true, Faction: Faction.Enemy }`. Both use a concrete-type pattern match to access a
property (`Faction`) that is not on any shared interface, coupling these generic services to the
specific entity type `CombatActorBase`.

**Impact:** Any new entity type with a faction that does not extend `CombatActorBase` (e.g., a
future `TurretEntity`, `DestructibleWall`) will bypass friendly-fire checks and be invisible to
`FindNearestAliveEnemy`. The service's domain logic is polluted by knowledge of a concrete class
hierarchy.

**Suggestion:** Add `Faction Faction { get; }` to `IHitboxProvider` (or a new `IFactionMember`
interface), then query for it:

```csharp
if (tgt is IFactionMember { Faction: var targetFaction } && active.OwnerFaction == targetFaction) continue;
```

`EntityService` would use the same interface. `CombatActorBase` and `ProjectileEntity` would
implement it.

---

## 4. Inheritance Smell — `EnemyStateMachineCallbacks` and `PlayerStateMachineCallbacks` are near-empty subclasses

**Location:**

- `MonoGameLearning.Game/StateMachines/EnemyStateMachineCallbacks.cs:6-11`
- `MonoGameLearning.Game/StateMachines/PlayerStateMachineCallbacks.cs:6-9`

**Problem:** `EnemyStateMachineCallbacks` adds 3 `Action?` properties
(`OnChasingEntry`, `OnEnteringEntry`, `OnEnteringExit`); `PlayerStateMachineCallbacks` adds 1
(`OnMovingEntry`). Neither has any behavior, only data slots. The base class
`CombatActorStateMachineCallbacks` already uses `init`-only setters so all slots are optional.

**Impact:** Two extra source files and two extra classes for 4 total callback slots. Adding a new
callback pattern requires either modifying both subclasses or the base class anyway.

**Suggestion:** Fold the extra four callback slots into the base class
`CombatActorStateMachineCallbacks` and delete the two subclasses. The only callers
(`EnemyEntity.CreateStateController` and `PlayerEntity.CreateStateController`) and their
`EnemyStateMachine.Create`/`PlayerStateMachine.Create` factory methods can reference the base
class directly.

---

## 5. Inheritance Smell — `LevelDirector` and `EnemyPool` exist only for wiring

**Location:**

- `MonoGameLearning.Game/Levels/LevelDirector.cs:16-30`
- `MonoGameLearning.Game/Levels/EnemyPool.cs:10-22`

**Problem:** `LevelDirector` only overrides `InitializePool()` (1 logical line — create pool and
call `Build(Level)`) and is otherwise a conduit for 10 constructor parameters to the base class.
`EnemyPool` only overrides `OnRentEnemy` (1 line — `enemy.Reset(position, target)`) and
`OnReturnEnemy` (1 line — `enemy.ClearCombatState()`). The base class
`LevelDirectorCore<TEnemy>` already accepts 6 factory delegates in its constructor.

**Impact:** Two extra files and two extra classes whose entire behavior could be captured by adding
one or two more delegate parameters to the base class's constructor.

**Suggestion:** Remove both subclasses. Add an `Action<EntityPool<EnemyEntity>>` buildPool
delegate to `LevelDirectorCore<TEnemy>`'s constructor. `GameLoop.InitLevelSystems` would pass
the two-liner inline:

```csharp
_levelDirector = new LevelDirectorCore<EnemyEntity>(
    ..., 
    pool => { pool = new EntityPool<EnemyEntity>(...); pool.Build(level); }
);
```

---

## 6. God Class — `GameLoop` orchestrates 15+ service domains in 386 lines

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:31-386`

**Problem:** `GameLoop` directly manages input, audio, state-machine setup, menu construction,
content loading (fonts, sprites, weapons, pickups, HUD), level creation, background rendering,
collision world setup, entity service creation, projectile service creation, camera updates,
movement bounds, entity updates, hitbox resolution, projectile updates, collision resolution,
pickup resolution, HUD proximity tracking, respawn/lives management, debug overlay, and the entire
rendering pipeline (two separate `SpriteBatch.Begin/End` blocks). Methods:

- `Initialize` (33 lines): sets up input, audio, settings, hitbox service, game state machine,
  menu service
- `LoadContent` (50 lines): loads every asset, creates player, creates HUD, creates action
  handlers, creates entity factory, reinitializes level
- `Update` (64 lines): sequential pipeline of 15+ service calls in precise order
- `Draw` (72 lines): two-phase rendering with camera and UI transforms
- `ReinitLevel` (22 lines): creates level data, background, collision world, entity/projectile
  services, calls `InitLevelSystems`
- `InitLevelSystems` (17 lines): creates level director and camera service

**Impact:** Any new system (particles, second player, weather, AI director, save system) requires
modifying this single class. The `Update` method's service ordering is implicit — the wrong order
produces bugs that are hard to diagnose. The class cannot be tested without a live window because
all dependencies are wired inline.

**Suggestion:** Extract into separate classes:

- **`GameUpdatePipeline`** (or `FrameProcessor`): encapsulates the ordered `Update` sequence
  (entity lifecycle → level director → camera → input → movement → snapshot → entity update →
  hit resolution → projectiles → collisions → pickups → HUD). `GameLoop` would call
  `pipeline.Update(gameTime)`.
- **`LevelBootstrapper`**: encapsulates `ReinitLevel` + `InitLevelSystems` logic — creates level
  data, background, collision world, entity service, projectile service, level director, camera.
- **`GameRenderPipeline`**: encapsulates the camera-space and screen-space render passes.

This keeps `GameLoop` as a thin composition root that wires services together without knowing the
frame-pipeline ordering.

---

## 7. Misplaced Core Class — `SettingsService` is a static class with mutable global state

**Location:** `MonoGameLearning.Core/Settings/SettingsService.cs:12-124`

**Problem:** All members are `public static`: `AudioSettings`, `CurrentResolution`, `Load`,
`Save`, `Apply`. Tests that read `SettingsService.AudioSettings` after another test wrote to it
get dirty state (order-dependent failures). The static state also prevents using Core in a
headless context (e.g., a server-side replay processor).

**Impact:** Tests cannot be parallelized or run in arbitrary order. Every new feature that needs
settings must couple to this static singleton.

**Suggestion:** Convert to an instance class with an `ISettingsProvider` interface for the
file-I/O part. Inject the `SettingsService` instance where needed (currently `GameLoop` and
`MenuService`).

---

## 8. Mixed Concerns — `EnemyEntity.FirePhaseCompleted` sets an AI cooldown

**Location:** `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs:50`

**Problem:** The `FirePhaseCompleted` method sets `_ai.AttackCooldown = 1.5f` in the
`default` branch (attack completion). This mixes AI state management into the
phase-completion callback that is fired from the base class's animation event handler. The
cooldown belongs in the attacking-exit callback (`OnAttackingExit`), which already runs when the
attack state is left.

**Impact:** If a new state is added that also calls `FirePhaseCompleted` in its default branch,
the AI cooldown fires unintentionally. Future maintainers must understand that
`FirePhaseCompleted` is an AI concern in addition to a state-machine concern.

**Suggestion:** Move `_ai.AttackCooldown = 1.5f` from `FirePhaseCompleted` into the
`OnAttackingExit` callback at `EnemyEntity.cs:121`.

---

## 9. Duplicate Boilerplate — `PlayerEntity` and `EnemyEntity` implement `IDamageResponse.OnKnockdown` and `OnHit` with nearly identical patterns

**Location:**

- `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs:114-126`
- `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs:97-107`

**Problem:** Both subclasses implement `OnKnockdown` and `OnHit` with the same three-step pattern:

1. Store `LastImpactSfx = info.ImpactSfx`
2. Optionally set a timer (player only)
3. Fire the corresponding state-machine trigger

The base class `CombatActorBase` already has protected `*Impl()` methods for entry/exit callbacks
and provides `LastImpactSfx`, but the `IDamageResponse` methods are entirely duplicate.

**Impact:** Adding a third actor type (e.g., boss, ally NPC) requires copying the same 4-6 lines
with only the trigger enum name changing.

**Suggestion:** Add `protected virtual` implementations for `OnKnockdown` and `OnHit` to
`CombatActorBase`. The base version sets `LastImpactSfx` and fires the trigger through a new
abstract method or protected property:

```csharp
protected abstract void FireTakeDamageTrigger();
protected abstract void FireTakeKnockdownTrigger();
```

Subclasses provide one-liner trigger invocations. Player's extra invincibility timer is handled
by overriding the base `OnHit`/`OnKnockdown` and calling `base.`
