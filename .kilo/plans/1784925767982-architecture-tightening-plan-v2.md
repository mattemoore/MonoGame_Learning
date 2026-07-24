# Architecture & Interface Tightening Plan

Reduce coupling, finish encapsulation, remove state machine startup indirection, and move generic engine code from the Game project into Core. Items are categorized **MUST** / **SHOULD** / **OPTIONAL**.

## Context from code review (verified against source)

- **Already done — do not re-implement:** commit `1b69bab` ("Tighten concrete Entity parameters to narrower interfaces") already narrowed `HitResult.Target` to `IDamageable`, removed the dead `HitResult.Source` field, changed `HitboxService.Clear`/`ClearAttackDedup`/`GetActiveHitboxBounds` to take `IHitboxProvider`, and introduced `IReadOnlyEntity` (implemented by `Entity`, consumed by `Mover.ClampToBounds`). GameLoop call sites use no casts for these.
- **Extensibility check: PASS.** No item below removes an extension point. `IAnimated`, `IMoveableEntity`, `IPickup`, and `CollisionAnchor` are intentionally kept (future enemy types, pickup types, animated entities, top-vs-bottom prop anchoring). `CreateStateController()` stays `virtual`, so pooled/subclassed enemies (e.g. `TestEnemyEntity`) keep working. Moving data types and engine services to Core benefits future levels, props, pickups, and games.
- **GC/perf check: PASS.** All items are allocation-neutral except M3, which allocates one `EnemyStateController` per enemy rent — rents are per-spawn, not per-frame, so acceptable. Verified non-issue: `CombatActorBase.Shape` news up `CollisionShape2D`/`BoundingBox2D` per get, but IL disassembly of MonoGame.Extended 6.0.0 confirms both are `readonly struct` — zero heap allocation.
- **Known library-level per-frame allocation (not yet tracked anywhere — S7 adds it to ROADMAP.md):** `CollisionWorld2D.QueryCollisionPairs` (called every frame in `GameLoop.ResolveCollisions`, :297) is a compiler iterator — allocates a state-machine object + `HashSet<ActorPairKey>` per call. Fixing requires vendoring/patching MonoGame.Extended, so it is out of scope for code changes in this plan; task S7 only records it as a profile-first investigation item.

---

## MUST — Core encapsulation & state machine startup

### M1. Encapsulate `EnemyBar` internals

**Files:** `MonoGameLearning.Core/UI/EnemyBar.cs`, `MonoGameLearning.Core/UI/HudService.cs`

- Change the 7 `internal` fields (EnemyBar.cs:13-19) to `private`.
- Add public surface to `EnemyBar`: `IsVisible`, `DisplayTarget`, `IsDeathLinger` (get-only properties) and `SetProximityTarget(IDamageable)`.
- `HudService` keeps its existing property names (`IsEnemyBarVisible`, `EnemyBarTarget`, `IsDeathLinger`) but delegates to the new `EnemyBar` members instead of `internal` fields; `HudService.SetProximityTarget` calls `_enemyBar.SetProximityTarget(target)`.
- **No test changes** — verified that HudServiceTests/PlayerHudTests only use `HudService` properties, never internals. They act as the canary: if they pass unchanged, the encapsulation is behavior-identical.

### M2. Remove `PlayerState.Dummy` + `Activate` trigger

**File:** `MonoGameLearning.Game/Entities/Player/PlayerStateController.cs`

- Initial state becomes `PlayerState.Idling`; delete the `Dummy` enum value, `PlayerTrigger.Activate`, the Dummy `Configure` block (:57-61), the 7 `.Ignore(PlayerTrigger.Activate)` lines (:70,:82,:95,:107,:122,:134,:148), and the `StateMachine.Activate()` call (:153).
- Invoke `config?.OnIdleEntry?.Invoke()` at the end of the constructor (replaces the deferred entry callback — equivalent behavior; `GameStateController` already proves the pattern by starting directly in `TitleScreen`).
- Verified: no player test references `Dummy`/`Activate`. Observable behavior after construction is `State == Idling` with idle animation set — same as today.

### M3. Remove `EnemyState.Dummy` + `Reset`/`Activate` triggers

**Files:** `MonoGameLearning.Game/Entities/Enemy/EnemyStateController.cs`, `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs`, `MonoGameLearning.Game.Tests/EnemyStateTests.cs`

- `EnemyStateController`: initial state `EnemyState.Idle`; delete the `Dummy` enum value, `EnemyTrigger.Activate`, `EnemyTrigger.Reset`, `ResetToRoot()` (:172-177), the Dummy `Transitions`/`IgnoredTriggers` entries (:62,:90), the `OnActivate` (:152-153), and the `Permit(Reset, Dummy)` line (:167-168). Invoke `callbacks?.OnIdleEntry?.Invoke()` at the end of the constructor.
- `EnemyEntity`: remove `readonly` from `_stateController` (:17); in `Reset()` (:201) replace `_stateController.ResetToRoot()` with `_stateController = CreateStateController()` — the call stays **virtual** so `TestEnemyEntity` and future enemy subclasses keep their overrides (matches `PlayerEntity.Reset`, PlayerEntity.cs:176).
- Tests: **delete** `ResetToRoot_FromDead_ReturnsToIdle` and `ResetToRoot_AllowsActivateToFireAgain` (EnemyStateTests.cs:382-418 — the only `ResetToRoot` callers besides `EnemyEntity.Reset`). Add: fresh controller starts in `Idle` and invokes `OnIdleEntry`. Verify no other test calls `.Reset(` on enemies (check EnemyEntityTests).
- `TestEnemyEntity.cs` needs **no** change for this item.

### M4. Rewrite `EnemyStateController` as fluent config

**File:** `MonoGameLearning.Game/Entities/Enemy/EnemyStateController.cs` — **do after M3** (less to translate).

- Replace the `Transitions`/`IgnoredTriggers` dictionaries, `AllStates` array, switch expressions, and loop machinery with explicit `StateMachine.Configure(EnemyState.X).OnEntry(...).Permit(...).Ignore(...)` blocks matching `PlayerStateController` style.
- **RISK (load-bearing):** `EnemyStateController.Fire` (:183-186) is **unguarded** — no `CanFire` check, unlike `PlayerStateController.Fire`. The Ignore lists are what turn stray triggers (e.g. `StopChase` while not chasing) into no-ops instead of `InvalidOperationException`. The rewrite must reproduce the Permit/Ignore matrix **exactly** (minus entries removed by M3). The ~40 EnemyStateTests are the safety net — all must pass unchanged.
- **Decision:** keep `Fire` unguarded. Adding a `CanFire` guard would mask missing-`Ignore` bugs as silent no-ops; the explicit throw aids diagnosis.

### M5. Replace `CombatActorBase` protected `Action` fields with an immutable callbacks object

**Files:** `MonoGameLearning.Core/Entities/CombatActorBase.cs`, `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs`, `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs`, `MonoGameLearning.Game.Tests/TestEnemyEntity.cs`

- New type (own file, per no-nested-types rule): `MonoGameLearning.Core/Entities/CombatActorCallbacks.cs` — `public sealed class` with 8 get-only `Action` properties (`OnAttackingExit`, `OnHurtEntry`, `OnHurtExit`, `OnKnockdownEntry`, `OnKnockdownExit`, `OnDyingEntry`, `OnDyingExit`, `OnDeadEntry`), matching the style of `PlayerStateControllerConfig`.
- The 8 callbacks are base-private method refs (`AttackingExitImpl`, etc.), so `CombatActorBase` builds the object in **its own constructor** and exposes `protected CombatActorCallbacks Callbacks { get; }`. Delete the 8 `protected Action ... = null!;` fields (:167-174) and `InitSharedCallbacks()` (:176-186).
- Delete the two `InitSharedCallbacks()` call sites (PlayerEntity.cs:81, EnemyEntity.cs:63) — removes the temporal-coupling footgun where every subclass must remember to call it before `CreateStateController()`.
- Update subclass configs to read `Callbacks.OnAttackingExit` etc. (PlayerEntity.cs:116,124,133,139,140; EnemyEntity.cs:97,105,114,120,121; TestEnemyEntity.cs:14-21).

---

## SHOULD — Moves to Core & small consistency items

### S1. Move `CameraController` to Core, narrow parameter, drop redundant fields

**File:** `MonoGameLearning.Game/GameLoop/CameraController.cs` → `MonoGameLearning.Core/Camera/CameraController.cs` (namespace `MonoGameLearning.Core.Camera`)

- Constructor param `PlayerEntity` → `Entity` (only `Position.X` is read: :70,:73,:81,:87,:90).
- Delete the 4 redundant `readonly` field copies (:11-14) — primary-constructor params are already in scope class-wide.
- Static methods move unchanged. Update `GameLoop` usings + call site (:410).

### S2. Move `BackgroundRenderer` to `MonoGameLearning.Core.Rendering`

- Pure move, zero Game deps. Update usings in `Level.cs` (:5), `Level1.cs`, `GameLoop.cs` (:24).
- Leave the hardcoded `"backgrounds/background1"` content path in `Create` as-is (acceptable coupling; tightening it is out of scope).

### S3. Move level data types to `MonoGameLearning.Core.Levels`

**Files:** `WaveDef.cs` (also contains `EnemySpawnDef` — both move), `PropSpawnDef.cs`, `PickupSpawnDef.cs`, `SpawnSide.cs`, `SpawnVertical.cs` → `MonoGameLearning.Core/Levels/`

- Pure data types; `PropSpawnDef` already references Core's `CollisionAnchor`. Update usings in `Level.cs`, `Level1.cs`, `LevelDirector.cs`, `EnemyPool.cs`, `EnemyEntity.cs` (:10), and affected tests (LevelDirectorTests, EnemyPoolTests, LevelDirectorPickupSpawnTests).

### S4. Move `GameLoop.CreateCollisionWorld` to Core

**File:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:416-432` → `MonoGameLearning.Core/Entities/CollisionWorldFactory.cs`

- Pure static factory (only MonoGame.Extended + `RectangleF`); same "engine code in Game project" category as S1–S3. CollisionLayerTests/CollisionWorld2DTests cover the behavior.
- Keep the `"actors"`/`"props"`/`"pickups"` layer names as-is.

### S5. Remove dead `_frameDirty` from `Entity.Frame`

**File:** `MonoGameLearning.Core/Entities/Entity.cs:15,24,33`

- The field is initialized `true`, set `false` on first compute, and **never set back to `true`** — the `_lastFramePosition`/`Width`/`Height` comparison performs all invalidation. Delete the field and its two usages. Behavior-identical.

### S6. Make `HitboxService` dedup key consistent with its `IHitboxProvider` parameters

**File:** `MonoGameLearning.Core/Combat/HitboxService.cs:55,123`

- `_attackDedup` is keyed by `Entity`, so `ClearAttackDedup(IHitboxProvider)` does `_attackDedup.Remove(owner as Entity)` — a **silent no-op** for any non-`Entity` provider. Re-key the dictionary to `IHitboxProvider` and drop the cast. Reference equality is preserved (`Entity` doesn't override `Equals`); `RegisterFrameHitboxes`/`ResolveHits` key by `active.Owner` (`Entity`), which implicitly converts.
- HitboxTests must pass unchanged.

### S7. Doc fixes

- **AGENTS.md:** MonoGame.Extended version says `5.3.1`; csproj uses `6.0.0`. Update.
- **ROADMAP.md:** add the `QueryCollisionPairs` per-frame iterator allocation as a profile-first investigation item (see Context section above).

---

## OPTIONAL — take or leave

### O1. Move abstract `Level` to `MonoGameLearning.Core.Levels`

Unblocked after S2 (its only Game dependency is `BackgroundRenderer`). Concrete `Level1` (content paths, GAME_WIDTH coupling) stays in Game. Note: the existing base-constructor-calls-abstract-property pattern (`WalkableTopY`, `BackgroundCount` in the `Level` ctor) predates this move and is unchanged by it.

### O2. `IMoveableEntity : IReadOnlyEntity`

Removes the `(IReadOnlyEntity)movable` cast at GameLoop.cs:206. All implementers are `Entity` today. **Rejected by default** if you want to keep interface surface minimal — the cast is safe in practice.

### O3. (explicitly rejected) Add `Frame` to `IRenderable` for the culling cast

GameLoop.cs:236 casts `((Entity)renderable).Frame` and `RenderableYComparer` casts similarly. **Do not do this** — it grows the interface for two call sites that are safe in practice.

---

## Execution order

1. **M1** (EnemyBar encapsulation) — Core-local, zero test churn.
2. **M2** (player Dummy removal).
3. **M3** (enemy Dummy/Reset removal) — before M4.
4. **M4** (enemy fluent rewrite) — smallest after M3.
5. **M5** (callbacks object).
6. **S1–S4** (all moves in one pass — one round of `using` updates across Game + tests).
7. **S5, S6** (micro-consistency).
8. **S7** (docs) + optional **O1/O2**.

Run `dotnet build` (0 errors, 0 warnings) and `dotnet test` after **each** step; all steps are independent enough to land separately.

## Validation

1. `dotnet build` — 0 errors, 0 warnings.
2. `dotnet test` — all tests pass; only EnemyStateTests loses the 2 `ResetToRoot` tests (replaced per M3).
3. Behavior-identical checklist (manual smoke, `IsDebug` on): enemy pool rent/return across waves, player death/respawn, HUD hit-linger + death-linger, camera wave-lock and wave-clear catch-up, attack dedup (one hit per swing per target), prop collision pushback, pickup heal.
4. After moves (S1–S4): confirm no stale `MonoGameLearning.Game.Levels` / `MonoGameLearning.Game.Rendering` usings remain for moved types.

## Files touched

| File | Items |
|---|---|
| `MonoGameLearning.Core/UI/EnemyBar.cs` | M1 |
| `MonoGameLearning.Core/UI/HudService.cs` | M1 |
| `MonoGameLearning.Game/Entities/Player/PlayerStateController.cs` | M2 |
| `MonoGameLearning.Game/Entities/Enemy/EnemyStateController.cs` | M3, M4 |
| `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs` | M3, M5 |
| `MonoGameLearning.Game.Tests/EnemyStateTests.cs` | M3 (delete 2, add 1) |
| `MonoGameLearning.Core/Entities/CombatActorBase.cs` | M5 |
| `MonoGameLearning.Core/Entities/CombatActorCallbacks.cs` | **New** (M5) |
| `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs` | M5 |
| `MonoGameLearning.Game.Tests/TestEnemyEntity.cs` | M5 |
| `MonoGameLearning.Game/GameLoop/CameraController.cs` → `MonoGameLearning.Core/Camera/` | S1 |
| `MonoGameLearning.Game/Rendering/BackgroundRenderer.cs` → `MonoGameLearning.Core/Rendering/` | S2 |
| `MonoGameLearning.Game/Levels/{WaveDef,PropSpawnDef,PickupSpawnDef,SpawnSide,SpawnVertical}.cs` → `MonoGameLearning.Core/Levels/` | S3 |
| `MonoGameLearning.Core/Entities/CollisionWorldFactory.cs` | **New** (S4, from GameLoop.cs) |
| `MonoGameLearning.Game/GameLoop/GameLoop.cs` | S1, S4 (call sites/usings) |
| `MonoGameLearning.Core/Entities/Entity.cs` | S5 |
| `MonoGameLearning.Core/Combat/HitboxService.cs` | S6 |
| `AGENTS.md`, `ROADMAP.md` | S7 |
| `MonoGameLearning.Game/Levels/Level.cs` | O1 (optional) |
| `MonoGameLearning.Core/Entities/Interfaces/IMoveableEntity.cs` | O2 (optional) |
| Various Game/test files | `using` updates for S1–S4 |
