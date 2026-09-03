# Architecture Audit — MonoGameLearning

Method used: read every `.cs` file in `MonoGameLearning.Core` (~90), `MonoGameLearning.Game` (~37),
plus all `.csproj` files. Cross-project structure verified: `Game → Core` and `Game.Tests → {Game, Core}`.
No project-level circular dependency, and Core contains zero references to `MonoGameLearning.Game`
(the only cross-assembly link is `InternalsVisibleTo` to the test project). All findings below are
verified against source with line numbers.

---

## Smell: Leaky abstraction — `StateMachineController` exposes the raw Stateless machine

**Location:** `MonoGameLearning.Core/StateMachines/StateMachineController.cs:9`
**Problem:** `public StateMachine<TState,TTrigger> StateMachine { get; }` hands callers the wrapped
Stateless object, so the wrapper's own `Fire`/`CanFire`/`IsInState` discipline is bypassable. `GameLoop.cs:92`
then reaches three levels deep (`_gameState.StateMachine.OnTransitioned(...)`).
**Impact:** Consumers mutate configure/transition on the raw machine and bypass the logging "ignored trigger"
guard; the wrapper stops being the single source of truth for FSM access.
**Suggestion:** Make the machine `private` (or expose only the read-only state) and add a
`SubscribeTransitions(Action)` method on the controller. Remove `.OnTransitioned` reach-through from `GameLoop`.

---

## Smell: God class — `CombatActorBase`

**Location:** `MonoGameLearning.Core/Entities/Actor/CombatActorBase.cs:21-266`
**Problem:** One abstract base implements 11 interfaces (render + debug + collision + damage + hitbox +
movement + animation + weapon) and encodes all state-machine choreography in 14 `*Impl` step methods
(`AttackingExitImpl`…`ResetActor`, lines 208-266) that exist only to be invoked by per-entity Game-side
FSM callbacks. It also re-declares empty virtual hooks `OnDeath/OnKnockdown/OnHit` (lines 64-66) that duplicate
the `IDamageResponse` default implementations, and abstract `Phase`/`FirePhaseCompleted` (195-196) force each
subclass to maintain a parallel phase mapping.
**Impact:** Any combat actor change must be harmonized across a giant base and two near-twin subclasses
(`PlayerEntity`/`EnemyEntity`); the render engine, combat math, and state choreography are fused so they cannot
change independently.
**Suggestion:** Drop the empty virtual hooks that duplicate interface defaults — but only `OnKnockdown`/`OnHit`
(only these two have non-abstract `IDamageResponse` defaults at `IDamageResponse.cs:9-10`; `OnDeath` and
`CanTakeDamage` are genuinely re-declared/overridden by `EnemyEntity.cs:87,90`, so they stay). Fold the `*Impl`
helpers into concrete (non-virtual) methods and simplify the `Phase`/`FirePhaseCompleted` abstract seam.
Long-term, split weapon-overlay rendering (`RenderWeaponOverlay` 129-155) out of the combat/state base — it is
the only frame-stepped sprite in the codebase and drags `MeleeWeaponDef` math into the entity.

---

## Smell: Duplicated combat-actor FSM — Player vs Enemy

**Location:** `MonoGameLearning.Game/Entities/Player/PlayerStateMachine.cs:65-127` and
`MonoGameLearning.Game/Entities/Enemy/EnemyStateMachine.cs:85-147`
**Problem:** The five combat states `Attacking/Hurt/KnockedDown/Dying/Dead` have effectively identical transition
tables in both files, distinguished only by different `PlayerState`/`EnemyState` enums, different
`PlayerTrigger`/`EnemyTrigger` enums, the post-combat target state (`Idling` vs `Idle`), and `Ignore` noise for
the other character's exclusive states. The same is true of the callback bundles
(`CombatActorStateMachineCallbacks` + one added state each in `PlayerStateMachineCallbacks`/
`EnemyStateMachineCallbacks`) and of `Phase`/`FirePhaseCompleted`/`CanTakeDamage/OnHit/OnKnockdown/OnDeath`
in `PlayerEntity`/`EnemyEntity`.
**Impact:** Every combat-state rule is written twice; the two actors drift unless edited in lockstep.
**Suggestion:** Extract the shared combat-state sub-FSM (and `CombatActorStateMachineCallbacks`) into Core with
game-side composition for the exclusive states (`Moving/Idling` vs `Entering/Chasing`). Note: the tables are
not literally byte-identical — they use separate state/trigger enums and different return targets — so the shared
Core builder must be parameterized over state/trigger enum types. This is the single largest duplication in the
codebase and the clearest "generic beat-em-up logic living in Game" case.
**Risk:** This touches the state-machine choreography that AGENTS.md flags as a game-breaking-bug surface
(state-machine deadlocks). Extract behind the existing tests (exit/entry subscriber pairing per animation, phase
completion) rather than as a bare sweep, and re-run `dotnet test` after each migrate.

---

## Smell: Misplaced Core class — `CombatActorStateMachineCallbacks`

**Location:** `MonoGameLearning.Game/StateMachines/CombatActorStateMachineCallbacks.cs:5`
**Problem:** A pure abstract bundle of `Action` delegates with zero game content lives in the Game project when
it is the shared combat-callback surface both actors build on.
**Impact:** The generic FSM callback surface cannot be reused by a new project without copying it.
**Suggestion:** Move this class to `MonoGameLearning.Core.StateMachines` and have the two Game-side
`*StateMachineCallbacks` subclasses derive from it there (paired with the FSM extraction above).

---

## Smell: Misplaced Core logic — duplicated sprite pipeline

**Location:** `MonoGameLearning.Game/AnimatedSprites/{PlayerSprite,EnemySprite,OilDrumSprite,BatSprite}.cs`
**Problem:** Four files repeat the identical `static SpriteSheet _spriteSheet` + `_loaded` guard +
`Load(content)` + center-origin `Create()` pattern, each only differing by asset path and frame-defs.
Core already ships `StaticTextureAsset` and `SpriteSheetAnimationExtensions` but has no shared
"atlas → frame-defs → `AnimatedSprite`" helper.
**Impact:** A fifth sprite requires a fifth full copy of the pipeline; the pattern lives where it can't be reused.
**Suggestion:** Add a small Core helper (e.g. a `SpriteSheetFactory` taking asset path + `(key,prefix,frames,loop)`
definitions) and reduce the four Game files to a one-line declaration of their own defs.

---

## Smell: God class — `GameLoop`

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs` (~473 lines)
**Problem:** `GameLoop` is simultaneously the composition root and the orchestrator, additionally owning 4 entity
factories (`CreateProp`/`CreatePickup`/`CreateEnemy`/`ConfigureSpawnedEnemy`), respawn/lives/music logic
(`TryConsumeLife`/`ApplyMusicForState`/`ComputeRespawnPosition`), debug-overlay text assembly, and GoIndicator
wiring. Three helpers are `internal static` solely to make them testable but remain statics on the god class.
**Impact:** The composition root is the least-likely-safe place to change game rules; nearly every feature edit
touches this file, and the static test helpers have no owning type.
**Suggestion:** Extract the entity factories into a dedicated Game-side factory type. For the respawn/life/music
statics (`TryConsumeLife`/`ComputeRespawnPosition`/`ApplyMusicForState`), relocate them to a single small owning
type (or stateless Core helpers where they are generic) — do not invent a separate service per rule; they are
already pure and unit-tested (`GameLoop.cs:301,312,338`).

---

## Smell: Feature envy / Law of Demeter — spawn-walk and pool cleanup reach into enemy internals

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:444-459` (sprite warmup/effects) and
`MonoGameLearning.Game/Levels/EnemyPool.cs:22-29`
**Problem:** `GameLoop.ConfigureSpawnedEnemy`/`CreateEnemy` reach into `enemy.SpriteRenderer.*` and
`enemy.Direction/Width/SetSpawnWalkData` to do the enemy's own visual setup. `EnemyPool.OnReturnEnemy` calls
`enemy.HitboxService.Clear(enemy)`/`ClearAttackDedup(enemy)` — duplicating what
`CombatActorBase.AttackingExitImpl` already performs.
**Impact:** Spawn/hitbox knowledge is split across three owners; the `foo.Service.Op(foo)` chain hides the intended
reset scope.
**Suggestion:** Fold the enemy-facing visual setup (`Direction`, `SpriteRenderer.SetEffect`) into the entity's own
`PrepareSpawn(FacingDirection, float targetX)`, so callers pass raw inputs instead of reaching into
`SpriteRenderer.*`. Keep the spawn-walk *target* computation (`GameLoop.cs:463-467`) on the Game side — it derives
from the camera view, and pushing that into the enemy would couple it to rendering/level geometry. Replace the
pool's manual `HitboxService.Clear/ClearAttackDedup` chain with an entity-owned reset path.
**Implemented:** `GameLoop.ConfigureSpawnedEnemy` → `LevelEntityFactory.ConfigureSpawnedEnemy` calls
`enemy.PrepareSpawn(initialFacing, targetX)`; `EnemyPool.OnReturnEnemy` now calls `enemy.ClearCombatState()`
(shared with `CombatActorBase.AttackingExitImpl`). Clearing stays at return time (not rent time) so a parked
enemy cannot leak hitboxes into `HitboxService.ResolveHits`.

---

## Smell: Leaky abstraction — mutable `List<>` exposed on level data

**Location:** `MonoGameLearning.Core/Levels/Level.cs:12,15,16` and `MonoGameLearning.Core/Levels/WaveDef.cs:5`
**Problem:** `WaveDefs`/`Props`/`Pickups` return `List<T>` and `WaveDef(... List<EnemySpawnDef> ...)` stores a
mutable list inside an immutable-feeling record.
**Impact:** Any caller can mutate level/wave definitions after validation, silently corrupting the level data the
constructor's `Debug.Assert` ordering checks were meant to guarantee.
**Suggestion:** Expose `IReadOnlyList<T>` (or immutable collections) for these surfaces.

---

## Smell: Misplaced gameplay query — `EntityService.FindNearestAliveEnemy`

**Location:** `MonoGameLearning.Core/Entities/EntityService.cs:79-96`
**Problem:** The entity registry (add/remove/typed-list) also implements combat-AI target selection, reaching into
`CombatActorBase`/`Faction` to scan for the nearest living enemy.
**Impact:** The registry now depends on combat concepts; adding an AI target rule forces touching the registry.
**Suggestion:** Move this to the AI (or combat) layer, operating on a query the registry already exposes.

---

## Smell: Excessive coupling — `CameraService` depends on concrete `Entity`

**Location:** `MonoGameLearning.Core/Camera/CameraService.cs:9`
**Problem:** The constructor takes `Entity player` but only ever reads `player.Position.X`.
**Impact:** The camera cannot follow anything that isn't a full `Entity`, and drags the whole entity type into a
geometry-only service.
**Suggestion:** Accept `ISpatial` (or a `Func<Vector2>`/`Func<float>` position getter).

---

## Smell: Leaky abstraction — mutable static global settings state

**Located at:** `MonoGameLearning.Core/Settings/SettingsService.cs:14-15`
**Problem:** `SettingsService` is a `static class` with mutable static `AudioSettings`/`CurrentResolution` that are
read/written implicitly but at different lifecycle points (`GameLoop.cs:42,87-88`). `LoadResolution` (96-97) has a
side effect that also mutates `AudioSettings`.
**Impact:** Order-dependent, globally-mutable state makes settings behavior a hidden precondition of the game loop.
**Implemented (minimal static cleanup, per user direction):** `LoadResolution` no longer writes the static
`AudioSettings`; its fallback persist path (`WriteSettings`) keeps the loaded `data.Audio` intact instead of
snapshotting the static, so a boot-time resolution fallback can't clobber persisted audio. `GameLoop` loads the
startup resolution once (`STARTUP_RESOLUTION`) instead of twice. Static mutable state remains (documented
tradeoff — full instance conversion deferred).

---

## Smell: Inheritance smell — `Level` abstract base adds only storage + validation

**Location:** `MonoGameLearning.Core/Levels/Level.cs:10-53`
**Problem:** `Level` exposes 6 abstract members (`BackgroundCount/EndTriggerX/Props/Pickups/WalkableTopY/
CreateBackgroundRenderer`) fully supplied by the single concrete `Level1`; its only concrete behavior is
`WaveDefs` storage and `ValidateWaveDefs`. No second subclass exists.
**Impact:** The abstraction layer is speculative; it splits one level's data across a base and child for no current
benefit.
**Suggestion:** Collapse `Level` into a concrete record/data type (fields + a free validation function), and have
`Level1` produce it directly (or delete `Level` and inline into `Level1`).

---

## Smell: Dead default — empty `DrawDebug` on `UiBase`

**Location:** `MonoGameLearning.Core/UI/UiBase.cs:15`
**Problem:** `DrawDebug` has an empty default body, so the base implements `IDebugDrawable` for free even though
the default never runs (every `UiBase` subclass overrides it with real geometry:
`PlayerBar.cs:68`, `EnemyBar.cs:141`, `HudRoot.cs:29`, `GoIndicatorEntity.cs:62`).
**Implemented:** `UiBase.DrawDebug` is now `abstract` and its empty default is deleted. All four widgets already
implement it, so the dead default is gone while the `IDebugDrawable` capability stays on the base contract
(`UiBase` remains `IDebugDrawable`; `GoIndicatorTests.UiBase_IsDebugDrawable`/`TestUiEntity` updated).
**Note (`Visible`/`Position` stay):** These are live, not dead — `GoIndicatorEntity` reads/writes both
(`GoIndicatorEntity.cs:33,35,44,50-59`), `GameLoop.cs:182` toggles `_goIndicator.Visible`, and
`GoIndicatorTests` covers the visibility contract (`GoIndicatorTests.cs:60-67`). They must stay.

---

## Smell: Leaky abstraction — `IDamageable.Heal` no-op + contract split across two interfaces

**Location:** `MonoGameLearning.Core/Entities/Prop/PropBase.cs:71`,
`MonoGameLearning.Core/Combat/IDamageable.cs:5-14`,
`MonoGameLearning.Core/Combat/IDamageResponse.cs:3-11`
**Problem:** `PropBase` implements `IDamageable.Heal` as an explicit no-op (props can't heal), and the damage
concept is split across `IDamageable` (`TakeDamage/Heal/IsAlive/Died`) and `IDamageResponse`
(`ReduceHealth/CanTakeDamage/OnDeath/OnKnockdown/OnHit`) with overlapping members (`IsAlive`, death
notification). `Name` is carried by `IDamageable` only so the HUD can label `EnemyBar`.
**Impact:** Two interfaces must be kept in sync to describe one "damageable" concept, and props are forced to stub
members they don't honor.
**Suggestion:** Reconcile into one damage contract; either split `Heal` out of `IDamageable` or drop the
`IDamageable`/`IDamageResponse` split. Move the HUD-label `Name` off the combat capability (e.g. expose it via
the existing `IHudPlayerData`-style snapshot rather than broadening `IDamageable`).

---

## Smell: Feature envy / duplicated interface — `EnemyEntity` re-implements `IPickupDropper`

**Location:** `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs:20,55-56`
**Problem:** `EnemyEntity.Drops`/`CreateDrops()` duplicate `PropBase.Drops`/`CreateDrops()`
(`PropBase.cs:58-60`), which already fully implement `IPickupDropper`. `Drops` is also a public mutable
setter mutated externally by `LevelDirectorCore` and cleared in `Reset`.
**Impact:** Drop behavior is implemented twice and mutated from outside the entity.
**Suggestion:** Centralize `IPickupDropper` in the shared `CombatActorBase` (or a small reusable mixin) instead of
duplicating it in `PropBase` and `EnemyEntity`.

---

## Implementation status (architecture audit — complete)

All priority items from the summary are implemented and verified (`dotnet build --warnaserror` + `dotnet test`,
464 passing). The two deferred decisions (full SettingsService instance conversion) remain documented
tradeoffs; the non-priority findings below (EntityService query, CameraService coupling, IDamageable split,
IPickupDropper duplication) are still open for a future pass.

Implemented and verified:

- **StateMachineController** — raw `StateMachine` made private; `SubscribeTransitions(Action<StateMachine<...>.Transition>)` added; `GameLoop.cs:83` now subscribes through it. Only `.StateMachine` consumer was GameLoop.
- **CombatActorBase** — dropped the `OnKnockdown`/`OnHit` empty virtuals; `PlayerEntity`/`EnemyEntity` re-implement `IDamageResponse` directly. `OnDeath`/`CanTakeDamage` kept virtual (genuinely overridden).
- **CombatActorStateMachineCallbacks** — moved to `MonoGameLearning.Core.StateMachines` (ns nullable-annotated); Game callbacks derive from it there.
- **GameLoop** — entity factories extracted to `Levels.LevelEntityFactory`; `TryConsumeLife`/`ApplyMusicForState`/`ComputeRespawnPosition` moved to `GameLoopRules` (single owning type); startup resolution loaded once.
- **Level/WaveDef mutable lists** — `LevelData.Props/Pickups/WaveDefs` + `WaveDef.Enemies` now `IReadOnlyList<T>`; `SpawnProps` signature updated.
- **Level abstract base** — collapsed into concrete `LevelData` record (Game width/height included so `MovementBounds` is derived in Core); `Level1` is now a static producer + `CreateBackgroundRenderer(Content, LevelData)`; validation in `LevelData.Validate`. `TestLevel` in tests now builds `LevelData` (implicit conversion).
- **Enemy spawn-walk / pool cleanup** — visual setup + walk direction folded into `EnemyEntity.PrepareSpawn(FacingDirection, float targetX)`; `EnemyPool.OnReturnEnemy` → `enemy.ClearCombatState()` (entity-owned; clears at return time so stalled hitboxes can never linger in `HitboxService`).
- **Sprite pipeline** — `Core.Rendering.SpriteSheetAsset` (+ `SpriteAnimationDef`) replaces the four static Load/Create copies; each sprite class is now a def-table + two one-liners.
- **UiBase** — empty `DrawDebug` default removed by making it `abstract` (all four widgets already override with real geometry); `Visible`/`Position` retained (live members).
- **SettingsService** — `LoadResolution` no longer mutates `AudioSettings`; fallback persist keeps the file's own audio; GameLoop loads startup resolution once. (Minimal static cleanup per user direction; instance conversion deferred.)
- **Duplicated Player/Enemy combat FSM** — the five combat states now come from one Core builder:
  `StateMachines.CombatStateMachineConfigurator.ConfigureCombatStates(..., CombatStateSet<TState>,
  CombatTriggerSet<TTrigger>, returnState, movementStart, movementStop)`. `PlayerStateMachine`/
  `EnemyStateMachine` keep only their exclusive states (Idling/Moving vs Idle/Entering/Chasing) and call it,
  deleting ~150 lines of duplicated transition tables. Validated unchanged by the full
  `PlayerStateTests`/`EnemyStateTests`/`StateMachineControllerTests` suites.

Still open (documented findings, not in priority list): `EntityService.FindNearestAliveEnemy` (misplaced combat
query), `CameraService` concrete-`Entity` coupling, the `IDamageable`/`IDamageResponse` contract split, and the
`EnemyEntity`/`PropBase` `IPickupDropper` duplication.
