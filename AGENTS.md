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
  * **`StateMachines.CombatStateMachineConfigurator`**: Shared builder for the five combat states (Attacking/Hurt/KnockedDown/Dying/Dead), parameterized over each actor's state/trigger enums via `CombatStateSet<TState>`/`CombatTriggerSet<TTrigger>`; `PlayerStateMachine`/`EnemyStateMachine` keep only their exclusive states and delegate here.
  * **`Levels.LevelData`**: The concrete level-data record (background count, dimensions, end trigger, props/pickups/wave defs) with a shared `MovementBounds` derivation and `Validate` checks — replaces the old abstract `Level` base. Produced by Game-side `Level1`.
  * **`Levels.EntityPool<TEnemy>`**: Generic rent/return/sentinel pool (per-type stacks). Game behavior (entity reset) comes from subclasses overriding `OnRentEnemy`; pool-return hitbox/dedup cleanup goes through the entity's `ClearCombatState()`; sprite warmup happens in the Game-side `LevelEntityFactory` createEnemy factory.
  * **`Levels.LevelDirectorCore<TEnemy>`** (where `TEnemy : CombatActorBase, IPickupDropper`): Encounter core — wave gating, scroll-lock, snapshots, prop/pickup/drop spawning, debug draw. Game content (drums, pickups, weapons, enemy visuals, spawn-walk) is injected via `createProp`/`createPickup`/`getWeapon`/`createEnemy`/`onEnemySpawned` delegates; enemy spawning sizes off the enemy's own dimensions.

* **Game Project (`MonoGameLearning.Game`)**
  * **`GameLoop`**: Inherits from `GameCore`. Implements the specific game logic (`Update`, `Draw`), manages entities (like the player), and handles the main application lifecycle. Composition root; entity content factories live in `Levels.LevelEntityFactory` and pure rules (lives/music/respawn) in `GameLoop`'s `GameLoopRules`.
  * **`Program.cs`**: The entry point, using C# top-level statements to bootstrap `GameLoop`.
  * **`Entities`**: Game-specific entities, such as `PlayerEntity`.
  * **`Levels.LevelDirector` / `Levels.EnemyPool`**: Thin Game-side subclasses of `LevelDirectorCore<EnemyEntity>` / `EntityPool<EnemyEntity>` — they only assemble the Game-specific pools and pass content factories through. **`Levels.LevelEntityFactory`**: the Game-side content factory (`CreateProp`/`CreatePickup`/`CreateEnemy`/`ConfigureSpawnedEnemy` + sprite warmup) leaned on by `GameLoop`.
* **`Weapons`**: Static melee weapon defs (e.g., `BatWeapon`) built from `Core.Combat.MeleeWeaponDef` that swap `Attack1Move`/`AttackMove` while armed. Swing rendering uses a 4-frame `bat-texture.png`/`bat.json` atlas via `AnimatedSprites.BatSprite`, positioned per `attack` frame via the actor's hand table plus the weapon's `SwingHandleOffsets`/`CarryHandleOffset`; hitboxes fire at swing apex (frames 2–3) only. The overlay is drawn directly by atlas region name (`MeleeWeaponDef.ResolveWeaponRegionName`: `<SwingPrefix>-<NN>` while the actor is performing the weapon's own swing move, else the single `CarryRegion` hold pose — attack2/attack3 keep the hold pose because `CombatActorBase.IsWeaponSwingActive` requires `CurrentMove == melee.SwingMove`), so it never frame-steps a sprite, so it stays in sync with the player's attack animation with no `SetFrame()`/`TextureRegion` workaround. `AnimatedSprites.BatPickupSprite` owns the separate static `bat-pickup.png` texture used by `WeaponPickupEntity` for the dropped-pickup icon (same pattern as `FoodPickupSprite`/`apple-pickup.png`). Source art is authored in **Aseprite** under `MonoGameLearning.Game/Sources/Weapons/<Name>/` and exported as a sprite-sheet texture + JSON (Aseprite's `JSON Array` or `JSON Hash` data format); `Utils/aseprite_to_monogame_extended.py` converts that export to the monogame-extended atlas. Aseprite `frameTags` become animations: each tag's source-frame run maps to a `<slug>-<tag>-NN` region run (matching `SpriteAnimationDef`/`DefineFrames`); untagged frames map to `<slug>-frame-NN`. Anchors are **split by ownership**: the **actor owns where its hand is per animation frame**, and the weapon owns only its own grip. The actor's `CombatActorBase.HandAnchors` (a `Core.Animation.HandAnchorTable`, one point per frame, relative to `Position`) is authored as a per-frame Aseprite `hand` slice pivot — `bounds.origin + pivot` minus half the frame size, emitted by `Utils/aseprite_to_monogame_extended.py` as a paste block for `PlayerSprite`/`EnemySprite`; the weapon's grip in frame-local pixels comes from its own Aseprite `handle` slice (`MeleeWeaponDef.CarryHandleOffset`/`SwingHandleOffsets`, frame-local). The overlay anchor is the one-line formula `WeaponDef.ComputeAnchor(hand, handleOffset, FrameCenter, Scale) = hand - (handleOffset - FrameCenter) * Scale`, where `Scale` is the weapon's render scale relative to the actor (default `1`; the bat uses `0.5`). Hand points are absolute actor-unit offsets from `Position` (not relative to each other — the carry-to-swing sweep is authored explicitly). The overlay positions the region center at `Position + anchor * actorScale` and scales the texture by `actorScale * Scale` — the weapon scale cancels out of the grip-on-hand invariant for any value. Pose-indexed data (actor hand points and weapon swing frames) is indexed by `SpriteRenderer.AnimationFrame` — a true animation-relative counter that starts at 0 in `SetAnimation` and increments on each frame change — **not** by `AnimationController.CurrentFrame` (which is the **atlas region index**, see the pitfalls below) and not by the monotonic `AnimationFrameTracker` (whose `TryGetNewFrame` stays the *new-frame event* source for hitboxes and the throwable `SpawnFrame`). `HandAnchorTable` wraps `AnimationFrame` with `%` for looping runs; melee clamps it for the non-looping swing, and the render path `Debug.Assert`s the swing index stays inside the run. A missing actor hand entry falls back to `Vector2.Zero` with a one-per-animation `Debug.WriteLine`. Debug draw overlays the weapon region center / anchor (orange, with name label), the resolved actor hand point (cyan) and the weapon grip point (green) — cyan and green coincide when the actor hand table and the weapon handle offsets are both correct — and the hand is flagged with a `Debug.WriteLine` warning when it falls outside the actor's sprite region. Because each side owns its own data, a weapon redraw needs only re-export + re-paste the weapon offsets, and an actor redraw only re-export + re-paste the `hand` table — neither forces retuning the other side. **Throwables** reuse the whole carry-overlay path: `Core.Combat.WeaponDef` is the shared base (`Name`/`Scale`/`FrameCenter`/carry pose + virtual `ResolveRegion`/`ResolveHandleOffset`/`HasHandleOffsets` + the static `ComputeAnchor`), `MeleeWeaponDef` overrides them with the swing run, and `ThrowableWeaponDef` (`KnifeWeapon`) keeps the carry defaults and adds `ThrowMove`/`SpawnFrame`/projectile fields. `PlayerEntity.PrimaryAttack` throws a held throwable (consuming it) via the `CombatActorBase.OnFrameAdvanced` hook at `SpawnFrame`; `ProjectileService` pools `ProjectileEntity` instances that re-register one hitbox per frame and despawn on the first hit (`DamageInfo.Source`) or past `MaxRange`. The projectile's flight art is a **time-driven region run**, not a code rotation: `ThrowableWeaponDef.ProjectileRegionPrefix`/`ProjectileFrameCount`/`ProjectileFrameDuration`/`ProjectileLoop` are resolved by `ResolveProjectileRegion(flightFrameIndex)` (cached `Texture2DRegion[]`, direct atlas indexing — never an `AnimatedSprite`, so no `SetFrame`/`TextureRegion` sync needed) and advanced by `ProjectileEntity`'s own frame timer; `FlightFrameIndex` exposes the current frame for debug/tests, and `SpinDegreesPerSecond` is additive rotation that stays `0` for an authored spin/ignition run (the knife's 7-frame `knife-fly-NN` run; a Molotov could animate its ignition this way).
* **`Sprites`**: Sprite management and animation logic (e.g., `PlayerSprite`, `EnemySprite`). Each actor builds its animations from a `SpriteSheetAsset` (one `SpriteAnimationDef` per tag) and owns a static `HandAnchorTable` (`HandAnchors`) of per-frame hand points used for weapon attachment (authored via the Aseprite `hand` slice — see `Weapons`). Hand tables are **per actor** even when two actors share an atlas, so a future sprite swap stays local to that actor. `PlayerSprite`/`EnemySprite` currently share the placeholder `adventurer` atlas (regions `adventurer-<tag>-NN`; the getup animation resolves from the `getup` tag, whose source frames are named `adventurer-stand-*`). `Utils/build_adventurer_sheet.py` rebuilds that sheet deterministically from the named source frames, and `Utils/verify_adventurer_sheet.py` checks any export against the expected frames, tags, and `hand` pivots.
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
* **Markdown Lint**: Whenever any `.md` file is created or modified (including plan files under `.kilo/plans/implemented/` and `.kilo/plans/todo/`), run the markdown linter (`.markdownlint-cli2.jsonc` config) and fix all warnings it reports before the change is complete.
* **Testing**: Always run `dotnet test` to execute unit tests after making any changes to verify no regressions were introduced.
* **Manual Testing**: `MANUAL_TESTING.md` at the repo root is the authoritative catalog of behavior that **cannot** be covered by `dotnet test` (needs a live window: rendering/animation quality, audio, real input feel, window/resize behavior, advanced debug tools). Comments that cite "not covered by tests" are acceptable; a "covered by tests" conclusion is NOT. When implementing features or fixing bugs that touch any area listed in the manual plan, update `MANUAL_TESTING.md` as needed — add, remove, or update rows so the plan stays a current source of truth for what a human must verify by hand.
* **Learning Reference (`LEARNING.md`)**: `LEARNING.md` at the repo root is the living study guide of the design patterns used in the code and the architecture-analysis vocabulary used in reviews. When a session introduces a new pattern, adds a review term, or restructures the code in a way that invalidates an existing entry, update `LEARNING.md` in the same change so it stays a current, accurate guide.
* **Architecture Smells**: When implementing or reviewing, watch for architecture smells and resolve them with minimal fixes that move responsibility to the right owner — do not invent new abstractions for a single value. The catalog of common smells and their minimal resolutions lives in `LEARNING.md` under "Common architecture smells & minimal resolutions". When a session introduces a new smell or resolution, add it to that catalog in the same change.
* **Skill File Updates**: Every change — whether made with a skill or without — must include an explicit **skill check** step: audit the skills under `.kilo/skills/` (and any built-in skills) to see which ones touch the changed workflows, tools, file paths, conventions, or code, and update the relevant `SKILL.md` files in the same change so the next run follows the new reality instead of stale instructions. When a change makes a skill's documented steps, commands, sample code, or referenced file paths outdated or wrong, updating that `SKILL.md` is mandatory, not optional. This check is step 4 of the **Mandatory Pre-Completion Checklist** below, alongside the `LEARNING.md`/`MANUAL_TESTING.md` and this file's upkeep, so it is performed every time rather than left to memory. If a change touches a skill-covered area and `SKILL.md` needs updating, do it in the same change.
* **Mandatory Pre-Completion Checklist**: Before marking any implementation task as complete, the following steps MUST be performed in order:
  1. Write unit/integration tests covering all new or modified logic.
  2. Run `dotnet build --warnaserror` to verify compilation with zero warnings.
  3. Run `dotnet test` to verify all tests pass with no regressions.
  4. Run the targeted skill check (see **Skill File Updates**) and update any `SKILL.md` whose documented steps, commands, file paths, or sample code this change invalidates; also refresh `LEARNING.md`/`MANUAL_TESTING.md`/`AGENTS.md` where this change touches patterns, manual-verification rows, or conventions documented there. Run the markdown linter on any `.md` file created or modified.
  5. If any step fails, fix the issue before proceeding.
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

* Driving frames manually via `SetFrame()` without calling `Sprite.Update()` leaves the sprite stuck on the first frame's texture region — the frame *index* is correct (so anchor/hitbox math works), but the *drawn texture* never changes.
* The fix is to sync the region yourself, mirroring what `AnimatedSprite.SetAnimation()` does internally:

  ```csharp
  sprite.Controller.SetFrame(frame);
  sprite.TextureRegion = sprite.Sheet.TextureAtlas[sprite.Controller.CurrentFrame];
  ```

* Two different regions can be animation drivers, and it matters which clock owns the frame: the **held weapon overlay follows the actor's animation frame** (`ResolveRegion`/`ResolveHandleOffset` take `actorFrameIndex` from `SpriteRenderer.AnimationFrame`, and the actor's own `HandAnchors` lookup takes `CurrentAnimationKey` + `AnimationFrame`, so swing regions and hand points stay keyed to the arm), while a **thrown weapon animates on its own clock** (`ThrowableWeaponDef.ResolveProjectileRegion(flightFrameIndex)` is advanced by `ProjectileEntity`'s timer). The melee overlay still never calls `AnimatedSprite.SetFrame`/`SetAnimation` — it resolves regions by name per actor frame — and the projectile indexes its cached region array directly, so neither hits the `Controller`/`TextureRegion` pitfall below. A pooled projectile also cannot own a per-spawn `AnimatedSprite` without allocating, which is why the flight run is a timer over a cached `Texture2DRegion[]` rather than an `AnimationController`. Reintroduce the manual `SetFrame` pattern only if a future effect genuinely needs an `AnimatedSprite`, and pair every `SetFrame` with the region sync above.

### `AnimationController.CurrentFrame` is the atlas region index, not the animation frame

`AnimationController.CurrentFrame` returns `SpriteSheetAnimationFrame.FrameIndex`, and `SpriteSheetAnimationBuilder` fills that from `TextureAtlas.GetIndexOfRegion(regionName)` — the region's index in the **whole atlas**, in content-build order (`SpriteSheetAnimationFrame` is documented as "the index of the frame in the overall sprite sheet"). Indexing per-animation pose data (hand anchors, swing frames) with it silently reads the wrong pose whenever an animation's atlas offset is not a multiple of its frame count. `SpriteSheetAnimationBuilder.AddFrame`/the animation name→region runs are fine; only the *clock* is wrong.

* The failure is easy to miss because it works by accident for some animations: with a 6-frame `run` whose regions start at atlas index 31, index `31 % 6 == 1` reads one frame ahead; a 4-frame attacker starting at atlas index 4 reads correctly. Adding or reordering animations changes which ones break.
* Use `SpriteRenderer.AnimationFrame` for anything indexed per animation frame: it starts at 0 in `SetAnimation` and increments on each frame change. Wrap with `% frameCount` for looping runs, clamp for non-looping ones.
* Keep `AnimationFrameTracker` (monotonic *new-frame event* counter, reset when a move starts) for hitbox registration and the throwable `SpawnFrame` — it is not a pose index either.
