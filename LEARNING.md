# LEARNING.md — Design Patterns & Review Vocabulary

A living study guide for two things: the **design patterns** this codebase builds
with, and the **architecture-analysis vocabulary** used in review sessions. Each
entry has four parts: **Concept** (plain-language), **Why it's used here**,
**Where** (concrete `file:line` pointers), and **How to spot it** (a one-line
recognition cue). Entries are labeled `[Pattern]` (thing we build with) or
`[Review term]` (thing we critique with).

The audit vocabulary below is documented with concrete findings in
`.kilo/plans/1787965629820-architecture-audit-findings.md`; several of those
findings have since been fixed (noted inline).

---

## Patterns

### 1. Composition root / dependency injection via delegates [Pattern]

- **Concept:** One place constructs the whole object graph; dependencies are passed in as `Func`/`Action` delegates rather than referenced by type.
- **Why it's used here:** Lets Core stay generic — `LevelDirectorCore` never references Game types, so any beat-'em-up can reuse it.
- **Where:** `GameLoop.InitLevelSystems` (`GameLoop.cs:352-369`) wires the Game-side content factories from `Levels.LevelEntityFactory`; `LevelDirectorCore` ctor takes `createProp`/`createPickup`/`getWeapon`/`createEnemy`/`onEnemySpawned`/`getCameraView` (`LevelDirectorCore.cs:59-86`); `Func<WorldSnapshot>` seam (`EntityPool.cs:11`, `EnemyEntity.cs:75`).
- **How to spot it:** A class whose constructor is a wall of `Func<...>`/`Action<...>` parameters.

### 2. Capability interfaces (interface segregation, adjective-named) [Pattern]

- **Concept:** Small interfaces named for what an object *can do*, not what it *is*.
- **Why it's used here:** `EntityService` probes entities by capability and fans them into typed lists, so behavior is decoupled from entity kind.
- **Where:** `IUpdatable`, `IRenderable`, `IDebugDrawable`, `IMoveable : ISpatial`, `IDamageable`, `IPickup`, `IPickupDropper`, `IHitboxProvider`, `IWeaponWielder`, `ICollisionLayer`, `IScreenRenderable`; registration probe in `EntityService.AddToTypedLists` (`EntityService.cs:114-127`).
- **How to spot it:** An interface named with an adjective/participle (`IUpdatable`) or a noun you obtain (`IHitboxProvider`), never `FooEntity`.

### 3. State pattern via Stateless FSM [Pattern]

- **Concept:** States and legal transitions are declared declaratively; illegal triggers are ignored with a debug trace.
- **Why it's used here:** Player/enemy/game-shell behavior is a table of states × triggers, and animation completion feeds back into the FSM.
- **Where:** `StateMachineController<TState,TTrigger>` wraps the raw Stateless machine privately — guarded `Fire` + `Debug.WriteLine` (`StateMachines/StateMachineController.cs:24-31`), `SubscribeTransitions` replaces direct `StateMachine.OnTransitioned` reach-through (`StateMachineController.cs:35`); `GameStateMachine`, `PlayerStateMachine`, `EnemyStateMachine`; the five combat states (Attacking/Hurt/KnockedDown/Dying/Dead) are configured once by `StateMachines.CombatStateMachineConfigurator` parameterized over each actor's enums (`CombatStateSet<TState>`/`CombatTriggerSet<TTrigger>`), so player/enemy tables can't drift; `ActorPhase` + `FirePhaseCompleted` bridge animation completion back into the FSM (`CombatActorBase.cs:195-196`). Each machine declares its callback surface explicitly (`CombatActorStateMachineCallbacks` base in `Core.StateMachines` + `PlayerStateMachineCallbacks`/`EnemyStateMachineCallbacks` subtypes).
- **How to spot it:** `.Configure(state).Permit(trigger, nextState)` chains, or `Fire` wrapped in a `CanFire` guard.

### 4. Object pooling (rent/return/sentinel) [Pattern]

- **Concept:** Reuse instances instead of allocating; a sentinel position marks "not in the world."
- **Why it's used here:** Enemies spawn/die constantly; pooling avoids GC churn and keeps memory flat across retries. The sentinel is the allocation-free alternative to a nullable "in-pool" flag — checking `Position == Sentinel` reuses the existing position field instead of adding per-entity bookkeeping.
- **Where:** `EntityPool<TEnemy>` (`EntityPool.cs`): per-type stacks, sentinel `(-99999,-99999)` (`EntityPool.cs:15`), `Build` pre-allocation, `OnRentEnemy`/`OnReturnEnemy` hooks. Pool-return cleanup goes through the entity itself — `EnemyPool.OnReturnEnemy` calls `enemy.ClearCombatState()` (hitboxes/attack-dedup) so the pool never reaches into `HitboxService`. Audio reuses `SoundEffectInstance` pools (`AudioService.cs:12` `PoolSize = 3`).
- **How to spot it:** A `Stack<T>` of instances plus a `Rent`/`Return` pair, and a magic sentinel position.

### 5. Snapshot pattern (consistent per-frame world view) [Pattern]

- **Concept:** AI reads one immutable snapshot of the world per frame instead of live-mutating state.
- **Why it's used here:** Prevents enemies from seeing half-updated positions mid-frame and gives a stable input to steering math.
- **Where:** `WorldSnapshot`/`ActorSnapshot` readonly record structs (`AI/`); `EnemyAI.Update(in WorldSnapshot)`; `LevelDirectorCore.PopulateSnapshots` fills pre-allocated `_enemyBuf`/`_propBuf` (`LevelDirectorCore.cs:123-145`).
- **How to spot it:** A `readonly record struct` of positions passed `in` to a per-frame update.

### 6. Steering behaviors [Pattern]

- **Concept:** Move toward a target while blending separation, avoidance, and bounds forces.
- **Why it's used here:** Enemy movement feels alive without a pathfinding grid; the dominant force is surfaced for debug drawing.
- **Where:** `EnemyAI` seek/separation/avoidance/bounds forces, weighted + normalized + capped (`EnemyAI.cs:111-168`); `DominantForce` enum (`DominantForce.cs`).
- **How to spot it:** A method that accumulates `Vector2` forces with weight constants and picks a "dominant" one.

### 7. Strategy via delegate seam [Pattern]

- **Concept:** Swap behavior by passing a function, not by subclassing.
- **Why it's used here:** The injected `Func`/`Action` factories (pattern 1) substitute for a formal `IEnemyFactory` hierarchy.
- **Where:** `LevelDirectorCore` ctor delegates (`LevelDirectorCore.cs:59-86`); `Func<WorldSnapshot>` (`EnemyEntity.cs:75`).
- **How to spot it:** A behavioral choice expressed as a constructor parameter of type `Func<...>`/`Action<...>`.

### 8. Observer pattern (events) [Pattern]

- **Concept:** Producers raise events; consumers subscribe. Subscriptions must be symmetric with their producers' lifetime.
- **Why it's used here:** `Died`/`Destroyed`/`ActionTriggered`/`LevelCompleted` decouple entities from the systems that react; the subscribe/unsubscribe symmetry around `SetAnimation` is mandatory (documented MonoGame.Extended pitfall).
- **Where:** `CombatActorBase.PlayAnimation` pairs `UnsubscribeFromAnimationEvent`/`SubscribeToAnimationEvent` (`CombatActorBase.cs:80-95`); `LevelDirectorCore` subscribes/unsubscribes `Destroyed`/`Died` (`LevelDirectorCore.cs:95,102,218,304`).
- **How to spot it:** `event EventHandler` members plus `+=`/`-=` in matching call sites.

### 9. Factory pattern [Pattern]

- **Concept:** Static or injected methods that construct configured instances.
- **Why it's used here:** Sprites need one-time content loading + atlas wiring; the composition root builds content entities.
- **Where:** Static sprite factories `PlayerSprite.Create()` etc., each backed by the shared `Core.Rendering.SpriteSheetAsset` atlas-load/frame-def/Create pipeline (`AnimatedSprites/*`, `SpriteSheetAsset.cs`); `BackgroundRenderer.Create` (`BackgroundRenderer.cs:33`); Game-side content factories in `Levels.LevelEntityFactory` (`LevelEntityFactory.cs`); `CollisionWorldFactory.Create` builds the canonical collision world (`CollisionWorldFactory.cs:11`).
- **How to spot it:** A `static` method returning a fully-configured object, or a `Create*` method taking a content/def parameter.

### 10. Template Method / hook methods [Pattern]

- **Concept:** The base class defines the skeleton; subclasses fill in the variable parts.
- **Why it's used here:** `CombatActorBase` owns the shared state-transition steps; subclasses call the base steps and add only audio, so knockdown/dying logic has a single source of truth.
- **Where:** Abstract `Update`/`Phase`/`FirePhaseCompleted` (`CombatActorBase.cs:117,195-196`); shared `protected *Impl` steps (`AttackingExitImpl`…`DeadEntryImpl`, `CombatActorBase.cs:207-241`); `ResetActor`/`TryHandleIncapacitatedUpdate` (`CombatActorBase.cs:244-263`); subclasses call base steps then play SFX (`PlayerEntity.cs`, `EnemyEntity.cs`).
- **How to spot it:** A base class with `protected` step methods that subclasses invoke inside their overrides.

### 11. Generic base class with type constraint [Pattern]

- **Concept:** A reusable base parameterized by the concrete entity type it manages.
- **Why it's used here:** Pooling and wave logic are identical for any enemy; the constraint guarantees the capabilities the base needs.
- **Where:** `LevelDirectorCore<TEnemy> where TEnemy : CombatActorBase, IPickupDropper` (`LevelDirectorCore.cs:20-21`); `EntityPool<TEnemy> where TEnemy : Entity` (`EntityPool.cs:9-13`).
- **How to spot it:** `class Foo<T> where T : SomeBase, ISomeCapability`.

### 12. Capability registry / typed parallel lists (lightweight ECS) [Pattern]

- **Concept:** One entity list plus parallel typed lists; entities are fanned into lists by capability probe.
- **Why it's used here:** Update/render/collision/debug passes iterate only the entities that participate, without per-frame type checks.
- **Where:** `EntityService` holds `_updatables`, `_renderables`, `_collidablesByLayer`, `_hitboxProviders`, `_movables`, `_debugDrawables`, `_props` (`EntityService.cs:34-40`); `SortRenderablesByY` via `RenderableYComparer` (`EntityService.cs:48`).
- **How to spot it:** A service with many `List<ICapability>` fields and a `TryAdd<T>` probe on register.

### 13. `ref readonly` + `in` for zero-copy large structs [Pattern]

- **Concept:** Return/consume structs by reference to avoid copying.
- **Why it's used here:** `WorldSnapshot` is rebuilt every frame and read by every enemy; copying it per enemy would churn the GC.
- **Where:** `LevelDirectorCore.CurrentWorld => ref _currentSnapshot` (`LevelDirectorCore.cs:57`); `EnemyAI.Update(in WorldSnapshot)`.
- **How to spot it:** `ref readonly` return types and `in` parameters on per-frame methods.

### 14. Zero-allocation buffers / pooled result lists [Pattern]

- **Concept:** Reuse buffers and cached delegates so hot paths never allocate.
- **Why it's used here:** `Update`/`Draw`/collision run every frame; allocations there cause GC-stutter (project rule, see AGENTS.md).
- **Where:** `HitboxService._resultBuffer`/`_boundsBuffer` cleared and reused (`HitboxService.cs:15-16,42,96`); small values as `readonly record struct` (`DamageInfo`, `HitboxData`, `ActiveHitbox`, `AIUpdateResult`); `GameLoop` caches `_playSfx` so `PickupService.ResolveOverlaps` gets a pre-built delegate (`GameLoop.cs:60,84`).
- **How to spot it:** A `List<T>` field that is `Clear()`ed inside a method that returns it, or a delegate stored in a field instead of passed as a fresh method group.

### 15. Defensive programming (Debug.Assert / Debug.WriteLine) [Pattern]

- **Concept:** Assert invariants and log unexpected conditions; compiled out in Release.
- **Why it's used here:** Catches sentinel drift, illegal wave layouts, and camera clamp violations during development at zero runtime cost.
- **Where:** `LevelData.Validate` (`Levels/LevelData.cs`), `CameraService.ComputeTargetX` (`CameraService.cs:33-35`), `AnimationFrameTracker.TryGetNewFrame`, `StateMachineController.Fire` (`StateMachineController.cs:31`).
- **How to spot it:** `Debug.Assert(condition, "message")` guarding an invariant, or `Debug.WriteLine` in a "should never happen" branch.

### 16. Modern C# [Pattern]

- **Concept:** Latest language features for terse, safe code.
- **Why it's used here:** Cuts boilerplate and makes intent explicit (project rule, see AGENTS.md).
- **Where:** Primary constructors (nearly every `Entity`/service); `required`/`init` properties (`MoveData`, `MeleeWeaponDef`); collection expressions (`[]`); record structs (`WaveDef`, `ResolutionSetting`); default interface methods (`IDamageResponse.cs:6,9-10`); explicit interface implementation (`PropBase.cs:67-71`); nullable reference types; `InternalsVisibleTo` (`Core.csproj:7`).
- **How to spot it:** `class Foo(...)` primary-constructor syntax, `= []`, `readonly record struct`, `=>`-bodied interface members.

### 17. Virtual resolution / world-vs-screen rendering [Pattern]

- **Concept:** Game logic runs in a fixed virtual resolution; rendering scales to the window.
- **Why it's used here:** The game is resolution-independent; the camera transforms world space and the viewport adapter scales UI.
- **Where:** `GameCore` builds `BoxingViewportAdapter` + `OrthographicCamera` (`GameCore.cs:45-53`); `GameLoop` draws world space with `Camera.GetViewMatrix()` (`GameLoop.cs:228`) then UI with `ViewportAdapter.GetScaleMatrix()` (`GameLoop.cs:270`).
- **How to spot it:** Two `SpriteBatch.Begin` calls per frame with different transform matrices.

---

## Review terminology

### 1. God Class [Review term]

- **Concept:** One class holding many responsibilities across many namespaces.
- **Detection:** "many responsibilities / references many namespaces."
- **Example:** `GameLoop.cs` (~373 lines: wiring + frame orchestration + rendering). Slimmed repeatedly by audits — collision/pickup/music rules → `GameLoopRules`, entity factories → `LevelEntityFactory`, level data → `LevelData` — but it remains the composition root + orchestrator.

### 2. Feature Envy [Review term]

- **Concept:** A method uses more of another class's members than its own.
- **Detection:** "a method uses more of another class's members than its own."

### 3. Law of Demeter / reach-through [Review term]

- **Concept:** Code reaches through one object into another's internals, coupling to their layout.
- **Detection:** "`.Foo.Bar.Baz`-style chains" or indexing into another object's children.
- **Example:** `MenuService` used to index-cast into `ContainerRuntime.Children[2..4]` to recover menu items — **fixed** by the audit: `MenuService.BuildMenuScreen` now stores the option `TextRuntime` list directly (`MenuService.cs`).

### 4. Leaky abstraction [Review term]

- **Concept:** The caller must understand the wrapped type's internals to use the facade.
- **Detection:** "caller must understand the wrapped type's internals."
- **Examples:** `GumUiService.CreateScreen`'s child-ordering contract — **fixed** (method deleted; `MenuService` builds its own screens). `HitboxService.ResolveHits`/`GetActiveHitboxBounds` returning their cleared-and-reused mutable buffers — **still open** (callers must consume within the frame).

### 5. Circular dependency [Review term]

- **Concept:** A references B and B references A (project or namespace).
- **Detection:** "A references B and B references A." This repo: verified none (`Game → Core`, `Tests → both`); Core has zero references to Game.

### 6. Inheritance smells [Review term]

- **Concept:** Deep hierarchies overriding one method, base members existing for one subclass, empty abstract methods, `new` hiding.
- **Detection:** "base members existing for one subclass" / "subclass re-implements base logic."
- **Example:** `CombatActorBase` entry `*Impl` methods were re-inlined by `PlayerEntity`/`EnemyEntity` — **fixed** by the audit: the base steps are now the single source of truth and subclasses add only audio (`CombatActorBase.cs:207-241`).

### 7. Misplaced Core Class [Review term]

- **Concept:** Reusable engine logic trapped in the game project (or game-specific logic leaking into Core).
- **Detection:** "would I copy this file verbatim into a new 2D sidescroller?" / "does Core reference Game?"
- **Examples fixed by the audit:** `AudioService.OnGameStateChanged` state→music mapping → `GameLoopRules.ApplyMusicForState` (`GameLoopRules.cs`); `GameLoop.CreateCollisionWorld` → `CollisionWorldFactory` (`CollisionWorldFactory.cs`); `SpriteSheetAnimationExtensions` → `Core.Rendering`; `CombatActorStateMachineCallbacks` → `Core.StateMachines`.

### 8. Coupling vs Cohesion [Review term]

- **Concept:** The axis an architecture audit grades on — how many things a class depends on vs how focused it is.
- **Detection:** Count distinct namespaces/types a class references and the number of distinct responsibilities it has.

### 9. Composition over inheritance [Review term]

- **Concept:** Delegates/factories instead of subclass plumbing.
- **Detection:** "a behavioral seam implemented as a constructor `Func`/`Action` rather than a subclass."

### 10. Duplication / single source of truth [Review term]

- **Concept:** The same value or logic existing in two places that can drift.
- **Detection:** "two constants/implementations for one concept; changing one does not change the other."
- **Example:** `HudLayoutConstants.INITIAL_LIVES` duplicated `PlayerEntity.InitialLives` — **fixed** (constant deleted; `GameLoop.INITIAL_LIVES` is now the single source, with lives moved off the player entirely).

### 11. Dead code [Review term]

- **Concept:** State or members written and maintained but never read.
- **Detection:** "written, never read" (grep for reads after the only writes).
- **Example:** `EntityService._damageables` list was populated/removed/cleared but never read — **fixed** (deleted). `PropBase`'s `IDamageResponse` adapters were unreachable — **fixed** (interface dropped; props damage only via `IDamageable.TakeDamage`).

### 12. Misplaced data (wrong-owner state) [Review term]

- **Concept:** A value is owned by one class but consumed only by another — the "owner" is just a bucket, not the decisionmaker.

- **Detection:** "data owned by A, read/written only by B; A never uses its own value."
- **Example:** `PlayerEntity.Lives`/`InitialLives` were carried by the player entity but only read/decremented by `GameLoop.OnPlayerDied` — **fixed**: lives moved to `GameLoop` (`_lives`, `INITIAL_LIVES`, consumed via `GameLoopRules.TryConsumeLife`), and the HUD receives them via a `Func<int>` getter (`HudService(..., Func<int>)`). Clinical sign: the player never used its own lives — the value was run/session state, not actor behavior.

---

## Common architecture smells & minimal resolutions

The actionable catalog used when implementing or reviewing. The rule: resolve each
smell with the minimal fix that moves responsibility to the right owner — **do not
invent new abstractions for a single value**. Each smell's full vocabulary entry is
linked in "Review terminology".

1. **Misplaced data (wrong-owner state)** — A value is owned by one class but consumed only by another. *Resolve:* move the value to the consumer and pass it where needed — `PlayerEntity.Lives` became `GameLoop._lives`/`INITIAL_LIVES` with the HUD fed via a `Func<int>` getter.
2. **Feature Envy** — A method uses more of another class's members than its own. *Resolve:* move the method (or the data it envies) to the class it depends on.
3. **Dead code / duplicate source of truth** — A member is written but never read, or a constant duplicates another. *Resolve:* delete the dead member or collapse to one source of truth.
4. **Law of Demeter / reach-through** — Chains like `.Foo.Bar.Baz` or indexing into another object's children. *Resolve:* have the owner expose what callers need directly — `MenuService` stores its option lists instead of casting `Children[i]`; enemy spawn/cleanup went through `EnemyEntity.PrepareSpawn`/`ClearCombatState` instead of callers reaching into `SpriteRenderer.*`/`HitboxService`.
5. **Leaky abstraction** — Callers must understand a wrapped type's internals to use it. *Resolve:* tighten the contract or move the logic to the caller.
6. **Misplaced Core Class** — Reusable engine logic trapped in the Game project. *Resolve:* promote it to `MonoGameLearning.Core` — `CollisionWorldFactory`, `PickupService`, `SpriteSheetAnimationExtensions`.
7. **Inheritance smell** — Base members existing only for one subclass, or subclasses re-implementing base logic. *Resolve:* promote the shared step to the base as the single source of truth — `CombatActorBase` `*Impl` steps; or delete the speculative abstraction — `Level` had a single subclass and collapsed to the concrete `LevelData` record.

When a session introduces a new smell or resolution, add it to this catalog in the
same change (AGENTS.md rule).

---

## How this file stays current

When a session introduces a new pattern, adds a review term, or restructures the
code in a way that invalidates an existing entry, update `LEARNING.md` in the
same change so it stays a current, accurate guide (AGENTS.md rule). Prefer stable
anchors (type/method names) over brittle line numbers.
