# MonoGame Learning Project

## Project Overview

This is a **C# MonoGame** project designed for learning game development concepts, with the primary goal of building a side-scrolling beat 'em up game in the vein of 90s classics like *Streets of Rage*, *Final Fight*, and *Double Dragon*. As part of this effort, we are developing a generic, reusable core engine library (`MonoGameLearning.Core`) that will be useful for bootstrapping future game projects.

The project targets **.NET 10.0** and utilizes **MonoGame.Framework.DesktopGL** for cross-platform desktop support, relying heavily on **MonoGame.Extended** for utilities like cameras, sprites, and input handling.

The solution is structured into two main projects:

1. **`MonoGameLearning.Core`**: A library containing reusable engine-level components, base classes, and utilities.
2. **`MonoGameLearning.Game`**: The main executable project containing specific game logic, assets, and the game loop.

## Architecture

* **Core Library (`MonoGameLearning.Core`)**
  * **`GameCore`**: A base class (inheriting from `Microsoft.Xna.Framework.Game`) that handles boilerplate setup as **instance members** (no statics): `GraphicsDevice`/`Content` are inherited from `Game`; `GraphicsDeviceManager`, `SpriteBatch`, `OrthographicCamera`, and `BoxingViewportAdapter` are instance properties assigned in `Initialize` (after `base.Initialize()` creates the `GraphicsDevice` — MonoGame resources only exist inside the lifecycle, never at construction).
  * **`Input`**: Contains `InputManager` to abstract raw input into game actions (e.g., `Action1Pressed`).
  * **`Entities`**: Defines base entity classes.
  * **`Drawing`**: Basic shape drawing utilities.
  * **`GameStateMachine`**: Core-level arcade-shell FSM factory (`GameState`/`GameTrigger` via Stateless + `StateMachineController`), owned by `GameLoop`.
  * **`Levels.EntityPool<TEnemy>`**: Generic rent/return/sentinel pool (per-type stacks). Game behavior (entity reset, hitbox cleanup) comes from subclasses overriding `OnRentEnemy`/`OnReturnEnemy`; sprite warmup happens in the Game-side createEnemy factory.
  * **`Levels.LevelDirectorCore<TEnemy>`** (where `TEnemy : CombatActorBase, IPickupDropper`): Encounter core — wave gating, scroll-lock, snapshots, prop/pickup/drop spawning, debug draw. Game content (drums, pickups, weapons, enemy visuals, spawn-walk) is injected via `createProp`/`createPickup`/`getWeapon`/`createEnemy`/`onEnemySpawned` delegates; enemy spawning sizes off the enemy's own dimensions.

* **Game Project (`MonoGameLearning.Game`)**
  * **`GameLoop`**: Inherits from `GameCore`. Implements the specific game logic (`Update`, `Draw`), manages entities (like the player), and handles the main application lifecycle. Composition root for injected factories.
  * **`Program.cs`**: The entry point, using C# top-level statements to bootstrap `GameLoop`.
  * **`Entities`**: Game-specific entities, such as `PlayerEntity`.
  * **`Levels.LevelDirector` / `Levels.EnemyPool`**: Thin Game-side subclasses of `LevelDirectorCore<EnemyEntity>` / `EntityPool<EnemyEntity>` — they only assemble the Game-specific pools and pass content factories through.
*   **`Weapons`**: Static melee weapon defs (e.g., `BatWeapon`) built from `Core.Combat.MeleeWeaponDef` that swap `Attack1Move`/`AttackMove` while armed. Swing rendering uses a 4-frame `bat-texture.png`/`bat.json` atlas via `AnimatedSprites.BatSprite`, positioned per `attack` frame via `SwingAnchors`/`CarryAnchor`; hitboxes fire at swing apex (frames 2–3) only. The swing is frame-stepped (see the `SetFrame()`/`TextureRegion` pitfall below) so it stays in sync with the player's attack animation. `AnimatedSprites.BatPickupSprite` owns the separate static `bat-pickup.png` texture used by `WeaponPickupEntity` for the dropped-pickup icon (same pattern as `FoodPickupSprite`/`apple-pickup.png`).
  * **`Sprites`**: Sprite management and animation logic (e.g., `PlayerSprite`).
  * **`Content`**: Contains game assets (images, fonts, etc.) processed by the MonoGame Content Pipeline (`.mgcb`).

## Key Technologies

* **Language**: C# (NET 10.0)
* **Framework**: MonoGame (DesktopGL 3.8.*)
* **Extensions**: MonoGame.Extended (6.0.0), Stateless (5.20.0)

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
* **Solution Simplification**: Before proposing a solution, ALWAYS include a consideration step to see if the proposed architecture or implementation can be further simplified, refactored, or streamlined. When creating plans, prioritize simplicity — actively seek out and suggest simplifying constraints that reduce code surface area, remove unnecessary abstractions, or collapse parallel structures. Every plan should explicitly consider what can be removed or constrained, not just what needs to be built.
* **Build Verification**: Always run `dotnet build --warnaserror` to ensure the project compiles successfully and produces zero warnings after any code modifications.
* **Testing**: Always run `dotnet test` to execute unit tests after making any changes to verify no regressions were introduced.
* **Mandatory Pre-Completion Checklist**: Before marking any implementation task as complete, the following steps MUST be performed in order:
  1. Write unit/integration tests covering all new or modified logic.
  2. Run `dotnet build --warnaserror` to verify compilation with zero warnings.
  3. Run `dotnet test` to verify all tests pass with no regressions.
  4. If any step fails, fix the issue before proceeding.
* **Preventing Game-Breaking Bugs (Test Requirement)**: Always write new unit/integration tests when modifying logic — this is **not optional**. Focus tests on critical gameplay failure modes such as:
  * **Out-of-Bounds**: Characters or entities slipping outside of screen, level, or walkable boundaries.
  * **Connectivity & Seams**: Disconnected backgrounds or levels that trap players or break scrolling.
  * **State Machine Deadlocks**: Entities getting stuck in non-interruptible states (e.g., infinite attacking or falling) without recovery.
  * **Collision Failures**: Entities passing through solid boundaries or failing to register collision responses.
* **Camera Tracking**: The camera tracking system losing track of player coordinates or clamping to incorrect screen areas.
* **Diagnostic Debug Warnings**: When adding logic with edge cases, invariants, or states that should never be reached, add `Debug.WriteLine` and/or `Debug.Assert` calls to surface unexpected conditions during local debugging. These are compiled out in Release builds (zero runtime cost) but catch sentinel drift, skipped frames, null invariants, and other game state corruptions during development. Use this pattern:

  ```csharp
  Debug.Assert(condition, "Description of what went wrong");
  if (unexpectedCondition)
      Debug.WriteLine($"[{name}] Descriptive warning — root cause hint");
  ```

* **Debug-Mode Drawing**: When planning or implementing any new system, always consider what should be drawn in debug mode (`IsDebug`). This includes: spatial markers (trigger zones, bounds, spawn points), state indicators (active wave index, enemy count), and any runtime data that aids diagnosis during development. Add debug drawing alongside the feature — not as an afterthought.
* **GC Optimization (Zero-Allocation Gameplay)**: All gameplay-critical paths (`Update`, `Draw`, collision detection, input handling) must be allocation-free to avoid GC-induced frame stutters. Follow these rules:
  * **Pool/reuse allocations** — Use object pools (`ArrayPool<T>`, `Queue<T>`, or custom pools) for transient entities, particles, projectiles, and temporary lists.
  * **Avoid LINQ in hot paths** — LINQ allocates enumerators and closures. Prefer `for`/`foreach` loops with pre-allocated buffers.
  * **Avoid `params` in hot paths** — `params` arrays allocate on every call. Use explicit overloads or `ReadOnlySpan<T>`.
  * **Use `struct` where appropriate** — Prefer `readonly struct` for small, frequently-created data types (e.g., vectors, hitbox results, damage info) to eliminate heap pressure and reduce GC scans.
  * **Pre-allocate buffers** — Use `ArrayPool<byte>` or pre-sized `List<T>` with `Capacity` for serialization, network I/O, and temporary geometry.
  * **Cache delegates and lambdas** — Store static/instance method references in fields; never allocate new lambdas per frame (e.g., don't write `list.ForEach(x => ...)` in Update).
  * **Profile allocations** — Run with `DOTNET_gcServer=1` and monitor GC pause times during development. Flag any unexpected per-frame allocations in code review.
* **Ask Questions When Coding**: Before implementing any design or architecture change, pause to ask the user clarifying questions. Do not silently implement ambiguous or multi-interpretation requests. If a requirement, edge case, or design decision is underspecified, present concrete options and ask for direction. This applies to test strategy, abstraction boundaries, naming, file placement, and any choice that would be costly to reverse.
* **No Nested Classes**: Do not create nested classes (private or otherwise). Every type (class, struct, record, enum) must be declared in its own file at namespace level. This keeps the type graph explicit, testable, and navigable.
* **Interface Naming — No `Entity` Suffix**: Name interfaces for the capability, not the host type. Capability interfaces use adjective/participial names (`IUpdatable`, `IRenderable`, `IDamageable`, `ISpatial`, `IMoveable`) and never carry an `Entity` suffix, because a capability is orthogonal to host type (`BackgroundRenderer` implements `IRenderable` without being an `Entity`). Object-noun interfaces (`IHitboxProvider`, `IPickup`, `ICollisionLayer`) are for things you obtain or traverse, and are also named without a host-type suffix.

## MonoGame.Extended Pitfalls

### `AnimatedSprite.Controller` is replaced by `SetAnimation()`

`MonoGame.Extended.AnimatedSprite.Controller` has a public setter. Calling `SetAnimation()` may replace the `Controller` property with a **new** `IAnimationController` instance. This means:

* **Event subscriptions must happen AFTER `SetAnimation()`**, not once at construction time. Subscribing to `Sprite.Controller.OnAnimationEvent` in the constructor subscribes to the *initial* controller, which becomes orphaned after the first `SetAnimation()` call. Events from the new controller (including `AnimationCompleted`) will never fire.
* **Always subscribe/unsubscribe in pairs** around `SetAnimation()` calls for non-looping animations that need completion detection. See `PlayerEntity.SubscribeToAnimationEvent()` / `UnsubscribeFromAnimationEvent()` for the pattern.
* The affected entry/exit callbacks are: `OnAttackingEntry/Exit`, `OnHurtEntry/Exit`, `OnDyingEntry/Exit` — any state that plays a non-looping animation requiring a completion trigger.

### `AnimationController.SetFrame()` does not update `AnimatedSprite.TextureRegion`

`AnimationController.SetFrame(index)` only changes the controller's internal frame index — it does **not** refresh `AnimatedSprite.TextureRegion`. That refresh only happens inside `AnimatedSprite.Update()` when the frame advances on its own (time-driven playback). Consequence:

* Driving frames manually via `SetFrame()` without calling `Sprite.Update()` leaves the sprite stuck on the first frame's texture region — the frame *index* is correct (so anchor/hitbox math works), but the *drawn texture* never changes. See `CombatActorBase.RenderWeaponOverlay` for the weapon swing.
* The fix is to sync the region yourself, mirroring what `AnimatedSprite.SetAnimation()` does internally:
  ```csharp
  sprite.Controller.SetFrame(frame);
  if (weapon.Sheet is not null)
      sprite.TextureRegion = weapon.Sheet.TextureAtlas[sprite.Controller.CurrentFrame];
  ```
* All other sprites in the codebase (player, enemy, oil drum) are time-driven via `Sprite.Update(gameTime)`, so they never hit this. The weapon overlay is the only frame-stepped sprite.
