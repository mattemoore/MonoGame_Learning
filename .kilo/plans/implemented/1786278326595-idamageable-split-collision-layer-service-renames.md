# Plan: Core/Entities feature reorganization, IDamageable split, ICollisionLayer, Service rename

A combined, structural-only refactor (no gameplay behavior changes) with a unified goal: organize the
codebase by **feature/capability** first, then clean up the interface/contract smells and unify the
`Service`/`Manager`/`Controller` taxonomy. Phases are ordered so each is independently buildable and
testable. **Part I runs first** and must land before Parts II–IV (they operate on the reorganized
paths/namespaces).

## Decisions (user-confirmed)

- **Part I — feature-first reorganization of `Core`.** Group every capability's pieces together
  (components/DTOs/enums/helpers/interfaces) in one folder; dissolve `Entities/Interfaces/` and
  `Entities/Components/`; **new C# namespaces per folder**.
- **Part II — `IDamageable` split** into `IDamageable` (public contract) + `IDamageResponse`
  (combat-facing). `CombatService.ApplyDamage(IDamageResponse, ...)`.
- **Part III — `ICollisionLayer` + EntityManager collapse** to a layer-keyed collision store.
- **Part IV — naming unification on `*Service`**: `InputManager→InputService`,
  `AudioManager→AudioService`, `EntityManager→EntityService`, `MenuManager→MenuService`,
  `GumManager→GumUiService` (avoids Gum engine `GumService` collision), `CameraController→CameraService`,
  `GameStateController→GameStateService`. `EnemyPool` stays.

---

## PART I — Feature-first reorganization (runs first)

### I.1 Target tree (`Core/`)

Reuse `Combat/` and `Rendering/`; create `AI/`, `Movement/`, `Animation/` and entity-kind subfolders.

#### Namespace `MonoGameLearning.Core` (root)

- `Core/Entities/` → `.Entities`: `Entity` (root base), `EntityManager` (renamed in Part IV),
  `IReadOnlyEntity`, `IUpdatable`, (+ new `ICollisionLayer` in Part III)

#### Entity-kind folders (under `Core/Entities/`)

- `Core/Entities/Actor/` → `.Entities.Actor`: `CombatActorBase`, `CombatActorCallbacks`
- `Core/Entities/Prop/` → `.Entities.Prop`: `PropBase`, `CollisionAnchor`
- `Core/Entities/Pickup/` → `.Entities.Pickup`: `PickupBase`, `PickupSpawnDef`, `IPickup`, `IPickupDropper`
- `Core/Entities/Trigger/` → `.Entities.Trigger`: `TriggerEntity` (currently unreferenced — kept)

#### Feature folders (top-level, alongside existing)

- `Core/Combat/` → `.Combat` (exists): += `Health` (was Components), `IDamageable` (was Interfaces),
  (+ new `IDamageResponse` in Part II). Existing: `CombatService`, `HitboxService`, `DamageInfo`,
  `Faction`, `MoveData`, `IHitboxProvider`, `HitResult`, `HitboxData`. (The latter two were split out
  of `HitboxService.cs` in the working tree — already here, so Part I only needs to track them;
  `HitResult.cs:2` uses `MonoGameLearning.Core.Entities.Interfaces` for `IDamageable`, which Part I
  moves into `Core.Combat`, so that `using` drops off once `IDamageable.cs` lands in `Combat/`.)
- `Core/AI/` → `.AI` (new): `EnemyAI`, `AIAction`, `DominantForce`, `ActorSnapshot`, `WorldSnapshot`
  (all AI together)
- `Core/Movement/` → `.Movement` (new): `Mover`, `FacingDirection`, `IMoveableEntity`
- `Core/Animation/` → `.Animation` (new): `AnimationFrameTracker`, `IAnimated`
- `Core/Rendering/` → `.Rendering` (exists): += `IRenderable`, `IScreenRenderable`, `IDebugDrawable`
  (existing: `RenderContext`, `DebugDrawContext`, `BackgroundRenderer`, `SpriteRenderer`)

#### Deleted: `Entities/Interfaces/`, `Entities/Components/`

Emptied; every file moves to a feature.

### I.2 File moves

| Current location | Move to / namespace |
| --- | --- |
| `Entities/CombatActorBase.cs`, `Entities/CombatActorCallbacks.cs` | `Entities/Actor/*` → `.Entities.Actor` |
| `Entities/PropBase.cs`, `Entities/CollisionAnchor.cs` | `Entities/Prop/*` → `.Entities.Prop` |
| `Entities/PickupBase.cs`, `Entities/PickupSpawnDef.cs` | `Entities/Pickup/*` → `.Entities.Pickup` |
| `Entities/TriggerEntity.cs` | `Entities/Trigger/*` → `.Entities.Trigger` |
| `Entities/FacingDirection.cs`, `Components/Mover.cs` | `Movement/*` → `.Movement` |
| `Components/EnemyAI.cs`, `AIAction.cs`, `DominantForce.cs`, `ActorSnapshot.cs`, `WorldSnapshot.cs` | `AI/*` → `.AI` |
| `Components/AnimationFrameTracker.cs` | `Animation/*` → `.Animation` |
| `Components/Health.cs` | `Combat/Health.cs` → `.Combat` |
| `Interfaces/IDamageable.cs` | `Combat/IDamageable.cs` → `.Combat` |
| `Interfaces/IMoveableEntity.cs` | `Movement/IMoveableEntity.cs` → `.Movement` |
| `Interfaces/IAnimated.cs` | `Animation/IAnimated.cs` → `.Animation` |
| `Interfaces/IRenderable.cs`, `IScreenRenderable.cs`, `IDebugDrawable.cs` | `Rendering/*` → `.Rendering` |
| `Interfaces/IPickup.cs`, `IPickupDropper.cs` | `Entities/Pickup/*` → `.Entities.Pickup` |
| `Interfaces/IReadOnlyEntity.cs`, `IUpdatable.cs` | `Entities/*` → `.Entities` (root) |

For each move: physically relocate the file, change its `namespace`, then fix usings.

### I.3 Migration approach (build-driven)

1. Move files and update `namespace` declarations per the table above.
2. `dotnet build` and repair each missing-`using`/namespace error iteratively. Consumers that need
   new usings:

   - `Game`: `GameLoop.cs`, `LevelDirector.cs`, `EnemyPool.cs`, `PlayerEntity.cs`, `EnemyEntity.cs`,
     `OilDrumEntity.cs`, `FoodPickupEntity.cs`, `GoIndicatorEntity.cs` (UI, not entities), `MenuManager.cs`.
   - `Core`: `GameCore.cs` (Rendering), `CombatActorBase` (`IUpdatable`/`IRenderable`/`IDebugDrawable`/
     collision), `PropBase`, `PickupBase`, `TriggerEntity`, `EntityManager`, `UI/UiBase.cs` +
     `HudRoot`/`PlayerBar`/`EnemyBar`/`GoIndicator` (`IUpdatable`/`IRenderable`/`IDebugDrawable`),
     `Levels/*`, `Input/*`.
   - Tests (~30 files): replace `using MonoGameLearning.Core.Entities.Interfaces;` and
     `using MonoGameLearning.Core.Entities.Components;` with the feature usings the referencing code
     needs.

3. Update `GameLoop.CreateCollisionWorld` layer strings if desired only in a follow-up (out of scope
   here); literals are unaffected by the reorg.
4. Optionally add `global using` aliases in `Core` for the most cross-cutting namespaces (e.g.
   `.Entities`, `.Combat`) to reduce churn — but prefer explicit usings to keep the new layout visible.

### I.4 Part I gate

`dotnet build` (0 errors) then `dotnet test` — full suite green. Watch specifically for:

- `FacingDirection` migrations breaking `CombatActorBase`/`HitboxService`/`IHitboxProvider` (they now
  `using Core.Movement`).
- `IUpdatable`/`IReadOnlyEntity` no longer under `Interfaces/` — any file that only needed those
  switches to `using Core.Entities;`.
- Snapshot/DTO consumers (`LevelDirector.PopulateSnapshots`, `EnemyEntity`) switch to `Core.AI`.

---

## PART II — Split IDamageable / IDamageResponse (post-reorg paths)

Both interfaces now live in `Core/Combat`.

### II.1 Create `Combat/IDamageResponse.cs`

```csharp
namespace MonoGameLearning.Core.Combat;

public interface IDamageResponse
{
    bool IsAlive { get; }
    bool CanTakeDamage();
    void ReduceHealth(int amount);
    void OnDeath();
    void OnKnockdown(DamageInfo info);
    void OnHit(DamageInfo info);
}
```

### II.2 Edit `Combat/IDamageable.cs`

Drop `CanTakeDamage`, `ReduceHealth`, `OnDeath`, `OnKnockdown`, `OnHit`. Remaining: `Faction`,
`Health`, `MaxHealth`, `IsAlive`, `Died`, `TakeDamage(DamageInfo)`, `Heal(int)`.

### II.3 Edit `Combat/CombatService.cs`

`ApplyDamage(IDamageResponse target, DamageInfo info)`; body unchanged.

### II.4 Implementors — add `IDamageResponse`, re-label moved members, add `IDamageResponse.IsAlive`

- `Entities/Actor/CombatActorBase.cs`
- `Entities/Prop/PropBase.cs`
- `Game.Tests/HitboxTests.cs` (`TestSpatialEntity`, `TestPropForHit`)
- `Game.Tests/PickupCollisionTests.cs` (`TestPickupEntity`)
- `Game.Tests/FoodPickupEntityTests.cs` (`HealTrackerEntity`)
  (`OilDrumEntity`, `PlayerEntity`, `EnemyEntity` inherit the base interfaces — no change.)

### II.5 Update IDamageResponse casts in tests

- `Game.Tests/FoodPickupEntityTests.cs` and `Game.Tests/EntityManagerRegistrationTests.cs`:
  `((IDamageable)...).ReduceHealth(...)` → `((IDamageResponse)...).ReduceHealth(...)`.

### II.6 Gate: `dotnet build` + `dotnet test` green

---

## PART III — ICollisionLayer + EntityManager collapse (post-reorg paths)

### III.1 Create `Entities/ICollisionLayer.cs`

```csharp
namespace MonoGameLearning.Core.Entities;

public interface ICollisionLayer { string LayerName { get; } }
```

### III.2 Implement `ICollisionLayer`

- `Actor/CombatActorBase.cs`: convert `static string LayerName => "actors"` → instance;
  add interface.
- `Prop/PropBase.cs`: `public string LayerName => "props";`; add interface.
- `Pickup/PickupBase.cs`: `public string LayerName => "pickups";`; add interface.

### III.3 Refactor `Entities/EntityManager.cs`

- Replace the three typed lists with `Dictionary<string, List<ICollisionActor>> _collidablesByLayer`.
- Public API: `GetCollidables(string layer)` (empty for unknown layer) and retained
  `PickupCollidables` convenience (`= _collidablesByLayer.TryGetValue("pickups", out var l) ? l : []`).
  Remove `ActorCollidables` / `PropCollidables`.
- `AddToTypedLists`/`RemoveFromTypedLists`: replace the
  `is CombatActorBase` / `is PropBase` / `is IPickup && is ICollisionActor` branches with
  `if (entity is ICollisionLayer { } layer && entity is ICollisionActor c)` →
  `AddToCollidables(c, layer.LayerName); world.Insert(c, layer.LayerName);` (and remove on destroy).
  Keep `IUpdatable`/`IRenderable`/`IDamageable`/`IScreenRenderable` bucketing. `Clear()` iterates the dict.

### III.4 Update consumers/tests

- `GameLoop.ResolvePickupOverlaps` — `PickupCollidables` unchanged.
- `EntityManagerRegistrationTests` / `PickupRegistrationTests`: `ActorCollidables`→`GetCollidables("actors")`,
  `PropCollidables`→`GetCollidables("props")`; `PickupCollidables` usages unchanged.

### III.5 Gate: `dotnet build` + `dotnet test` green

---

## PART IV — Unify Manager/Controller names on `*Service`

Rename file + class, then update all references (production + tests). Purely mechanical.

| Old file/class | New file/class |
| --- | --- |
| `Core/Input/InputManager.cs` `InputManager` | `InputService.cs` `InputService` |
| `Core/Audio/AudioManager.cs` `AudioManager` | `AudioService.cs` `AudioService` |
| `Core/Entities/EntityManager.cs` `EntityManager` | `EntityService.cs` `EntityService` |
| `Core/Camera/CameraController.cs` `CameraController` | `CameraService.cs` `CameraService` |
| `Core/UI/GumManager.cs` `GumManager` | `GumUiService.cs` `GumUiService` |
| `Game/GameLoop/MenuManager.cs` `MenuManager` | `MenuService.cs` `MenuService` |
| `Game/GameLoop/GameStateController.cs` `GameStateController` (enums `GameState`/`GameTrigger` stay in file) | `GameStateService.cs` `GameStateService` |

- `GameLoop.cs` (fields/ctors + `CameraService.ComputeTargetX/ComputeMovementBounds`), `MenuService.cs`
  (`GameStateService`, `GumUiService`), `EnemyPool.cs` + `LevelDirector.cs` (`EntityService`,
  `AudioService`).
- Tests: `GameStateTests`, `AudioManagerTests` (→`AudioService`, incl. static
  `AudioService.ComputeMusicVolume`), `LifecycleTests` (`CameraService.`), `EnemyPoolTests`,
  `LevelDirector*`, `EnemyDropsOnDeathTests`, `HudServiceTests`, `EntityManagerRegistrationTests`
  (→`EntityServiceRegistrationTests`), `PickupRegistrationTests`, `GoIndicatorTests`, `PropDropsOnDestroyTests`.
- Rename field/local `_entityManager`→`_entityService` where it aids clarity (optional).
- Optionally rename test files `AudioManagerTests`→`AudioServiceTests`,
  `EntityManagerRegistrationTests`→`EntityServiceRegistrationTests`.

Gate: `dotnet build` + `dotnet test` green.

---

## Validation (final, mandatory per AGENTS.md)

1. `dotnet build` — 0 errors, 0 warnings.
2. `dotnet test` — full suite passes, no regressions (~394 tests; adjust the handful of
   `EntityManagerRegistrationTests`/`PickupRegistrationTests`/`FoodPickupEntityTests` branches noted in
   II.5/III.4).
3. New data structure (collision dictionary) and split interfaces keep existing `HitboxTests`,
   `EntityManagerRegistrationTests`, `PickupRegistrationTests` effective; no behavior change.
4. Manual smoke (`IsDebug`): game boots, player/enemy combat resolves, OilDrum at `x=1000` drops Food,
   pickups collide, debug overlay renders. Identical behavior pre/post.

## Risks / Notes

- **Part I is the largest churn** (namespace + every `using` across Core/Game/~30 test files), but it
  is mechanical and fully surfaced by `dotnet build`. Land it first; Parts II–IV assume the new paths.
- **New tiny namespaces** (`Core.AI`, `Core.Movement`, `Core.Animation`) are a deliberate tradeoff for
  feature cohesion; cross-feature `using`s (e.g. `IDamageable` consumers in Actor/Pickup now
  `using Core.Combat`) are expected. If namespace proliferation feels heavy, a later pass can add
  `global using` directives.
- **`Health` under `Core.Combat`** means combat-adjacent includes carry it; acceptable, since both
  `CombatActorBase` and `PropBase` use it.
- **Parts II–IV operate on Part I paths**: `CombatActorBase.cs` ⇒ `Actor/`, `PropBase.cs` ⇒ `Prop/`,
  `PickupBase.cs` ⇒ `Pickup/`, `EntityManager.cs` ⇒ `Entities/` root, `IDamageable.cs` ⇒ `Combat/`.

## Out of scope

- Centralizing `"actors"`/`"props"`/`"pickups"` literals in `GameLoop.CreateCollisionWorld` (follow-up).
- Moving UI (`UiBase`, `GoIndicatorEntity`) out of `Core.UI`/`Game.Entities.GoIndicator`.
- Renaming `EnemyPool`, `PickupBase.OnPickup(IDamageable)` signature, or `GoIndicatorEntity`.
- Merging namespace-adjacent folders further (e.g. `Combat` + `Prop`); left as-is for now.
