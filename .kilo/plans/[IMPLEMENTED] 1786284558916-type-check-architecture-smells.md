# Plan: Remove type checks that indicate architecture smells

Findings from a codebase-wide review for `is`/`as`/cast/`GetType()` type checks that reveal design problems. All are reductions/refactors — no feature additions.

## Scope

Six distinct type-check smells across `Core` and `Game`. Each item is independent; implement in the order listed (file-conflict-free).

---

## 1. Downcast in `IRenderable` Y-sort comparer & draw-culling

**Location:** `MonoGameLearning.Core/Entities/EntityService.cs:50-58`; `MonoGameLearning.Game/GameLoop/GameLoop.cs:237,303`
**Problem:** `RenderableYComparer` receives `IRenderable` then downcasts `x is not Entity ex` to read `.Position.Y`. `GameLoop` repeats this: `((Entity)renderable).Frame` for culling and `actor is Entity entity` to apply MTV. `IRenderable` lacks `Position`/`Frame`, so consumers must downcast to the concrete `Entity`.
**Change:** Add `Position` (and `Frame`) to the render/shared abstraction so sorting, culling, and collision-pushback operate on the interface without downcasting.

- Decide the exact surface during implementation: either add `Position`/`Frame` to `IRenderable`, or introduce a shared base interface (e.g. `IPositionedEntity`) that both `IRenderable` and the collision actor expose.
- Update `RenderableYComparer` to use the interface member (delete the `is not Entity` branch / the `return 0` fallback).
- Update `GameLoop.Draw` culling and `GameLoop.ResolveCollisions` to remove the casts.

## 2. Type-based entity registry dispatch

**Location:** `MonoGameLearning.Core/Entities/EntityService.cs:110-142`
**Problem:** `AddToTypedLists`/`TryAdd<T>` probe each `Entity` with runtime `is T` against many interfaces; `IDamageable` is added to both `_damageables` and `_combatants` (lines 136/141).
**Change:** Replace the implicit runtime probe with an explicit capability surface. Prefer the smallest change:

- Remove the `_combatants` duplicate (keep `_damageables`) and update `Combatants` consumer to use `_damageables`, OR make `_combatants` hold only `IDamageable`s that are live combatants (decide with consumer semantics).
- Optionally: have `Entity` expose readiness/capability flags instead of the registry sniffing. **Do not** invent a component framework — this is a reduction.

## 3. Concrete-type filter over the whole entity list for AI props

**Location:** `MonoGameLearning.Game/Levels/LevelDirector.cs:126-130`
**Problem:** `PopulateSnapshots` scans `_entityManager.All` and pattern-matches `all[i] is PropBase prop` every frame to build the AI prop snapshot.
**Change:** Maintain a dedicated prop list instead of re-scanning/filtering `All` by type.

- Add `IReadOnlyList<PropBase> Props` (or a buffered prop-snapshot list) to `EntityService` populated at registration, matching how props are registered alongside other entities.
- Update `LevelDirector.PopulateSnapshots` to iterate that list (drop the `is PropBase` scan).

## 4. Event subscriber type-checks `sender`

**Location:** `MonoGameLearning.Game/Levels/LevelDirector.cs:308-310` (and `:86`)
**Problem:** `OnEnemyDied(object sender, EventArgs e)` does `if (sender is not EnemyEntity enemy) return;` to recover the entity; `OnPropDestroyed` uses a `PropBase` parameter/event already.
**Change:** Use a typed event (`event Action<EnemyEntity>`) so handlers receive the entity without a runtime check.

- Verify the `Died` event declaration and all subscribers; update to a typed `Action<EnemyEntity>`.
- Note: `PropBase.Destroyed` is currently `Action<PropBase>` already (see existing plan `1785173487970` for the `Destroyed` typing direction — keep consistent).

## 5. Force-cast on a stored base reference in hitbox resolution

**Location:** `MonoGameLearning.Core/Combat/HitboxService.cs:60-67`
**Problem:** `ActiveHitbox.Owner` stored as `Entity`, then unchecked `(IHitboxProvider)active.Owner` (lines 60,63) and type-check `active.Owner is IDamageable src` (line 67).
**Change:** Store the needed capability at registration instead of casting at resolution.

- Capture `Owner` as `IHitboxProvider` (plus `Faction`/`IDamageable?`) on the `ActiveHitbox` struct at `RegisterFrameHitboxes` time.
- Remove the `(IHitboxProvider)` cast and the runtime `is IDamageable` check in `ResolveHits`.

## 6. Downcast to read `Name` from `IDamageable`

**Location:** `MonoGameLearning.Core/UI/EnemyBar.cs:112`
**Problem:** `_displayTarget is Entity entity ? entity.Name : "?"` — HUD holds `IDamageable` but reaches into concrete `Entity` for a label.
**Change:** Surface the label without downcasting.

- Add `Name`/`DisplayName` to `IDamageable`, or pass the label through `OnHit`/`SetProximityTarget`.
- Prefer the additive `IDamageable` member only if `Name` is universally meaningful for damageables; otherwise pass the label in at the call site.

---

## Not in scope (valid type checks)

- State-enum `is` dispatches: `PlayerEntity.cs:68`, `EnemyEntity.cs:28,166,177`, `GameLoop.cs:222`.
- Null-guard `is null`/`is not null`: `EnemyAI.cs:166,194`.
- Minor interface probes in `GameLoop.cs:249,280` (`is IScreenRenderable`, `is IDebugDrawable`) — leave unless item 2's capability refactor naturally subsumes them.
- `EnemyBar.cs` null-checks (lines 90-101).

## Validation

1. `dotnet build --warnaserror` — 0 compilation, 0 warnings.
2. `dotnet test` — all existing tests pass (no regressions). Current suite ~394 tests.
3. Where behavior changes (items 1, 3, 4), add/adjust unit tests covering the critical failure modes: render sort order by Y, AI prop-avoidance snapshot, and typed `Died` event firing.

## Risks & Notes

- **Item 1 interface change ripples** through `EntityService`, `GameLoop`, and any other `IRenderable` consumer — verify all references compile.
- **Item 2** touches registration semantics; ensure `Register`/`ProcessPending` still add/remove every role type exactly once.
- **Item 4** changes the `Died` event signature — all emitters and subscribers must be updated together (compile-time enforced).
- Items are independent; each should be its own commit for clean review.
