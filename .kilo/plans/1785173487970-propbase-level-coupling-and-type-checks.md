# Plan: Remove PropBase → Core.Levels coupling and collapse two type checks in LevelDirector.OnPropDestroyed

## Smells (user-flagged)

1. **PropBase → Core.Levels inverted dependency**
   `MonoGameLearning.Core/Entities/PropBase.cs:11` imports `MonoGameLearning.Core.Levels` solely to expose `Drops` and `CreateDrops()` (typed `IReadOnlyList<PickupSpawnDef>?`). A reusable runtime entity base class now depends on level-authoring types.
   `IPickupDropper` (`MonoGameLearning.Core/Entities/Interfaces/IPickupDropper.cs:2`) carries the same cross-namespace reference.

2. **Two type checks in `LevelDirector.OnPropDestroyed`** (`MonoGameLearning.Game/Levels/LevelDirector.cs:84`)
   - `if (prop is OilDrumEntity oilDrum)` — needed only to reach `Destroyed` because the event is declared on `PropBase` and the parameter is `Entity`. The cast is structurally redundant: `Destroyed` only fires from `PropBase.OnDestroyed()`.
   - `if (prop is IPickupDropper dropper)` — redundant: `PropBase` already implements `IPickupDropper`, so any `PropBase` reaching this handler can call `CreateDrops()` directly. (Kept on `PropBase` for the planned future enemy-drop system.)

## Decisions

- **Keep `IPickupDropper` interface** for the planned future enemy-drop wiring (user-confirmed: enemies may drop weapons). Drop the interface check from `OnPropDestroyed`; the single `PropBase` cast subsumes it.
- **Move `PickupSpawnDef` into `MonoGameLearning.Core.Entities`** alongside `PickupBase` and `IPickup`. `Core.Levels` already references `Core.Entities` (for `CollisionAnchor`, `Entity`); the reverse edge goes away.
- **Do NOT strongly type `Destroyed` as `Action<PropBase>`** in this pass. Doing so forces every test stub that simulates prop destruction to extend `PropBase`, which requires either a non-sprite constructor overload or `FormatterServices` hacks — code-additive for a marginal typing benefit. The cast collapse below delivers the smell reduction without that cost. Defer the stronger typing if a future need emerges (e.g., the enemy-drop work).
- **Do NOT rename `PickupSpawnDef`** in this pass. Its `(Type, Position)` shape is generic enough for weapons and other droppables; renaming touches `Level.Pickups`, `PropSpawnDef.Drops`, and every test fixture. Defer until the weapon-drop work surfaces a clearer contract.
- **Keep the existing public API of `OnPropDestroyed(Entity)` and `TestLevelDirector.SimulatePropDestroyed(Entity)`** so existing Entity-based test stubs continue to work.

## Approach (single cast)

```csharp
// MonoGameLearning.Game/Levels/LevelDirector.cs
protected void OnPropDestroyed(Entity prop)
{
    if (prop is PropBase p)
    {
        p.Destroyed -= OnPropDestroyed;     // no OilDrumEntity cast
        var drops = p.CreateDrops();        // no IPickupDropper cast — PropBase implements it
        if (drops.Count > 0)
            SpawnPickups(drops);
    }
    _entityManager.Destroy(prop);            // always remove from manager (matches existing tests)
}
```

- One cast (`is PropBase p`) replaces the two.
- `_entityManager.Destroy(prop)` runs unconditionally so the existing `OnPropDestroyed_WithoutDrops_DoesNotSpawn` and `OnPropDestroyed_WithEmptyDropsList_DoesNotSpawn` tests still pass with the Entity-based `StubPropDropperEntity`.
- `CreateDrops()` is invoked directly on `PropBase` — interface kept, interface check gone.
- `_entityManager.Destroy(prop)` accepts `Entity` (current signature); no signature changes ripple into tests.

## File changes (ordered)

1. **Move** `MonoGameLearning.Core/Levels/PickupSpawnDef.cs` → `MonoGameLearning.Core/Entities/PickupSpawnDef.cs`
   - Same record body: `public record PickupSpawnDef(string Type, Vector2 Position);`
   - Namespace: `MonoGameLearning.Core.Entities` (was `MonoGameLearning.Core.Levels`).
   - Drop the `using MonoGameLearning.Core.Entities;` line that was already present (now redundant).

2. **Edit** `MonoGameLearning.Core/Entities/Interfaces/IPickupDropper.cs`
   - Remove `using MonoGameLearning.Core.Levels;` — `PickupSpawnDef` is now in the same namespace.

3. **Edit** `MonoGameLearning.Core/Entities/PropBase.cs`
   - Remove `using MonoGameLearning.Core.Levels;` — no other reference needed; `PickupSpawnDef` is now in `Core.Entities`.

4. **Edit** `MonoGameLearning.Core/Levels/Level.cs`
   - `PickupSpawnDef` now lives in `MonoGameLearning.Core.Entities`. `Level.cs` already accesses `CollisionAnchor` from that namespace via the `PropSpawnDef` import. The `PropSpawnDef` import (or equivalent) makes `PickupSpawnDef` resolvable here too. Verify the using list covers it; add `using MonoGameLearning.Core.Entities;` if not present.

5. **Edit** `MonoGameLearning.Game/Levels/Level1.cs`
   - Verify `using MonoGameLearning.Core.Entities;` is present (it is — needed for `Entity`). `PickupSpawnDef` resolves from there.

6. **Edit** `MonoGameLearning.Game/Levels/LevelDirector.cs`
   - Replace `OnPropDestroyed(Entity prop)` body as shown above. No signature change.

7. **Tests** — no edits required.
   - `MonoGameLearning.Game.Tests/PropDropsOnDestroyTests.cs` (StubPropDropperEntity, CreateDrops_*): unaffected — `PickupSpawnDef` is now imported from `Core.Entities`; the file already has `using MonoGameLearning.Core.Entities;`. The two `OnPropDestroyed_*` tests continue to pass because `_entityManager.Destroy(prop)` runs unconditionally.
   - `MonoGameLearning.Game.Tests/LevelDirectorTests.cs` (`TestLevelDirector.SimulatePropDestroyed(Entity)`): signature unchanged.
   - `MonoGameLearning.Game.Tests/LevelDirectorPickupSpawnTests.cs` (`SpawnPickups`): unaffected.

## Validation

1. `dotnet build` — 0 errors, 0 warnings.
2. `dotnet test` — all existing tests pass (current count: ~394 tests including the 6 in `PropDropsOnDestroyTests`).
3. Manual smoke (`IsDebug` on): destroy the OilDrum at `x=1000` → Food pickup appears on the ground. Destroy other drums → nothing spawns. No subscription-leak warning after destroying the same drum repeatedly.

## Risks & Notes

- **Cross-namespace move touches many files** but each is a one-line using-statement change. Mechanical.
- **`_entityManager.Destroy(prop)` always running** is a deliberate test-compat choice; if the event is ever fired by something other than a registered `PropBase` in production, the entity will still be removed — which is the original pre-refactor behavior.
- **`PickupSpawnDef` naming** is now slightly misleading if weapons are ever added to the drop pool. Acceptable for now: the field is `(Type, Position)` and the type string is dispatched in `SpawnPickups`'s switch.

## Follow-up: TODO.md additions

Append these bullets to `TODO.md` (file currently holds free-form numbered items — append a new "Drop System Follow-ups" section so they're easy to find later):

```markdown
1. Strongly type `PropBase.Destroyed` as `Action<PropBase>` and change `LevelDirector.OnPropDestroyed(PropBase)`. Update `StubPropDropperEntity` (and any other Entity-based test stubs that simulate prop destruction) to extend `PropBase` via a sprite-less constructor overload on `PropBase`.
2. Rename `PickupSpawnDef` → `DropDef` (or `ItemSpawnDef`) when weapon drops land. Update `Level.Pickups`, `PropSpawnDef.Drops`, `LevelDirector.SpawnPickups` switch, and all test fixtures.
3. Wire enemy drops: in `LevelDirector.SpawnWave`, set `enemy.Drops` per `EnemySpawnDef` (add a `Drops` field) and call `CreateDrops()` from `OnEnemyDied`, forwarding through `SpawnPickups`. Extend `SpawnPickups`'s switch to include weapon types (`"Knife"`, `"Bat"`, etc.) when those entities exist.
4. Investigate whether `Level.Pickups` (level-start pickups) should merge into a generic level-item-spawn list now that prop drops share the same DTO shape — eliminates the parallel `Level.Pickups` and `LevelDirector.SpawnPickups(IReadOnlyList<PickupSpawnDef>)` initial-spawn path.
```

These are explicitly out of scope for this refactor; the implementer adds them to `TODO.md` and stops.

## Out of scope

- Strongly typing `Destroyed` as `Action<PropBase>` — tracked in TODO.md (item 1).
- Renaming `PickupSpawnDef` → `DropDef` / `ItemSpawnDef` — tracked in TODO.md (item 2).
- Enemy-drop wiring — tracked in TODO.md (item 3).
- Consolidating `Level.Pickups` with prop drops — tracked in TODO.md (item 4).
- Adding new tests — the existing `PropDropsOnDestroyTests` cover the `OnPropDestroyed` branch via `StubPropDropperEntity` and continue to pass without modification.
