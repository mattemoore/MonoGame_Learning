# Entity Inheritance → Composition Analysis

## Current Inheritance Hierarchy (Post-Refactor)

```
Entity (abstract)                        — 6 lines, pure abstract: Position, Width, Height, Name, Rotation
  abstract Update(), DrawDebug()
  |
  +-- SpatialEntity (abstract)           — 25 lines, adds Frame (RectangleF computed from Position/Width/Height)
  |     |
  |     +-- ActorEntity (abstract)       — ~90 lines, ICollisionActor + IAnimated + IHitboxProvider
  |     |     Sprite, AnimationFrameTracker (composed), Scale, MovementBounds,
  |     |     HitboxService, CurrentMove, Direction, ClampToBounds(),
  |     |     OnCollision(), TakeDamage(), hitbox registration per animation frame
  |     |     2 constructors: (position, scale, sprite) and (position, width, height) — the latter
  |     |     for test use without GraphicsDevice; Sprite held as null! (guarded in Update())
  |     |     |
  |     |     +-- PlayerEntity            — 170 lines, ICombatant: state machine (PlayerStateController),
  |     |           Health, Attack1/2/3(), TakeDamage(), Move(), Reset(), direction flipping,
  |     |           animation event wiring for state transitions
  |     |
  |     +-- BackgroundEntity             — 30 lines: background sprite, MovementBounds, Draw()
  |
+-- TestActorEntity (test only, ActorEntity via width/height constructor, collision + clamping tests)
  +-- TestSpatialEntity (test only, SpatialEntity + ICombatant, hitbox resolution tests)

```
## Verification of Concerns

Each layer adds a clear set of concerns:

| Layer | Concerns |
|-------|----------|
| `Entity` | Identity (Name), spatial properties (Position, Width, Height, Rotation) |
| `SpatialEntity` | Axis-aligned bounding box (Frame), debug visualization |
| `ActorEntity` | Animated sprite + animation frame counter, hitbox registration, collision contract (ICollisionActor), movement bounds clamping + movement input (IMoveableEntity), base TakeDamage |
| `PlayerEntity` | Player-specific state machine, health system, attack input routing, movement input passes through inherited ActorEntity properties, animation event -> state machine bridge |
| `BackgroundEntity` | Static background rendering, level movement bounds region |

## Analysis: Should We Switch to Composition?

### ✅ What Works Well Currently

1. **Hierarchy depth is shallow** (3 levels max) — no diamond problem, no deep fragile chains.
2. **Each level has focused responsibility** — no single class does too much *in principle*.
3. **`Entity` and `SpatialEntity` are very stable** — unlikely to need changes.
4. **The state machine is already composed** — `PlayerEntity` delegates to `PlayerStateController` (a separate class with config callbacks). This is a composition success story.
5. **HitboxService is already composed** — entities hold a reference rather than inheriting hitbox logic.
6. **Test patterns already work around the hierarchy** — `TestActor` extends `SpatialEntity` directly instead of `ActorEntity` to avoid the `AnimatedSprite` dependency.

### ❌ Pain Points

1. **`ActorEntity` bundles too many cross-cutting concerns** in a single 107-line class:
   - Animation management (Sprite, frame counter, ResetAnimationFrameIndex)
   - Hitbox registration per animation frame
   - Collision contract (`ICollisionActor`, `OnCollision`, `Bounds`)
   - Movement bounds clamping
   - Base `TakeDamage` (empty virtual — a smell)
   - Drawing (`Draw()`, `DrawDebug()`)

2. **Testing friction**: `ActorEntity` requires an `AnimatedSprite` (needs `GraphicsDevice`), so tests can't instantiate it. This forces parallel test-only types (`TestActor`, `TestCollisionEntity`) that duplicate behavior.

3. **Polymorphism via downcasting**: `GameLoop.cs:136` casts `hit.Target` to `ActorEntity` to call `TakeDamage`. If we had non-`ActorEntity` combatants (e.g., destructible objects), this would break.

4. **`TakeDamage` as empty virtual**: The base implementation does nothing. Subclasses must remember to override it. An interface (`ICombatant`) would enforce this contract.

5. **No type-level distinction between player and enemy**: Both would extend `ActorEntity`. If enemies need different state machines, they'd either duplicate or awkwardly share `PlayerStateController`.

### 🏆 Recommendation: Interface-First Hybrid (Not Full Composition)

Full composition (ECS-style entities as bags of components) is **overkill** for this project. It would:
- Add significant ceremony (component registration, system dispatch, entity queries)
- Scatter state across many small objects, making debug traceability harder
- Work against the Stateless state machine pattern already in use
- Require rewriting all existing tests

Instead, **keep the shallow inheritance but extract cross-cutting behaviors into interfaces + helper types**. This gives the benefits of composition (loose coupling, testability, contract enforcement) without the ceremony of a full component system.

## Concrete Plan: Interface-First Hybrid

### Phase 1: Define Interfaces (Contract Extraction) — ✅ Done

Extract existing implicit contracts into interfaces. No behavioral changes yet.

| Interface | Members | Consumed By |
|-----------|---------|-------------|
| `ICombatant` | `int Health`, `int MaxHealth`, `void TakeDamage(int)`, `bool IsAlive`, `event EventHandler Died` | GameLoop hit resolution, PlayerEntity |
| `IAnimated` | `AnimatedSprite Sprite`, `void ResetAnimationFrameIndex()` | ActorEntity itself, drawing code |
| `IHitboxProvider` | `MoveData CurrentMove`, `HitboxService HitboxService`, `FacingDirection Direction` | GameLoop, HitboxService, ActorEntity |
| `IMoveableEntity` | `Vector2 MovementDirection`, `float Speed`, `RectangleF MovementBounds` | ActorEntity (inherited by PlayerEntity) |
| `ICollisionActor` (already exists) | `IShapeF Bounds`, `void OnCollision(CollisionEventArgs)` | CollisionComponent, ActorEntity |

> **Note**: Renamed from `IMovable` to `IMoveableEntity` to avoid collision with `MonoGame.Extended.IMovable`.

### Phase 2: Decouple `AnimatedSprite` from `ActorEntity` Constructor — ✅ Done

- Added a **second protected constructor** `(name, position, width, height, rotation)` that does NOT require `AnimatedSprite`. This allows `ActorEntity` subclasses in tests without a `GraphicsDevice`.
- The primary constructor `(name, position, scale, sprite, rotation)` remains unchanged for production use.
- `Sprite` property uses `private set` — encapsulation preserved. The `IAnimated` interface only requires `{ get; }`.
- An explicit null guard (`if (Sprite is null) return;`) was added in `Update()` on top of `Debug.Assert` to prevent NPE in Release builds.

> **Deviation from original plan**: Instead of making `Sprite` publicly settable, we used a second protected constructor with `private set`. This is stricter: external code cannot reassign `Sprite`, preserving the original encapsulation. The null guard was added during code review to handle the test-constructed null-`Sprite` edge case in Release builds.

### Phase 3: Extract Animation Frame Tracker from `ActorEntity` — ✅ Done

Pulled `_animationFrameIndex`, `_lastRegisteredAnimationFrame`, and the frame-advance + hitbox registration logic into a dedicated `AnimationFrameTracker` class:

```csharp
public class AnimationFrameTracker
{
    public void AdvanceOnFrameChange(AnimatedSprite sprite, GameTime gameTime) { /* detect frame change */ }
    public bool TryGetNewFrame(out int newFrameIndex) { /* first-call returns true, subsequent returns false until next advance */ }
    public int FrameIndex { get; }
    public void Reset() { ... }
}
```

`ActorEntity.Update()` composes `AnimationFrameTracker` instead of managing the counters inline.

> **Deviation**: Named `AdvanceOnFrameChange` + `TryGetNewFrame` (not `Update` + `HasFrameChanged`). Takes `GameTime` parameter because `AnimatedSprite.Update(gameTime)` needs it.

### Phase 4: Replace Downcasts with Interface Checks in `GameLoop` — ✅ Done

`GameLoop.cs:134-142`:
```csharp
// Before:
if (hit.Target is ActorEntity actor)
{
    actor.TakeDamage(hit.Damage);
    actor.Position += hit.Knockback;
}

// After:
if (hit.Target is ICombatant combatant)
{
    combatant.TakeDamage(hit.Damage);
    hit.Target.Position += hit.Knockback;
}
```

> **Deviation**: Also simplified knockback application to use `hit.Target.Position` (a `SpatialEntity`) instead of casting to `ActorEntity`.

### Phase 5: Add Enemy Entity — ⏳ Not Yet Implemented

Not implemented in this pass. Ready to do when needed:
- Extends `ActorEntity` (reuses animation, hitbox, collision, clamping)
- Implements `ICombatant` (shared interface with `PlayerEntity`)
- Has its own state machine (`EnemyStateController`) using the same Stateless pattern

### Phase 6: Clean Up Test Types — ✅ Done

| Old Type | Replaced With |
|----------|--------------|
| `TestCollisionEntity` | `TestActorEntity(ActorEntity)` — concrete subclass using width/height constructor, tests collision + clamping |
| `TestActor` | `TestSpatialEntity(SpatialEntity + ICombatant)` — concrete subclass for hitbox resolution tests |

- `TestActorEntity` in `ActorCollisionTests.cs` is a minimal 3-line subclass of `ActorEntity`
- `TestSpatialEntity` in `HitboxTests.cs` extends `SpatialEntity` and implements `ICombatant`
- No `SpriteFactory.CreateNull()` was needed; the protected constructor approach proved simpler

### New Tests Added

| File | Tests |
|------|-------|
| `MonoGameLearning.Game.Tests/AnimationFrameTrackerTests.cs` | 6 tests: initial state, TryGetNewFrame first-call/later-call, Reset, idempotency |

### Files Modified

| File | Change |
|------|--------|
| `MonoGameLearning.Core/Entities/Entity.cs` | No change (stable) |
| `MonoGameLearning.Core/Entities/SpatialEntity.cs` | No change (stable) |
| `MonoGameLearning.Core/Entities/ActorEntity.cs` | Extracted frame tracker, added second protected constructor, added null guard in `Update()`, implements `IAnimated`/`IHitboxProvider`/`IMoveableEntity`, added `Speed` and `MovementDirection` properties |
| `MonoGameLearning.Core/Entities/BackgroundEntity.cs` | No change needed |
| `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs` | Implements `ICombatant`, added `using` for interfaces; removed `MovementDirection` and `BASE_MOVEMENT_SPEED` (now inherited from `ActorEntity` via `IMoveableEntity`), `Move()` uses `Speed` property instead of const |
| `MonoGameLearning.Game/GameLoop/GameLoop.cs:134` | Changed cast to `ICombatant`, simplified knockback line |
| `MonoGameLearning.Game.Tests/ActorCollisionTests.cs` | Removed `TestCollisionEntity`, added `TestActorEntity` |
| `MonoGameLearning.Game.Tests/HitboxTests.cs` | Removed `TestActor`, added `TestSpatialEntity` (implements `ICombatant`), changed hitbox damage test to use `is ICombatant` |
| `MonoGameLearning.Core/Combat/HitboxService.cs` | No change needed (already uses `SpatialEntity`) |

### New Files Created

| File | Purpose |
|------|---------|
| `MonoGameLearning.Core/Combat/ICombatant.cs` | Combat contract |
| `MonoGameLearning.Core/Entities/Interfaces/IAnimated.cs` | Animation contract |
| `MonoGameLearning.Core/Combat/IHitboxProvider.cs` | Hitbox data contract |
| `MonoGameLearning.Core/Entities/Interfaces/IMoveableEntity.cs` | Movement contract (implemented by ActorEntity) |
| `MonoGameLearning.Core/Entities/AnimationFrameTracker.cs` | Extracted animation frame counter logic |
| `MonoGameLearning.Game.Tests/AnimationFrameTrackerTests.cs` | 6 unit tests for AnimationFrameTracker |

### Test Results

- 78 existing tests pass unmodified
- 6 new `AnimationFrameTrackerTests` pass
- Total: 84 tests, 0 failures, 0 errors
