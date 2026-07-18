# Concrete vs Interface Parameters — Tier A Implementation Plan

## Goal

Apply 4 high-value, low-cost interface tightening changes to remove over-specified concrete-typed parameters without expanding abstraction surface area. These changes target real test friction and dead data on the wire.

**Out of scope (deferred):** Decoupling `LevelDirector`, `CameraController`, `EnemyEntity.Target` from `Entity`/`PlayerEntity` — confirmed not in this round.

## Background

The codebase already extensively passes interfaces where it matters (`IDamageable`, `IHitboxProvider`, `IMoveableEntity`, `IUpdatable`, `IRenderable`, etc.). Concrete-typed friction is concentrated in 4 spots:

1. `HitResult.Target : Entity` is over-specified — callers always narrow to `IDamageable`.
2. `HitResult.Source : Entity` is **unused** at every call site — dead field.
3. `HitboxService.Clear/ClearAttackDedup/GetActiveHitboxBounds(Entity)` use the parameter purely as a dictionary/set key — `IHitboxProvider` is the right identity.
4. `Mover.ClampToBounds(Entity)` forces test stubs to inherit `Entity` even though it only needs `Position`, `Width`, `Height`.

## Changes

### 1. `HitResult.Target` → `IDamageable`

**File:** `MonoGameLearning.Core/Combat/HitboxService.cs`

- Line 33–41: change `HitResult.Target` from `Entity` to `IDamageable`.
- Line 60–77: `RegisterFrameHitboxes` takes `Entity owner` — leave as-is (already accepts concrete Entity; only `CombatActorBase` calls it, and it IS-A Entity).
- Line 79–114: `ResolveHits` constructs `HitResult` — change `_resultBuffer.Add(...)` block to set `Target = target as IDamageable` (guarded by the existing `is IDamageable` check at line 99; refactor so we only add results that pass the check).
- Line 56: change `_attackDedup : Dictionary<Entity, HashSet<Entity>>` to `Dictionary<IHitboxProvider, HashSet<IDamageable>>` (or keep Entity keys for the dedup map and convert only on output — see implementation note below).
- Line 45–53: `ActiveHitbox.Owner : Entity` → keep as `Entity` for now (used as equality key in `_activeHitboxes` and as `target.Frame` access at line 90; `IHitboxProvider` impls are always `Entity`-derived so behavior is identical).

**Implementation note:** The dedup map currently keys `Entity → HashSet<Entity>`. Since `HitResult.Target` becomes `IDamageable`, change the inner set to `HashSet<IDamageable>`. At dedup-check time, if the target is not `IDamageable`, skip (cannot hit). This removes the redundant `is IDamageable` cast at line 99 inside the loop.

**Caller:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:187`

```csharp
// Before
if (hit.Target is IDamageable damageable)
{
    damageable.TakeDamage(new DamageInfo { ... });
    if (damageable.Faction == Faction.Enemy)
        _hudService.OnEnemyHit(damageable);
}

// After
var damageable = hit.Target;
damageable.TakeDamage(new DamageInfo { ... });
if (damageable.Faction == Faction.Enemy)
    _hudService.OnEnemyHit(damageable);
```

### 2. Remove `HitResult.Source`

**File:** `MonoGameLearning.Core/Combat/HitboxService.cs`

- Line 37: delete `public Entity Source { get; init; }`.
- Line 105 (inside `_resultBuffer.Add(...)`): delete the `Source = active.Owner,` line.
- Confirm with grep: `rg "hit\.Source" MonoGameLearning.*` — must return zero hits.

**Validation tests** (`MonoGameLearning.Game.Tests/HitboxTests.cs`):
- `RegisterAndResolve_Hit` (line 102–117) asserts `hits[0].Source` — remove that assertion.
- `SameFaction_NoHit`, `CrossFaction_Hits`, `HitboxService_HitAppliesDamage` — none use `Source`, no change.
- `DoubleHitPrevention`, `PerAttackDedup_*` — none use `Source`, no change.

### 3. `HitboxService.Clear/ClearAttackDedup/GetActiveHitboxBounds` → `IHitboxProvider`

**File:** `MonoGameLearning.Core/Combat/HitboxService.cs`

- Line 116: `public void Clear(Entity owner)` → `public void Clear(IHitboxProvider owner)`.
- Line 122: `public void ClearAttackDedup(Entity owner)` → `public void ClearAttackDedup(IHitboxProvider owner)`.
- Line 134: `public IReadOnlyList<RectangleF> GetActiveHitboxBounds(Entity owner)` → `public IReadOnlyList<RectangleF> GetActiveHitboxBounds(IHitboxProvider owner)`.
- Line 119: `_activeHitboxes.RemoveAll(hb => hb.Owner == owner)` — works because `ActiveHitbox.Owner : Entity` is still `Entity`-typed and `IHitboxProvider` impls are always `Entity` (compare via reference equality on the Entity).
- Line 124–126: same — `_attackDedup.Remove(owner)` works because key type matches impl runtime type.

**Callers to update:**
- `MonoGameLearning.Core/Entities/CombatActorBase.cs:137` — `HitboxService?.Clear(this)` — `this` is `CombatActorBase`, implements `IHitboxProvider`. ✓
- `MonoGameLearning.Core/Entities/CombatActorBase.cs:191,192` — same.
- `MonoGameLearning.Game/Levels/EnemyPool.cs:89,90` — `enemy.HitboxService.Clear(enemy)` — `enemy` is `EnemyEntity`, implements `IHitboxProvider`. ✓

**Tests to update:** `HitboxTests.cs` uses `TestSpatialEntity : Entity, IDamageable, ICollisionActor` — **does NOT implement `IHitboxProvider`**. These tests do NOT call `Clear`/`ClearAttackDedup` on these entities (they use service methods only). Confirm by grep — only line 119 in test (`Clear` of `HitboxService` itself, not the entity). Safe.

### 4. `Mover.ClampToBounds` → `IReadOnlyEntity`

**New file:** `MonoGameLearning.Core/Entities/Interfaces/IReadOnlyEntity.cs`

```csharp
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IReadOnlyEntity
{
    Vector2 Position { get; set; }
    int Width { get; }
    int Height { get; }
}
```

**Update `Entity`** (`MonoGameLearning.Core/Entities/Entity.cs`):

Add `: IReadOnlyEntity` to the class declaration. All three members already exist on `Entity` — no body changes. Verify:
- `Entity.Position` is `{ get; set; }` ✓ (line 8)
- `Entity.Width` is `{ get; init; }` — `init` is assignable to `{ get; }` in an interface implementation ✓
- `Entity.Height` is `{ get; init; }` — same ✓

**Update `Mover.ClampToBounds`** (`MonoGameLearning.Core/Entities/Components/Mover.cs:11`):

```csharp
public static void ClampToBounds(IReadOnlyEntity entity, RectangleF movementBounds)
{
    if (movementBounds.IsEmpty) return;
    float halfWidth = entity.Width / 2f;
    float halfHeight = entity.Height / 2f;
    entity.Position = new Vector2(
        MathHelper.Clamp(entity.Position.X, movementBounds.Left + halfWidth, movementBounds.Right - halfWidth),
        MathHelper.Clamp(entity.Position.Y, movementBounds.Top + halfHeight, movementBounds.Bottom - halfHeight)
    );
}
```

No body change — only the parameter type.

**Caller:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:205`

```csharp
Mover.ClampToBounds((Entity)movable, movable.MovementBounds);
```

Since `movable` is `IMoveableEntity` and is also an `Entity` (always, in practice), the cast `(Entity)` is redundant after this change. Update to:

```csharp
Mover.ClampToBounds(movable, movable.MovementBounds);
```

This works only if `IMoveableEntity` extends `IReadOnlyEntity`, OR if we accept that `movable` might not be `IReadOnlyEntity` (in which case the cast stays). Decision: keep the cast. The intent of the cast is documentation — `ClampToBounds` clamps the spatial position of an entity, not just any `IMoveableEntity`. Optionally change to `Mover.ClampToBounds((IReadOnlyEntity)movable, ...)` for clarity. **Implementation note:** make this choice during implementation; either is fine, prefer the explicit `IReadOnlyEntity` cast.

**Tests to slim** (`MonoGameLearning.Game.Tests/ActorCollisionTests.cs`):

- `TestActorEntity` (line 10): currently `Entity + ICollisionActor + IMoveableEntity` — could remove `Entity` and add `IReadOnlyEntity` impl, but then `Frame` and `Shape` must be hand-rolled. **Recommendation: leave `TestActorEntity` as-is** (it's already minimal: 8 lines including class body).
- `CollisionPushEntity` (line 20): same — leave as-is.
- New tests are unaffected; existing tests continue to work because `Entity` implements `IReadOnlyEntity`.

The marginal test-friction win here is: **any future test that wants to verify `ClampToBounds` math** can use a 3-line `IReadOnlyEntity` stub instead of subclassing `Entity`. Existing tests are not rewritten.

## Validation checklist

After each change:

1. `dotnet build` — must succeed with no new warnings.
2. `dotnet test` — all existing tests pass.
3. New test additions:
   - **`IReadOnlyEntity` contract**: a tiny stub class implementing `IReadOnlyEntity` (no inheritance), call `Mover.ClampToBounds` against it, verify position clamping.
   - **`HitResult.Target` is `IDamageable`**: assert `hits[0].Target is IDamageable` is always true when a hit is produced (regression guard against the `as` cast returning null silently).

## Files touched

| File | Lines | Action |
|---|---|---|
| `MonoGameLearning.Core/Combat/HitboxService.cs` | ~12 | `HitResult.Target` type, remove `Source`, dedup map key/value types, parameter type changes × 3 |
| `MonoGameLearning.Game/GameLoop/GameLoop.cs` | 1 (line 187) | Remove defensive cast; remove `(Entity)` cast on line 205 |
| `MonoGameLearning.Core/Entities/Interfaces/IReadOnlyEntity.cs` | new | 8-line interface |
| `MonoGameLearning.Core/Entities/Entity.cs` | 1 | Add interface to base class |
| `MonoGameLearning.Core/Entities/Components/Mover.cs` | 1 (line 11) | Parameter type |
| `MonoGameLearning.Game.Tests/HitboxTests.cs` | ~1 | Remove `hits[0].Source` assertion |
| `MonoGameLearning.Game.Tests/ActorCollisionTests.cs` | new file or new test | Add `IReadOnlyEntity` contract test |
| `MonoGameLearning.Game.Tests/EntityManagerRegistrationTests.cs` | maybe new test | Add `HitResult.Target` regression test |

## Risk assessment

- **GC impact**: zero. Interface dispatch on reference types already on the hot path. No new boxing, no new allocations.
- **Behavior change**: none expected. `IDamageable` and `IHitboxProvider` are already implemented by every concrete type that flows through these APIs.
- **Test churn**: minimal. One assertion removed in `HitboxTests.cs`; one optional test added.
- **Backwards compatibility**: none — this is a tightening, no callers are widened.

## Out of scope (explicit)

- Decoupling `LevelDirector`, `CameraController`, `EnemyEntity.Target` from `Entity`/`PlayerEntity`.
- Introducing `IPositioned` interface.
- Replacing `Entity` base class with composition.
- Any DI / IoC work.
- Reworking `EntityManager`'s typed lists.
