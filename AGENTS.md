# MonoGame Learning Project

## Project Overview

This is a **C# MonoGame** project designed for learning game development concepts, with the primary goal of building a side-scrolling beat 'em up game in the vein of 90s classics like *Streets of Rage*, *Final Fight*, and *Double Dragon*. As part of this effort, we are developing a generic, reusable core engine library (`MonoGameLearning.Core`) that will be useful for bootstrapping future game projects.

The project targets **.NET 10.0** and utilizes **MonoGame.Framework.DesktopGL** for cross-platform desktop support, relying heavily on **MonoGame.Extended** for utilities like cameras, sprites, and input handling.

The solution is structured into two main projects:

1. **`MonoGameLearning.Core`**: A library containing reusable engine-level components, base classes, and utilities.
2. **`MonoGameLearning.Game`**: The main executable project containing specific game logic, assets, and the game loop.

## Architecture

* **Core Library (`MonoGameLearning.Core`)**
  * **`GameCore`**: A base class (inheriting from `Microsoft.Xna.Framework.Game`) that handles boilerplate setup: `GraphicsDeviceManager`, `SpriteBatch`, `ContentManager`, and an `OrthographicCamera` with a `BoxingViewportAdapter` for resolution independence.
  * **`Input`**: Contains `InputManager` to abstract raw input into game actions (e.g., `Action1Pressed`).
  * **`Entities`**: Defines base entity classes, interfaces, helpers, and managers (see [Entity Architecture](#entity-architecture) below).
  * **`Combat`**: Contains `CombatService`, `HitboxService`, `DamageInfo`, `ICombatant`, `IHitboxProvider`, and related combat data types.
  * **`Drawing`**: Basic shape drawing utilities.
  * **`Entities`**: Also contains `RenderContext` and `DebugDrawContext` — lightweight data classes consumed by `IRenderable.Render()` and `IDebugDrawable.DrawDebug()` respectively.

* **Game Project (`MonoGameLearning.Game`)**
  * **`GameLoop`**: Inherits from `GameCore`. Orchestrates the game loop at a high level, delegating to managers, controllers, and services (see [Game Loop Delegation Pattern](#game-loop-delegation-pattern) below). Contains `GameStateController` and related game-loop infrastructure.
  * **`Menus`**: Menu screen construction and navigation — `MenuManager`.
  * **`Camera`**: Camera tracking — `CameraController`.
  * **`Program.cs`**: The entry point, using C# top-level statements to bootstrap `GameLoop`.
  * **`Entities`**: Game-specific entities — `PlayerEntity`, `EnemyEntity`, `OilDrumEntity` — and their state controllers.
  * **`Sprites`**: Sprite management and animation logic (e.g., `PlayerSprite`).
  * **`Content`**: Contains game assets (images, fonts, etc.) processed by the MonoGame Content Pipeline (`.mgcb`).

## Architecture Patterns

### Entity Architecture

The entity system follows a layered composition pattern:

1. **Base class (`Entity`)**: Provides shared state — `Position`, `Rotation`, `Width`, `Height`, `Name`, and a computed `Frame` (`RectangleF`).

2. **Interfaces for composability** (in `Entities/Interfaces/`, except `ICombatant`/`IHitboxProvider` in `Combat/`):
   * `IUpdatable` — `Update(GameTime)`
   * `IRenderable` — `Render(RenderContext)`
   * `IDebugDrawable` — `DrawDebug(DebugDrawContext)`
   * `IDamageable` — `TakeDamage(DamageInfo)`
   * `IHasHealth` — `Health`, `MaxHealth`
   * `IMoveableEntity` — `MovementDirection`, `Speed`, `MovementBounds`
   * `IAnimated` — `Sprite`, `ResetAnimationFrameIndex()`
   * `ICollisionActor` — `Bounds`, `OnCollision(CollisionEventArgs)`
   * `ICombatant` — `CanTakeDamage()`, `ReduceHealth(int)`, `IsAlive`, `Faction`, `OnDeath()`, `OnKnockdown(DamageInfo)`, `OnHit(DamageInfo)`
   * `IHitboxProvider` — `HitboxService`, `CurrentMove`

3. **Composite classes** that combine multiple interfaces:
   * `CombatActorBase : Entity, IUpdatable, IRenderable, IDebugDrawable, ICollisionActor, ICombatant, IHitboxProvider, IMoveableEntity, IAnimated` — the primary combatant base.
   * `PropBase : Entity, IRenderable, IDebugDrawable, ICollisionActor, IDamageable, IHasHealth` — destructible world objects. `TakeDamage()` is concrete `virtual` (subclasses override to customize input, then delegate to `base.TakeDamage()` for the common pipeline).
   * `BackgroundEntity : Entity, IRenderable, IDebugDrawable` — static background layers.
   * `TriggerEntity : Entity, ICollisionActor` — invisible collision zones.

4. **Helper classes** (in `Entities/Helpers/`) encapsulate common functionality reused by composites:
   * `Health` (instance) — health tracking (`Value`, `MaxHealth`, `Subtract`, `SetToMax`, `IsAlive`).
   * `SpriteRenderer` (instance) — wraps `AnimatedSprite` + `Scale` with a `Render` method.
   * `Mover` (static) — `ClampToBounds()`, `UpdateFacingDirection()`, `PreventDiagonal()`.
   * `AnimationFrameTracker` (instance) — tracks frame index changes for hitbox registration.
   * `HealthDisplay` (static) — formats and draws health text (`{value}/{max}`) centered above entity frames in debug overlays.

5. **Game-level entities** extend the composites:
   * `PlayerEntity : CombatActorBase` — uses `PlayerStateController` (Stateless).
   * `EnemyEntity : CombatActorBase` — uses `EnemyStateController` (Stateless).
   * `OilDrumEntity : PropBase, IUpdatable` — uses `OilDrumStateController` (Stateless).

### Game Loop Delegation Pattern

The `GameLoop` orchestrates at a high level without knowing the internals of each game system. It delegates to:

1. **Managers** — lifecycle, bulk operations, and cross-cutting concerns:
   * `EntityManager` — register/destroy entities, maintain typed lists (`Updatables`, `Renderables`, `Movables`, `Collidables`, `Damageables`, `HitboxProviders`, `DebugDrawables`, `Combatants`), per-entity add/remove from `CollisionComponent`. Exposes `Clear()` and `SetCollisionComponent()` for game reset.
   * `MenuManager` (in `Menus/`) — menu screen construction, navigation, visibility toggling.
   * `InputManager` (in `Core`) — raw keyboard state → `MovementDirection` vector + `ActionTriggered` events. Has two modes (`Gameplay` vs `Menu`): in `Gameplay` mode, WASD/arrows accumulate `MovementDirection`; in `Menu` mode, W/S/arrows fire `MenuUp`/`MenuDown` events. All `_keyActions` fire in both modes.

2. **Controllers** — state machines and per-frame logic:
   * `GameStateController` (in `GameLoop/`) — Stateless finite state machine for `TitleScreen` → `Playing` → `Paused`/`GameOver`/`LevelComplete` transitions, with return transitions (`Paused` → `Playing`, `Paused`/`GameOver`/`LevelComplete` → `TitleScreen`). `Fire(trigger)` guards with `CanFire()` before dispatching.
   * `CameraController` (in `Camera/`) — clamps camera to player position within level bounds.
   * `PlayerStateController` / `EnemyStateController` / `OilDrumStateController` — entity-specific Stateless state machines.

3. **Services** — cross-cutting domain logic:
   * `HitboxService` — registers active hitboxes per animation frame, resolves against all entities, deduplicates per-owner per-definition per-frame (keyed as `(owner, definition, target)` so the same hitbox can hit different targets but not the same target twice per frame). Exposes `GetActiveHitboxBounds(Entity)` for debug rendering. Contains inline `HitboxData` and `HitResult` record types.
   * `CombatService` (static) — applies `DamageInfo`: checks `CanTakeDamage()` → reduces health → dispatches death/knockdown/hit callbacks.

4. **Result**: The `GameLoop.Update()` body is only ~30 lines and reads as a high-level script:
   * Update input (switched by `GameState`)
   * Process entity lifecycle (pending destroys)
   * If `Playing`: update camera, collect movement input, set movement bounds, update all entities, resolve hitboxes, apply damage (actors via `CombatService`, props via `PropBase.TakeDamage()`), update collision, clamp movables

* **Language**: C# (NET 10.0)
* **Framework**: MonoGame (DesktopGL 3.8.*)
* **Extensions**: MonoGame.Extended (5.3.1), Stateless (5.20.0)

## Building and Running

### Prerequisites

* .NET 10.0 SDK
* MonoGame Content Builder (if modifying assets)

### Commands

**Run the Game:**

```bash
dotnet run --project MonoGameLearning.Game/MonoGameLearning.Game.csproj
```

**Build the Solution:**

```bash
dotnet build
```

**Run Tests:**

```bash
dotnet test
```

## Development Conventions

* **Separation of Concerns**: Keep generic, reusable logic in `Core` and specific game implementation in `Game`.
* **State Management**: The project uses the `Stateless` library for managing entity states.
* **Input**: Input is decoupled from logic via `InputManager` events.
* **Resolution**: The game uses a virtual resolution (`GAME_WIDTH`, `GAME_HEIGHT`) scaled to the window size using `BoxingViewportAdapter`.
* **Conciseness**: Responses and suggestions should include code that is as concise and terse as possible.
* **Modern C#**: Always use the latest C# features (e.g., primary constructors, collection expressions, raw string literals) to ensure the codebase remains modern and idiomatic.
* **Solution Simplification**: Before proposing a solution, ALWAYS include a consideration step to see if the proposed architecture or implementation can be further simplified, refactored, or streamlined.
* **Build Verification**: Always run `dotnet build` to ensure the project compiles successfully after any code modifications.
* **Testing**: Always run `dotnet test` to execute unit tests after making any changes to verify no regressions were introduced.
* **Mandatory Pre-Completion Checklist**: Before marking any implementation task as complete, the following steps MUST be performed in order:
  1. Write unit/integration tests covering all new or modified logic.
  2. Run `dotnet build` to verify compilation.
  3. Run `dotnet test` to verify all tests pass with no regressions.
  4. If any step fails, fix the issue before proceeding.
* **Preventing Game-Breaking Bugs (Test Requirement)**: Always write new unit/integration tests when modifying logic — this is **not optional**. Focus tests on critical gameplay failure modes such as:
  * **Out-of-Bounds**: Characters or entities slipping outside of screen, level, or walkable boundaries.
  * **Connectivity & Seams**: Disconnected backgrounds or levels that trap players or break scrolling.
  * **State Machine Deadlocks**: Entities getting stuck in non-interruptible states (e.g., infinite attacking or falling) without recovery.
  * **Collision Failures**: Entities passing through solid boundaries or failing to register collision responses.
* **Camera Tracking**: The camera tracking system losing track of player coordinates or clamping to incorrect screen areas.
* **Diagnostic Debug Warnings**: When adding logic with edge cases, invariants, or states that should never be reached, add `Debug.WriteLine` and/or `Debug.Assert` calls to surface unexpected conditions during local debugging. These are compiled out in Release builds (zero runtime cost) but catch sentinel drift, skipped frames, null invariants, and other game state corruptions during development. Use this pattern:
* **Keep AGENTS.md in Sync**: Whenever architectural changes are made (new classes, renamed files, changed patterns, etc.), update AGENTS.md to reflect them. This file is the single source of truth for project architecture.

  ```csharp
  Debug.Assert(condition, "Description of what went wrong");
  if (unexpectedCondition)
      Debug.WriteLine($"[{name}] Descriptive warning — root cause hint");
  ```

## MonoGame.Extended Pitfalls

### `AnimatedSprite.Controller` is replaced by `SetAnimation()`

`MonoGame.Extended.AnimatedSprite.Controller` has a public setter. Calling `SetAnimation()` may replace the `Controller` property with a **new** `IAnimationController` instance. This means:

* **Event subscriptions must happen AFTER `SetAnimation()`**, not once at construction time. Subscribing to `Sprite.Controller.OnAnimationEvent` in the constructor subscribes to the *initial* controller, which becomes orphaned after the first `SetAnimation()` call. Events from the new controller (including `AnimationCompleted`) will never fire.
* **Always subscribe/unsubscribe in pairs** around `SetAnimation()` calls for non-looping animations that need completion detection. See `PlayerEntity.SubscribeToAnimationEvent()` / `UnsubscribeFromAnimationEvent()` for the pattern.
* The affected entry/exit callbacks are: `OnAttackingEntry/Exit`, `OnHurtEntry/Exit`, `OnDyingEntry/Exit` — any state that plays a non-looping animation requiring a completion trigger.
