# Architecture Audit — Findings & Simplification Plan

**Scope:** Full read of every `.cs` file in `MonoGameLearning.Core` (76 files),
`MonoGameLearning.Game` (25 files), and inspection of `.csproj` files.

**Conclusion:** No project-level circular dependencies. The dependency graph is
strictly `Game → Core` and `Game.Tests → Game + Core`; `AudioGenerator` is
standalone. Core contains zero references to `MonoGameLearning.Game`
(verified by grep). One namespace has no `.Core.Tests` project (the directory is
empty); all tests live in `MonoGameLearning.Game.Tests`.

Findings are ordered by impact; each includes a concrete simplification. Several
are pure removals (dead code), which should be done first.

---

## Smell: Misplaced Core Class (content coupling)

**Location:** `MonoGameLearning.Core/Audio/AudioService.cs:142-166`
**Problem:** `AudioService.OnGameStateChanged` hardcodes the `GameState` → music
track mapping (`TitleScreen`/`Settings`→`TitleMenu`, `Playing`→`Gameplay`, …),
coupling the generic audio engine to one game's arcade-shell states. This was the
explicitly deferred "task 4" of the already-implemented
`audioservice-content-coupling` plan — the asset-path leak was fixed, but the
state→music mapping remains in Core.
**Impact:** Any future game reusing `AudioService` inherits this game's music
selection logic; audio policy is spread across the reusable layer.
**Suggestion:** Remove `OnGameStateChanged` and drive music from `GameLoop`'s
existing `OnTransitioned` handler (`GameLoop.cs:91-98`), calling
`_audio.PlayMusic(...)` / `_audio.SetPaused(...)` with the mapping owned by the
composition root. Delete/relocate the `AudioServiceTests` cases that assert this
mapping.

## Smell: Dead code (registry state never read)

**Location:** `MonoGameLearning.Core/Entities/EntityService.cs:24,38,123,150`
**Problem:** `_damageables` (`List<IDamageable>`) is populated in `AddToTypedLists`,
removed in `RemoveFromTypedLists`, and cleared in `Clear`, but has no public
accessor and is never read (`FindNearestAliveEnemy` iterates `_all`; nothing else
consumes the list). It is a leftover from the earlier `_combatants`/`_damageables`
split.
**Impact:** Dead state that must be maintained on every register/remove, and
misleads readers into thinking damageables are queried by that list.
**Suggestion:** Delete `_damageables`, its `Clear`, and the `TryAdd`/`TryRemove`
`IDamageable` lines in `EntityService`.

## Smell: Leaky Abstraction / Law of Demeter (reach-through indexing)

**Location:** `MonoGameLearning.Game/GameLoop/MenuService.cs:69-83` (casts
`(TextRuntime)_titleScreen.Children[2..4]`); helper at
`MonoGameLearning.Core/UI/GumUiService.cs:30`
**Problem:** `GumUiService.CreateScreen` returns a raw `ContainerRuntime` whose
layout (child 0 = background, 1 = title, 2+ = option text) is an implementation
detail. `MenuService` depends on that exact ordering and index range to recover
selectable items, and mutates them in `UpdateMenuCursor` (`:333-340`). The
consumer must understand the facade's internals.
**Impact:** Any reordering/addition of elements in `CreateScreen` silently breaks
menu navigation and cursor rendering; a Core facade carries game-menu-specific
layout.
**Suggestion:** Move the title/option screen construction out of `GumUiService`
into `MenuService` (which already builds its settings screen the same way),
keeping `GumUiService` a thin Gum wrapper. Delete `GumUiService.CreateScreen`;
then `MenuService` owns its own children and there is no index-cast coupling.

## Smell: Misplaced Core Class (engine collision setup)

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:480-496`
**Problem:** `CreateCollisionWorld` builds the actors/props/pickups `CollisionWorld2D`
and its layer-pair rules using only MonoGame.Extended and Core's `CollisionLayers`.
It is generic beat-'em-up engine plumbing living in the Game composition root.
The sibling methods `ResolveCollisions` (`:295-307`, actor-vs-prop MTV pushback)
and `ResolvePickupOverlaps` (`:309-326`, pickup AABB overlap) are equally generic
and likewise trapped.
**Impact:** Engine-level collision infrastructure is not reusable; the composition
root is bloated with logic another project would copy verbatim.
**Suggestion:** Move `CreateCollisionWorld` into Core (e.g. a `CollisionWorldFactory`
in `Core.Entities` or a factory on `EntityService`). Move pickup-overlap
resolution into Core as a small `PickupService` (or onto `EntityService`),
parameterized by the player and the `PlaySfx` callback, and call it from
`GameLoop`.

## Smell: God Class

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs` (~497 lines)
**Problem:** `GameLoop` mixes the composition root (factory wiring, `CreateProp`/
`CreatePickup`/`CreateEnemy`/`ConfigureSpawnedEnemy`, `InitLevelSystems`) with
per-frame gameplay orchestration (level director, camera, hitbox resolution,
collision + pickup resolution, respawn) and rendering (backgrounds, entities,
UI, debug), plus generic engine concerns (`CreateCollisionWorld`, `Resolve*`).
**Impact:** Hard to unit-test and hard to extract reusable logic; the single class
is the catch-all for every subsystem.
**Suggestion:** After promoting the collision/pickup logic (above), extract the
remaining frame orchestration into a small `GameplaySession`/`CombatDirector`
coordinator so `GameLoop` is reduced to lifecycle + wiring. Do not add this
layer until the generic pieces are out; the goal is a thinner composition root.

## Smell: Misplaced Core Class (sprite pipeline utility)

**Location:** `MonoGameLearning.Game/AnimatedSprites/SpriteSheetAnimationExtensions.cs:17`
**Problem:** `DefineFrames` (define an animation from a `{prefix}-NN` run of atlas
regions) is generic pipeline glue with no game content, depending only on
MonoGame.Extended.Graphics, yet it lives in Game and is `internal`.
**Impact:** A reusable sprite-pipeline utility is trapped in the Game project and
would be re-copied by any new game.
**Suggestion:** Move `SpriteSheetAnimationExtensions` to Core (e.g.
`MonoGameLearning.Core.Rendering`), leaving the content-specific `*Sprite`
sheets (`PlayerSprite`/`EnemySprite`/`OilDrumSprite`/`BatSprite`) in Game.

## Smell: Leaky Abstraction (dead interface adapters)

**Location:** `MonoGameLearning.Core/Entities/Prop/PropBase.cs:15,67-75`
**Problem:** `PropBase` implements `IDamageable` and `IDamageResponse`. The
explicit `IDamageResponse` members (`ReduceHealth`, `OnDeath`, `IsAlive`) are
unreachable — props are damaged only through the abstract
`IDamageable.TakeDamage` override (`OilDrumEntity.TakeDamage`), never through
`CombatService.ApplyDamage(IDamageResponse)` (whose only production caller is
`CombatActorBase.TakeDamage`). `IDamageable.Heal` is an empty no-op.
**Impact:** Dead adapter members that double the apparent damage surface and
mislead about how props actually take damage.
**Suggestion:** Drop `IDamageResponse` from `PropBase` (props have no
`OnKnockdown`/`OnHit` semantics); keep only `IDamageable` with the abstract
`TakeDamage`. Removes ~8 lines of dead explicit adapters.

## Smell: Inheritance / duplication (base entry-logic not reused)

**Location:** `MonoGameLearning.Core/Entities/Actor/CombatActorBase.cs:207-247`;
`PlayerEntity.cs:131-147`; `EnemyEntity.cs:116-132`
**Problem:** The base defines `private *EntryImpl` methods (`HurtEntryImpl`,
`KnockdownEntryImpl`, `DyingEntryImpl`) wrapped by `protected virtual
On*EntryHook` methods. Production subclasses (`PlayerEntity`/`EnemyEntity`) do
**not** use those entry hooks — they inline the same logic (set phase, unequip
weapon, play animation) plus audio. Only the test doubles
(`TestPlayerEntity`/`TestEnemyEntity`) wire `On*EntryHook`. So the base's
guarded entry logic is duplicated inline by its subclasses rather than reused.
**Impact:** Knockdown/dying state transitions are implemented twice, once in the
base and once per subclass; drift between them is a correctness risk.
**Suggestion:** Make the base's entry transitions the single source of truth —
e.g. promote `HurtEntryImpl`/`KnockdownEntryImpl`/`DyingEntryImpl` to callable
`protected` steps so subclasses call them and then add only audio, or move the
audio hook into an overridable `protected virtual` point. Delete the
`On*EntryHook` indirection that only test doubles use.

## Smell: Leaky Abstraction (shared callback bag with reciprocal null-guards)

**Location:** `MonoGameLearning.Game/StateMachines/ActorStateMachineCallbacks.cs`;
`PlayerStateMachine.cs:35-39`; `EnemyStateMachine.cs:38-46`
**Problem:** One `ActorStateMachineCallbacks` bag holds callbacks for both actor
kinds (`OnMovingEntry` is player-only; `OnChasingEntry`/`OnEnteringEntry`/
`OnEnteringExit` are enemy-only). Each machine defensively asserts the other's
callbacks are null rather than declaring the shape it actually needs.
**Impact:** The config type is a superset of both consumers; using the wrong
callback compiles silently and is only caught by a runtime `Debug.Assert`.
**Suggestion:** Split into `PlayerStateMachineCallbacks` and
`EnemyStateMachineCallbacks` (or pass distinct per-machine delegate records) so
each machine's callback surface is explicit and the null-asserts are deleted.

## Smell: Duplication (two sources of truth for lives)

**Location:** `MonoGameLearning.Core/UI/HudLayoutConstants.cs:14` vs
`MonoGameLearning.Game/Entities/Player/PlayerEntity.cs:16`
**Problem:** `INITIAL_LIVES = 3` in Core's `HudLayoutConstants` is referenced
nowhere in Core (game balance data that leaked into the reusable UI layer) and is
duplicated by `PlayerEntity.InitialLives` in Game, which is the value actually
used.
**Impact:** Two constants for the same balance value; changing one does not change
the other.
**Suggestion:** Delete `INITIAL_LIVES` from `HudLayoutConstants`; keep
`PlayerEntity.InitialLives` as the single source of truth.

## Smell: Leaky Abstraction (exposed internal buffers) — minor

**Location:** `MonoGameLearning.Core/Combat/HitboxService.cs:42,75,96-105`
**Problem:** `ResolveHits` returns the pooled `List<DamageInfo> _resultBuffer`
directly and `GetActiveHitboxBounds` returns the pooled `_boundsBuffer`; both are
cleared and reused on the next call.
**Impact:** Callers that retain the reference across frames observe stale/mutated
data. Currently safe only because `GameLoop` fully consumes within the same frame.
**Suggestion:** Return `IReadOnlyList<T>` and document single-frame validity, or
return a defensive copy/slice buffer; keep the pooled backing list private.

---

## Recommended implementation order

1. Dead-data removal (`EntityService._damageables`, `HudLayoutConstants.INITIAL_LIVES`, `PropBase` `IDamageResponse` adapters) — zero behavior change.
2. `AudioService.OnGameStateChanged` removal → drive music from `GameLoop.OnTransitioned`.
3. `SpriteSheetAnimationExtensions` promotion to Core.
4. `CreateCollisionWorld` (+ collision/pickup resolution) promotion to Core.
5. `MenuService`/`GumUiService.CreateScreen` de-coupling (move screen build into `MenuService`).
6. `CombatActorBase` entry-logic dedup; `ActorStateMachineCallbacks` split.

Every step must end with `dotnet build --warnaserror` (0 warnings) and
`dotnet test` (all green), and must preserve the invariant that Core never
references `MonoGameLearning.Game`.
