# Plan: Prop Drops (OilDrumEntity drops Food pickup on destroy, per-instance)

## Goal

When a `PropBase`-derived entity is destroyed, it spawns one or more pickups declared per-instance in the level data. The concrete example: most `OilDrumEntity`s in `Level1` do **not** drop anything; one specific drum drops a `Food` (heals 15 HP) pickup at its own position. Drops are 100% deterministic (no RNG). The mechanism is generic so any prop can declare drops without code changes.

## Decisions (resolved with user)

- **Locus:** Drops live on the prop via a new `IPickupDropper` interface (added to `PropBase`).
- **Per-instance data:** `PropSpawnDef` gains a `Drops` field (`IReadOnlyList<PickupSpawnDef>?`). `Level1.cs` declaratively says "this drum drops, this one doesn't". `PropSpawnDef` is the single source of truth.
- **Threading:** `PropBase` gets a settable `Drops` property (set during construction by `LevelDirector` from the def). Default is null/empty → no drops.
- **Interface:** `IPickupDropper.CreateDrops()` returns the per-instance `Drops` by default; subclasses can override to compute drops dynamically (out of scope for now).
- **Spawning:** Drops are `PickupSpawnDef`s. `LevelDirector.OnPropDestroyed` pipes them through the existing `LevelDirector.SpawnPickups(List<PickupSpawnDef>)` factory (which already maps `"Food"` → `FoodPickupEntity` and registers with `EntityManager`) — single source of truth.
- **Trigger:** `LevelDirector.OnPropDestroyed` (already subscribed to `PropBase.Destroyed`) checks for `IPickupDropper`, calls `CreateDrops()`, forwards to `SpawnPickups`.
- **Determinism:** No RNG.

## Files

### Create
- `MonoGameLearning.Core/Entities/Interfaces/IPickupDropper.cs` — new interface.
- `MonoGameLearning.Game.Tests/PropDropsOnDestroyTests.cs` — new test file.

### Modify
- `MonoGameLearning.Core/Levels/PropSpawnDef.cs` — add `Drops` field (positional record change with optional last param).
- `MonoGameLearning.Core/Entities/PropBase.cs` — add `IPickupDropper` to base interface list, add `Drops` property + `CreateDrops()` virtual method.
- `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs` — pass `prop.Drops` through to base; no behavior override needed (default `CreateDrops()` returns `Drops`).
- `MonoGameLearning.Game/Levels/LevelDirector.cs` — pass `prop.Drops` to `OilDrumEntity` constructor; in `OnPropDestroyed`, call `SpawnPickups(dropper.CreateDrops())` when non-empty.
- `MonoGameLearning.Game/Levels/Level1.cs` — add `Drops: [...]` to one drum (e.g., the standalone drum at x=1000) and leave the others at the default null.
- `ROADMAP.md` — update Milestone 7 "Drop Table" sub-bullet.

## Interface Signature

```csharp
// MonoGameLearning.Core/Entities/Interfaces/IPickupDropper.cs
namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IPickupDropper
{
    IReadOnlyList<PickupSpawnDef> CreateDrops();
}
```

`IReadOnlyList` (not `List`) — matches `IReadOnlyEntity`/`IReadOnlyList<T>` style in the project.

## PropSpawnDef Change

Current:
```csharp
public record PropSpawnDef(string Type, Vector2 Position, CollisionAnchor Anchor = CollisionAnchor.Top);
```

New (add `Drops` as optional last param to preserve source compatibility for existing call sites that omit it):
```csharp
public record PropSpawnDef(
    string Type,
    Vector2 Position,
    CollisionAnchor Anchor = CollisionAnchor.Top,
    IReadOnlyList<PickupSpawnDef>? Drops = null);
```

`IReadOnlyList<PickupSpawnDef>?` — null means "no drops" (default), non-null non-empty means drops.

## PropBase Changes

Add `using MonoGameLearning.Core.Levels;` to `PropBase.cs` for `PickupSpawnDef`.

Add `IPickupDropper` to base class interface list:
```csharp
public abstract class PropBase(...) : Entity(...), IRenderable, IDebugDrawable,
    ICollisionActor, IDamageable, IPickupDropper
```

Add a settable `Drops` property and virtual `CreateDrops()`:
```csharp
public IReadOnlyList<PickupSpawnDef>? Drops { get; set; }

public virtual IReadOnlyList<PickupSpawnDef> CreateDrops() => Drops ?? [];
```

The setter is `set;` (not `init;`) because `LevelDirector.SpawnProps` may need to set it post-construction if the prop type doesn't expose a constructor parameter (see wiring section). Alternatively, restrict the setter to a `protected set` and have `LevelDirector` use a `PropBase` overload constructor; for simplicity, public `set` is fine — no external code mutates it after spawn.

**Alternative considered:** Pass `Drops` as a new constructor parameter to `PropBase` (and `OilDrumEntity`). Rejected: `PropBase` primary constructor already has 5 params; adding a 6th `IReadOnlyList` makes the call site noisy. Settable property is cleaner and only `LevelDirector` writes it.

## OilDrumEntity Changes

Add `using MonoGameLearning.Core.Levels;` to `OilDrumEntity.cs`.

`OilDrumEntity` does **not** need to override `CreateDrops()` — the base virtual already returns `Drops ?? []`. If a future `OilDrum` variant wants static drops regardless of the def, it can override.

No changes to `TakeDamage` / `OnDestroyed` flow.

## LevelDirector Wiring

### SpawnProps

Current signature:
```csharp
var drum = new OilDrumEntity(prop.Type, prop.Position, 1.0f, OilDrumSprite.Create(), _audio, anchor: prop.Anchor);
```

The constructor doesn't take `Drops`. Two options:
- **(A) Add `Drops` parameter to `OilDrumEntity` constructor and pass to base.** Cleanest. Requires updating the existing test stub `OilDrumEntity` ctors (1 site in `LevelDirector`, plus test doubles if any).
- **(B) Set `drum.Drops = prop.Drops` after construction.** Uses the settable property. Simpler diff.

**Chosen: (B)** — minimal diff, doesn't ripple into test doubles. The `Drops` property is public-set; only `LevelDirector` writes it.

```csharp
public void SpawnProps(List<PropSpawnDef> propDefs)
{
    foreach (var prop in propDefs)
    {
        var drum = new OilDrumEntity(prop.Type, prop.Position, 1.0f, OilDrumSprite.Create(), _audio, anchor: prop.Anchor);
        drum.Drops = prop.Drops;                       // NEW
        drum.Destroyed += OnPropDestroyed;
        _entityManager.Register(drum);
    }
}
```

### OnPropDestroyed

Current:
```csharp
private void OnPropDestroyed(Entity prop)
{
    if (prop is OilDrumEntity oilDrum)
        oilDrum.Destroyed -= OnPropDestroyed;
    _entityManager.Destroy(prop);
}
```

New:
```csharp
private void OnPropDestroyed(Entity prop)
{
    if (prop is OilDrumEntity oilDrum)
        oilDrum.Destroyed -= OnPropDestroyed;
    _entityManager.Destroy(prop);

    if (prop is IPickupDropper dropper)
    {
        var drops = dropper.CreateDrops();
        if (drops.Count > 0)
            SpawnPickups(drops);
    }
}
```

Capture `drops` once in a local — never call `CreateDrops()` twice (subclass override might do work).

**Order rationale:** the prop is queued for destroy first, then drops are spawned in the same frame. `EntityManager.Destroy` only adds to `_pendingDestroy`; `Register` adds directly to `_all`. So the new pickup is registered before `ProcessPending` clears the drum. Verified against `EntityManager.cs:72-91`.

## Level1.cs Change

Currently all 6 drums have no `Drops`. Add a drop to one specific drum (e.g., the standalone `x=1000` drum), leave the rest default-null:

```csharp
public override List<PropSpawnDef> Props =>
[
    new("OilDrum", new Vector2(200, 560), Anchor: CollisionAnchor.Bottom),
    new("OilDrum", new Vector2(400, 560), Anchor: CollisionAnchor.Bottom),
    new("OilDrum", new Vector2(600, 560), Anchor: CollisionAnchor.Bottom),
    new("OilDrum", new Vector2(800, 460)),
    new("OilDrum", new Vector2(1000, 460), Drops:
    [
        new PickupSpawnDef("Food", new Vector2(1000f, 560f)),
    ]),
    new("OilDrum", new Vector2(1200, 460)),
];
```

Note: the drop spawn position is **not** necessarily the drum's `Position` — it's whatever `PickupSpawnDef.Position` says. So the drop can be offset (e.g., sit on the ground below the drum). The plan uses `y=560f` so the food sits on the ground line. This demonstrates that `PickupSpawnDef` is the source of truth for drop position; future props can drop at different positions without code changes.

## ROADMAP Update (Milestone 7.2 — "Drop Table" bullet)

Edit `ROADMAP.md` line 104. Replace the existing "- [ ] **Drop Table**: ..." bullet with the following:

```markdown
- [x] **Prop Drops**: When an `IDamageable` prop is destroyed, spawn any pickups declared on its `PropSpawnDef.Drops`. *(Implemented: `IPickupDropper` interface on `PropBase`, `PropSpawnDef.Drops` field, `LevelDirector.OnPropDestroyed` pipes drops through `SpawnPickups`. `Level1` declares one `OilDrumEntity` at x=1000 drops a `Food` pickup on destroy.)* **Enemy drops remain `[ ]`** — separate work, blocked on enemy death-event drop hookup.
```

Implementation note for the implementer:
- Use the exact bullet text above (including the backticks, the `[x]` checkbox flip, and the parenthetical implementation summary).
- Do **not** edit any other bullets in Milestone 7 — only bullet 7.2 (the "Drop Table" bullet) changes.
- Do **not** edit the milestone header (Milestone 7 stays `[ ]` because weapons/items/animations still have unchecked bullets).
- The parenthetical references `IPickupDropper`, `PropSpawnDef.Drops`, `LevelDirector.OnPropDestroyed`, `SpawnPickups`, and `Level1` — all of which exist after this plan's implementation lands.

## Tests (new file)

`MonoGameLearning.Game.Tests/PropDropsOnDestroyTests.cs`:

1. **`OilDrum_WithDrops_Destroyed_SpawnsFoodPickupAtConfiguredPosition`** — construct a drum with `Drops = [PickupSpawnDef("Food", (500, 560))]`, drive to 0 HP, assert one `FoodPickupEntity` registered in `EntityManager` at `(500, 560)`.
2. **`OilDrum_WithoutDrops_Destroyed_SpawnsNothing`** — construct a drum with `Drops = null`, destroy, assert no new pickups in `EntityManager`.
3. **`OilDrum_WithEmptyDropsList_Destroyed_SpawnsNothing`** — `Drops = []` (non-null but empty) → no spawn.
4. **`CreateDrops_ReturnsConfiguredList`** — unit test the `IPickupDropper` contract directly without destroying.
5. **`PropBase_DefaultCreateDrops_ReturnsEmpty`** — concrete subclass without `Drops` set returns `[]`.

Use existing test infrastructure (`TestLevel`, `TestLevelDirector`, `TestEntityManager` patterns from `LevelDirectorTests.cs`).

## Validation

1. `dotnet build` — 0 errors, 0 warnings.
2. `dotnet test` — all 389 existing tests pass + 5 new tests.
3. Manual smoke (`IsDebug` on): destroy the 1000-x drum → Food pickup appears on the ground. Destroy other drums → nothing spawns.
4. Existing `LevelDirectorPickupSpawnTests` still pass — `SpawnPickups` factory unchanged.

## Risks & Notes

- **`PropSpawnDef` is a record with positional params.** Adding `Drops = null` as the last param is source-compatible (existing callers that omit it still compile).
- **`Drops` property is public-set** — acceptable here because the only writer is `LevelDirector`, and `PropBase` already has other public-settable state (`Faction`, `MovementBounds`, etc.). Not a leak.
- **No double-subscribe on Destroyed** — `OnPropDestroyed` only calls `Destroyed -= ...` once per drum (existing behavior), and the new drop path is outside the unsubscribe branch.
- **Future RNG:** Interface shape supports a future subclass that overrides `CreateDrops()` to return a randomized subset of `Drops`.
- **Multiple drops per prop:** `IReadOnlyList` allows N pickups; tested only for 1 in the smoke test.

## Out of Scope

- Drop tables, rarity, RNG.
- Enemy death drops (Milestone 7 follow-up).
- Pickup animation/fade-out (existing TODO item).
- New pickup types beyond `Food` (factory unchanged).
- Constructor-passing `Drops` through the `PropBase` ctor (rejected for diff size).