# Plan: Drop System Follow-ups (TODO.md items 1, 3, 4; item 2 deferred)

## Goal

Resolve the four "Drop System Follow-ups" in `TODO.md`:

1. **Item 1 (do):** Strongly type `PropBase.Destroyed` as `Action<PropBase>`; `LevelDirector.OnPropDestroyed(PropBase)`; `StubPropDropperEntity` extends `PropBase` via a new sprite-less constructor overload.
2. **Item 2 (defer):** Renaming `PickupSpawnDef` → `DropDef` stays in TODO.md, gated on weapon drops (weapon entities don't exist yet — Milestone 7 unchecked).
3. **Item 3 (do):** Wire enemy drops — `EnemySpawnDef.Drops` → `LevelDirector.SpawnWave` → `EnemyEntity` (implements `IPickupDropper`) → `OnEnemyDied` spawns aligned drops. One Level1 Grunt drops Food as a smoke-test target.
4. **Item 4 (reject):** Do **not** merge `Level.Pickups` into a generic level-item list. Investigation conclusion recorded below; TODO item removed.

## Decisions (resolved with user)

- **Item 4 rejected.** `Level.Pickups` (trigger: level start) and `PropSpawnDef.Drops` / `EnemySpawnDef.Drops` (trigger: on-destroy) share only the `(Type, Position)` DTO shape, not semantics. A unified `Level.Items` would need a union def type (`PropSpawnDef` has `Anchor`+`Drops`; `PickupSpawnDef` doesn't) — more machinery to save one line in `GameLoop`. `LevelDirector.CreatePickup` is already the single spawning source of truth.
- **Director owns drop alignment.** Today alignment is split: `PropBase.CreateDrops()` rewrites def X to `Frame.Center.X` (allocating a copied array) and `LevelDirector.SpawnPickupAligned` rewrites Y. Move all alignment into one private `LevelDirector.SpawnDrops(IPickupDropper, Entity)` that places pickups at `source.Frame.Center.X` / bottom-aligned on `source.Frame.Bottom`. `PropBase.CreateDrops()` simplifies to `Drops ?? []` (no copy — small alloc win). Consequence: `PickupSpawnDef.Position` inside a `Drops` list is ignored (already de-facto true; `Level1` passes `default`). This also makes `PropBase.CreateDrops()` semantically identical to the existing stub's, so the 4 `CreateDrops_*` tests keep passing unchanged after item 1.
- **Item 1 mechanism:** C# forbids a secondary constructor that doesn't chain to the primary one, and the current primary ctor derives width/height from `sprite.Size` — so `PropBase` converts from primary constructor to two explicit constructors (behavior-preserving; `OilDrumEntity`'s `base(...)` call is syntactically unchanged). `SpriteRenderer` tolerates null sprites (proven by `TestEnemyEntity`).
- **Pooling correctness:** enemies are pooled. `EnemyEntity.Reset` clears `Drops` (called on every `Rent` via `OnRentEnemy`); `SpawnWave` sets `enemy.Drops = def.Drops` **after** `Rent`. `OnEnemyDied` spawns drops **before** `EnemyPool.Return` (Return sets the -99999 sentinel position).
- **`EnemySpawnDef` split:** it currently lives inside `WaveDef.cs`, violating the AGENTS.md one-type-per-file rule. Move it to its own file while adding the `Drops` field.

## Task 1 — Director-owned alignment (prerequisite refactor)

**`MonoGameLearning.Game/Levels/LevelDirector.cs`:**

- Replace `SpawnPickupAligned(PickupSpawnDef, float)` with:

```csharp
private void SpawnDrops(IPickupDropper dropper, Entity source)
{
    foreach (var def in dropper.CreateDrops())
    {
        var pickup = CreatePickup(def);
        pickup.Position = new Vector2(source.Frame.Center.X, source.Frame.Bottom - pickup.Height / 2f);
        _entityManager.Register(pickup);
    }
}
```

- `OnPropDestroyed` body becomes: unsubscribe, `SpawnDrops(p, p)`, `_entityManager.Destroy(prop)` (signature change happens in Task 2; keep `Entity` + `is PropBase` cast for this task).
- Add `using MonoGameLearning.Core.Entities.Interfaces;`.

**`MonoGameLearning.Core/Entities/PropBase.cs`:**

- `CreateDrops()` simplifies to:

```csharp
public IReadOnlyList<PickupSpawnDef> CreateDrops() => Drops ?? [];
```

Removes the per-destroy `PickupSpawnDef[]` copy and the `with`-expression rewriting.

- Existing tests must still pass (stub overrides `CreateDrops`, so it is unaffected at this step).

## Task 2 — Item 1: strongly-typed `Destroyed` + sprite-less `PropBase` ctor

**`MonoGameLearning.Core/Entities/PropBase.cs`** — convert primary ctor to two explicit ctors; move field initializers that referenced ctor params into ctor bodies:

```csharp
public abstract class PropBase : Entity, IRenderable, IDebugDrawable, ICollisionActor, IDamageable, IPickupDropper
{
    protected PropBase(string name, Vector2 position, AnimatedSprite sprite, float scale, int maxHealth, CollisionAnchor anchor)
        : base(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale))
    {
        Anchor = anchor;
        SpriteRenderer = new(sprite, scale);
        HealthComponent = new(maxHealth);
    }

    // Sprite-less overload for test doubles — SpriteRenderer gets null! sprite (Render/DrawDebug already guard).
    protected PropBase(string name, Vector2 position, int width, int height, int maxHealth, CollisionAnchor anchor)
        : base(name, position, width, height)
    {
        Anchor = anchor;
        SpriteRenderer = new(null!, 1f);
        HealthComponent = new(maxHealth);
    }

    public CollisionAnchor Anchor { get; }
    public event Action<PropBase> Destroyed = null!;   // was Action<Entity>
    // ...everything else unchanged...
}
```

- `OnDestroyed()` already invokes `Destroyed?.Invoke(this)` — now typed `PropBase`. `Died` event untouched.
- Verify no other subscribers to `Destroyed` exist (only `LevelDirector.SpawnProps`).

**`MonoGameLearning.Game/Levels/LevelDirector.cs`:**

```csharp
protected void OnPropDestroyed(PropBase prop)
{
    prop.Destroyed -= OnPropDestroyed;
    SpawnDrops(prop, prop);
    _entityManager.Destroy(prop);
}
```

`SpawnProps`'s `drum.Destroyed += OnPropDestroyed;` still compiles (method group matches `Action<PropBase>`).

**`MonoGameLearning.Game.Tests/LevelDirectorTests.cs`:**

- `TestLevelDirector.SimulatePropDestroyed(Entity prop)` → `SimulatePropDestroyed(PropBase prop)`.

**`MonoGameLearning.Game.Tests/PropDropsOnDestroyTests.cs`** — slim the stub to a real `PropBase`:

```csharp
internal sealed class StubPropDropperEntity(string name, Vector2 position, int width, int height)
    : PropBase(name, position, width, height, maxHealth: 1, CollisionAnchor.Top)
{
    public override void TakeDamage(DamageInfo info) => OnDestroyed();
    public void FireDestroyed() => OnDestroyed();
}
```

- Delete the stub's own `Drops`, `CreateDrops()`, `Destroyed`, `Shape`, `ICollisionActor`/`IPickupDropper` declarations — all inherited. (`PropBase.Shape` works for `mgr.Register`.)
- Add `using MonoGameLearning.Core.Combat;` (for `DamageInfo`).
- All 6 existing tests in this file pass **unchanged** — the `CreateDrops_*` tests assert raw positions, which now matches real `PropBase` behavior (Task 1).

## Task 3 — Item 3: enemy drops

**`MonoGameLearning.Core/Levels/EnemySpawnDef.cs`** (new file — split out of `WaveDef.cs`):

```csharp
using System.Collections.Generic;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Levels;

public record EnemySpawnDef(string Type, SpawnSide Side, SpawnVertical Vertical, IReadOnlyList<PickupSpawnDef>? Drops = null);
```

- `WaveDef.cs` keeps only `WaveDef`. Optional trailing param = all existing 3-arg call sites compile unchanged.

**`MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs`:**

- Add `IPickupDropper` to the class declaration; add `using MonoGameLearning.Core.Entities.Interfaces;`.

```csharp
public IReadOnlyList<PickupSpawnDef>? Drops { get; set; }
public IReadOnlyList<PickupSpawnDef> CreateDrops() => Drops ?? [];
```

- In `Reset(Vector2 position, Entity target)`: add `Drops = null;` — prevents stale drops leaking across pool rentals.

**`MonoGameLearning.Game/Levels/LevelDirector.cs`:**

- In `SpawnWave`, immediately after `var enemy = EnemyPool.Rent(...)` (Rent → Reset clears Drops, so assignment must follow Rent):

```csharp
enemy.Drops = def.Drops;
```

- In `OnEnemyDied`, spawn drops **before** `EnemyPool.Return` (Return sets sentinel position):

```csharp
protected virtual void OnEnemyDied(object sender, EventArgs e)
{
    if (sender is not EnemyEntity enemy) return;
    enemy.Died -= OnEnemyDied;
    _activeEnemies.Remove(enemy);
    SpawnDrops(enemy, enemy);   // before Return — position is still real
    EnemyPool.Return(enemy);
}
```

- `TestLevelDirector.OnEnemyDied` override calls `base` — inherits drop behavior for free.
- `CreatePickup` switch unchanged (`"Food"` only). Weapon cases (`"Knife"`, `"Bat"`) land with the weapon entities — tracked by retained TODO item 2.

**`MonoGameLearning.Game/Levels/Level1.cs`** — one demonstrative enemy drop (wave 2, first Grunt):

```csharp
new WaveDef(TriggerX: 1600f, EndX: 2000f, Enemies:
[
    new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Top, Drops:
    [
        new PickupSpawnDef("Food", default),
    ]),
    new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Bottom),
])
```

## Task 4 — Tests (new file)

**`MonoGameLearning.Game.Tests/EnemyDropsOnDeathTests.cs`** — reuse `CreateTestWorld`/`TestLevel`/`TestLevelDirector` patterns from `PropDropsOnDestroyTests.cs` (world must include the `"pickups"` layer):

1. `SpawnWave_EnemyDefWithDrops_AssignsDropsToRentedEnemy` — `TestLevel` wave with one `EnemySpawnDef(..., Drops: [Food])`; move player to trigger; `Update`; assert `_director.SpawnedEnemies[0]` cast to `EnemyEntity` has `Drops` count 1.
2. `OnEnemyDied_WithDrops_SpawnsFoodAtEnemyFeet` — same setup; capture `var frame = enemy.Frame;` **before** `SimulateEnemyDied(enemy)` (position becomes sentinel after); assert exactly one `FoodPickupEntity` in `mgr.All` with `Position.X == frame.Center.X` and `Frame.Bottom == frame.Bottom` (bottom-aligned invariant; avoids hardcoding pickup height).
3. `OnEnemyDied_WithoutDrops_SpawnsNothing` — def without Drops; `SimulateEnemyDied`; assert no `FoodPickupEntity` in `mgr.All`.
4. `Reset_ClearsDrops_PreventingStalePoolDrops` — construct `TestEnemyEntity`, set `Drops = [Food]`, call `Reset(Vector2.Zero, player)`, assert `Drops` is null. (Pooling regression guard — AGENTS.md critical failure mode.)

Item 1 needs no new tests — the 6 existing `PropDropsOnDestroyTests` cover the behavior through the now-real `PropBase` path.

## Task 5 — Docs

**`TODO.md`:** remove follow-up items 1, 3, 4. Keep item 2 (rename), renumbered to 1, text unchanged (still gated on weapon drops).

**`ROADMAP.md`** (Milestone 7, "Prop Drops" bullet, line ~104): replace the trailing `**Enemy drops remain `[ ]`** — separate work, blocked on enemy death-event drop hookup.` with:

```markdown
**Enemy drops `[x]`**: `EnemySpawnDef.Drops` wired through `LevelDirector.SpawnWave` → `OnEnemyDied` (spawns before pool return) through the shared aligned-drop path; `EnemyEntity` implements `IPickupDropper`; `Reset` clears `Drops` per rental. One Level1 wave-2 Grunt drops Food.
```

## Validation

1. `dotnet build` — 0 errors, 0 warnings.
2. `dotnet test` — all existing tests pass (incl. unchanged `PropDropsOnDestroyTests`, `LevelDirectorTests`, `LevelDirectorPickupSpawnTests`, `EnemyPoolTests`) + 4 new `EnemyDropsOnDeathTests`.
3. Manual smoke (`IsDebug` on):
   - Destroy the x=1000 drum → Food still lands at its feet (regression on the moved alignment path).
   - Kill the wave-2 top Grunt → Food appears at its death position; kill the other Grunt → nothing.
   - Clear both waves → no stale drops from re-rented pooled enemies.

## Risks & Notes

- **PropBase ctor conversion** is mechanical but touches every field initializer that referenced primary-ctor params (`Anchor`, `SpriteRenderer`, `HealthComponent`). `CollisionHeightFraction`, `CollisionBounds`, `Shape`, `Render`, `DrawDebug`, `TakeDamage`, `OnDestroyed` are unaffected.
- **Order sensitivity in `OnEnemyDied`** is the single most important invariant: `SpawnDrops` before `EnemyPool.Return`. The test captures the frame pre-death to lock this in.
- **`enemy.Drops = def.Drops` after Rent** is equally order-sensitive (Rent → Reset clears). A code comment at both sites is warranted.
- **No new debug drawing:** spawned drops are ordinary pickups, already visible via their own rendering; nothing drop-specific to draw.
- **GC:** drop spawning is death-time only (not per-frame); Task 1 removes the `PropBase` array copy. `Drops ?? []` allocates nothing.
- **`FireDestroyed()`** on the slimmed stub: keep for API compat (fires the real inherited event); implementer should grep for usages and drop it only if provably unused.

## Out of Scope

- Renaming `PickupSpawnDef` → `DropDef`/`ItemSpawnDef` (TODO item 2 — deferred to weapon-drops work).
- Weapon drop types in the `CreatePickup` switch (blocked on weapon entities, Milestone 7).
- Merging `Level.Pickups` with prop/enemy drops (TODO item 4 — investigated and rejected).
- Drop tables, rarity, RNG; pickup animation/fade-out.
- Changing `Level.Pickups` / `SpawnPickups` member names.
