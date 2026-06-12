# Hit Detection Implementation Plan

## Problem

No hit detection exists. Entities can overlap but attacks never register — there is no system to detect when an attack's range intersects another entity, no damage application, and no knockback. The existing `CollisionComponent` only handles physics body separation (preventing overlap), not attack-vs-entity hit testing. This blocks Milestone 2 of ROADMAP.md.

## Design

### Data Model

**`HitboxData`** — a single attack rectangle on one frame:
```csharp
public record struct HitboxData
{
    public Vector2 Offset { get; init; }          // from entity center
    public Point Size { get; init; }
    public RectangleF CreateRectangle(Vector2 center, FacingDirection facing)
        // flips X when facing Left
}
```

**`MoveData`** — a complete attack, the primary authoring unit:
```csharp
public class MoveData
{
    public string Name { get; init; }             // "Punch", "Uppercut", etc.
    public string AnimationKey { get; init; }     // maps to AnimatedSprite animation name
    public int Damage { get; init; }
    public Vector2 Knockback { get; init; }
    public Dictionary<int, List<HitboxData>> FrameHitboxes { get; init; }
    // Future: Dictionary<int, HurtboxModifier> FrameHurtboxModifiers
}
```

Damage and knockback live on the **Move**, not per hitbox — a swing can have multiple hitbox rectangles on a frame but they all deal the same damage. Empty or missing frame entries = no hitbox that frame.

### Player Move Definitions (`Game/Entities/Player/PlayerMoves.cs`)

```csharp
public static class PlayerMoves
{
    public static readonly Dictionary<string, MoveData> All = new()
    {
        ["attack1"] = new()
        {
            Name = "Punch",
            AnimationKey = "attack1",
            Damage = 10,
            Knockback = new(120, 0),
            FrameHitboxes = new()
            {
                [1] = [new() { Offset = (60, 0), Size = (40, 40) }],
                [2] = [new() { Offset = (60, 0), Size = (40, 40) }],
            }
        },
        ["attack2"] = new()
        {
            Name = "Uppercut",
            AnimationKey = "attack2",
            Damage = 15,
            Knockback = new(160, -40),
            FrameHitboxes = new()
            {
                [1] = [new() { Offset = (70, -10), Size = (50, 50) }],
                [2] = [new() { Offset = (70, -10), Size = (50, 50) }],
            }
        },
        ["attack3"] = new()
        {
            Name = "Strong Punch",
            AnimationKey = "attack3",
            Damage = 25,
            Knockback = new(200, 0),
            FrameHitboxes = new()
            {
                [2] = [new() { Offset = (80, 0), Size = (60, 40) }],
            }
        },
    };
}
```

No `FrameHitboxMap` class needed — the dictionary on each `MoveData` already serves that role.

### Runtime: `HitboxService`

Single shared instance owned by `GameLoop`:

```csharp
public class HitboxService
{
    public void RegisterFrameHitboxes(SpatialEntity owner, MoveData move, int frameIndex, FacingDirection facing)
    // resolves move.FrameHitboxes[frameIndex] to world-space RectangleF, stores internally

    public List<HitResult> ResolveHits(IEnumerable<SpatialEntity> targets)
    // tests active hitboxes vs targets, deduplicates per (HitboxData, Target) pair per-owner
    // hitboxes persist across calls until Clear(owner) — NOT auto-cleared

    public void Clear(SpatialEntity owner)
    // owner-scoped: removes only that owner's hitboxes AND that owner's resolved-this-frame tracking

    public void ClearAll()
    // nukes everything — used on game reset
}

public record struct HitResult
{
    public SpatialEntity Target { get; init; }
    public int Damage { get; init; }
    public Vector2 Knockback { get; init; }
    public SpatialEntity Source { get; init; }
}
```

### Entity Changes

**`ActorEntity`** additions:
- `HitboxService` property (assigned by `GameLoop`)
- `MoveData CurrentMove { get; set; }` — set when attack state begins, null otherwise
- `FacingDirection Direction { get; set; }` — moved up from `PlayerEntity` (was private set, now public)
- `_animationFrameIndex` + `_lastRegisteredAnimationFrame` — manual frame tracking (see Gotchas)

**`PlayerEntity`** — already sets `_pendingAttackType`. In `OnAttackingEntry`:
1. Look up `PlayerMoves.All[animKey]` and assign to `CurrentMove`
2. Call `ResetAnimationFrameIndex()`
3. `CurrentMove` is consumed by `ActorEntity.Update()` each frame during the attack

**`FacingDirection`** enum extracted from `PlayerEntity` to its own file `Core/Entities/FacingDirection.cs`:
```csharp
public enum FacingDirection { Left, Right }
```

### Integration Flow (`GameLoop.Update()`)

```
1. entity.Update(gameTime)              // sprite advances, state ticks, hitbox registration inlined
2. hitboxService.ResolveHits(actors) →  // hit detection against active hurtboxes
     target.TakeDamage(hit.Damage)
     target.Position += hit.Knockback   // instant (TODO: velocity/physics lerp)
3. collision.Update(gameTime)           // existing body/obstacle collisions
4. entity.ClampToBounds()               // keep within level movement bounds
```

Hitbox registration happens **inside** `ActorEntity.Update()`, not as a separate GameLoop step:
```
ActorEntity.Update():
  1. detect sprite frame change → _animationFrameIndex++
  2. if CurrentMove != null && new frame → HitboxService.Clear(this) + RegisterFrameHitboxes()
```

### Multi-Hit Prevention

`Dictionary<SpatialEntity, HashSet<(HitboxData, SpatialEntity)>>` keyed by hitbox owner.

Within a single animation frame, the same `(HitboxData, target)` pair can only resolve once per owner. This prevents double-hitting on consecutive game ticks during the same frame.

**Critical: per-owner scoping.** A global `HashSet` would let Entity A's frame advance reset the resolved-tracking for Entity B, causing Entity B's hitboxes to re-resolve against the same target. The per-owner `Dictionary` ensures `Clear(owner)` only affects that owner's tracking.

Cleared by `Clear(owner)` when the animation frame advances or the attack ends, or by `ClearAll()` on game reset.

### Hurtbox

The entity's `Frame` (centered bounding box) serves as the hurtbox. `HitService.ResolveHits` tests `hitboxBounds.Intersects(target.Frame)`.

Future: `MoveData.FrameHurtboxModifiers` can override the hurtbox size dynamically per frame.

### Debug Visualization

In `ActorEntity.DrawDebug()`:
- Red `DrawRectangle` for each active hitbox (from `HitboxService.GetActiveHitboxBounds()`)
- Blue outline for `Frame` (hurt area)

## Gotchas & Difficult Findings

### 1. `_resolvedThisFrame` must be per-owner, not global

The initial plan specified a single `HashSet<(HitboxData, ActorEntity)>`. Review caught that `Clear(owner)` called `_resolvedThisFrame.Clear()` — wiping ALL entities' resolved-hit tracking when only one entity's animation frame advanced. This created a cross-entity double-hit vulnerability: Entity B's hitboxes could re-resolve against already-hit targets because their resolved status was cleared when Entity A's frame changed.

**Fix:** `Dictionary<SpatialEntity, HashSet<(HitboxData, SpatialEntity)>>` with `Clear(owner)` calling `Remove(owner)` instead of `Clear()`. A `ClearAll()` method explicitly nukes everything for game reset.

### 2. `PlayerEntity.Reset()` and `GameLoop.ResetGame()` must clear hitbox state

Game reset (new game, level restart) did **not** clear `CurrentMove`, `_animationFrameIndex`, or `_hitboxService._activeHitboxes`. Stale hitboxes from the previous game would persist and could resolve against entities in the new game, applying phantom damage. Additionally, a stale non-null `CurrentMove` after reset would cause `ActorEntity.Update()` to register hitboxes against the idle animation's frame counter.

**Fix:**
- `PlayerEntity.Reset()`: set `CurrentMove = null`, call `ResetAnimationFrameIndex()`
- `GameLoop.ResetGame()`: call `_hitboxService.ClearAll()` before resetting entities

### 3. `Sprite.Controller.CurrentFrame` is the atlas index, not the animation frame index

MonoGame.Extended's `AnimatedSprite.Controller.CurrentFrame` returns the global texture atlas region index, NOT the 0-based position within the current animation's frame sequence. This means comparing consecutive `CurrentFrame` values to detect animation frame changes is unreliable for frame-index-based hitbox lookup.

**Fix:** Manual `_animationFrameIndex` counter incremented on every atlas frame change, reset by `ResetAnimationFrameIndex()` after every `SetAnimation()` call. The counter always starts at 0 for a new animation.

### 4. `AnimatedSprite.Controller` is replaced by `SetAnimation()`

(See AGENTS.md for full details.) Calling `SetAnimation()` may replace the `Controller` property with a new `IAnimationController` instance. Event subscriptions (e.g., `OnAnimationEvent += OnAnimationCompleted`) must happen AFTER `SetAnimation()`, or they attach to the orphaned controller. Always subscribe in `OnAttackingEntry` (after `SetAnimation`) and unsubscribe in `OnAttackingExit`.

### 5. Hitbox Register/Resolve Timing

Hitbox registration is **decoupled** from resolution:
- `RegisterFrameHitboxes()` is called inside `ActorEntity.Update()` when a frame change is detected
- `ResolveHits()` is called in `GameLoop.Update()` AFTER all entities have updated
- Hitboxes persist across `ResolveHits()` calls until `Clear(owner)` is called (next frame advance or attack end)

This means a single animation frame's hitboxes can resolve against targets on multiple consecutive game ticks, which is intentional — it gives consistent hit detection at varying frame rates.

### 6. `ActorEntity.TakeDamage` is a virtual no-op

The base `ActorEntity.TakeDamage(int amount)` is empty. `PlayerEntity` overrides it to apply damage and trigger state transitions (Hurt/Die). Enemy entities will also need their own override. This is by design — the base is a hook point for all actor types.

### 7. HitResult uses `SpatialEntity` not `ActorEntity`

`HitResult.Target` and `HitResult.Source` are typed as `SpatialEntity` (the common base), not `ActorEntity`. The `GameLoop` casts `hit.Target` to `ActorEntity` when applying damage. This was done to keep `HitboxService` agnostic of the full `ActorEntity` interface, but the cast in GameLoop will silently skip non-ActorEntity targets.

## Files

| Action | Path |
|---|---|
| Create | `MonoGameLearning.Core/Combat/HitboxData.cs` |
| Create | `MonoGameLearning.Core/Combat/MoveData.cs` |
| Create | `MonoGameLearning.Core/Combat/HitboxService.cs` |
| Create | `MonoGameLearning.Core/Entities/FacingDirection.cs` |
| Create | `MonoGameLearning.Game/Entities/Player/PlayerMoves.cs` |
| Modify | `MonoGameLearning.Core/Entities/ActorEntity.cs` |
| Modify | `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs` |
| Modify | `MonoGameLearning.Game/GameLoop/GameLoop.cs` |

## Tests (all GraphicsDevice-free)

| Test | What it covers |
|---|---|
| `HitboxData.CreateRectangle_RightFacing` | Offset applied correctly from center |
| `HitboxData.CreateRectangle_LeftFacing` | X mirrored correctly |
| `MoveData_FrameHitboxLookup_ValidFrame` | Returns hitboxes for a frame that has them |
| `MoveData_FrameHitboxLookup_EmptyFrame` | Returns empty for wind-up/recovery |
| `MoveData_FrameHitboxLookup_InvalidFrame` | Returns empty for out-of-range frame |
| `HitboxService_RegisterAndResolve_Hit` | Overlapping hitbox + hurtbox produces `HitResult` |
| `HitboxService_RegisterAndResolve_NoHit` | Non-overlapping produces no results |
| `HitboxService_NoFriendlyFire` | Owner excluded from hit results |
| `HitboxService_DoubleHitPrevention` | Same hitbox can't hit same target twice per resolve |
| `HitboxService_ClearsAfterResolve` | Active list empty after `Clear()` |
| `HitboxService_MultipleTargets_OnlyOverlappingGetsHit` | Distant targets not hit |
| `TestActor_TakeDamage_ReducesHealth` | Damage deduction works |
| `HitboxService_HitAppliesDamage` | End-to-end: hit → damage taken |

Test actor: `TestActor : SpatialEntity` with `Frame`, stub `TakeDamage()`, no `GraphicsDevice` needed. Note: `TestActor` extends `SpatialEntity` directly, not `ActorEntity`, so it doesn't require an `AnimatedSprite`.

## Order of Implementation

1. `HitboxData` + `MoveData` — pure data, zero dependencies
2. `HitboxService` — depends on above
3. Tests for 1–3
4. `FacingDirection.cs` — extract from PlayerEntity to Core
5. `PlayerMoves` — game-specific data definitions
6. `ActorEntity` changes — `HitboxService`, `CurrentMove`, `Direction`, frame tracking
7. `PlayerEntity` changes — `CurrentMove` assignment on attack, `override TakeDamage`, reset hygiene
8. `GameLoop` integration — wire up `HitboxService`, update loop changes, reset hygiene
9. Debug drawing
10. `dotnet build` + `dotnet test`

## Verification

- `dotnet build` — 0 warnings, 0 errors
- `dotnet test` — 78 pass, 0 fail