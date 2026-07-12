# Plan: Debug Visual Indicator for Invulnerable Entities

## Context

`PlayerEntity` already tracks invincibility state via `_invincibilityTimer` and exposes `IsInvincible` (set in `OnHit`, `OnKnockdown`, `Respawn`). The HUD shows `Inv:{player.IsInvincible}` as text, but there is no **in-world** visual cue in debug mode — so a tester cannot see *when* the player is invulnerable just by looking at the scene.

The existing `CombatActorBase.DrawDebug` already draws the entity's frame outline in debug mode (two `DrawRectangle(Frame, …)` calls: `Color.AntiqueWhite` outer, `Color.Blue` inner). `DrawDebug` is only invoked when `IsDebug == true` (`MonoGameLearning.Game/GameLoop/GameLoop.cs:201`), so anything drawn inside is debug-only by construction.

Goal: when an entity is invulnerable and debug mode is on, the existing frame outline's color should change. No new draw calls. No sprite tint. Extend the existing debug-draw path.

## Design

### 1. New interface — `IInvulnerable`

Create `MonoGameLearning.Core/Entities/Interfaces/IInvulnerable.cs`:

```csharp
namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IInvulnerable { bool IsInvulnerable { get; }
```

Generic so any future entity (enemy buffs, props with damage cooldown, etc.) can opt in by implementing it.

### 2. `PlayerEntity` implements `IInvulnerable`

In `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs`:

- Add `IInvulnerable` to the class declaration's interface list (alongside `IHudPlayerData`).
- Add explicit interface implementation mapping to the existing public property:
  ```csharp
  bool IInvulnerable.IsInvulnerable => IsInvincible;
  ```
- Do **not** rename `IsInvincible` — `HudService` (`MonoGameLearning.Core/UI/HudService.cs:131, 376`) and existing tests reference it.

No change to `_invincibilityTimer` semantics. `OnHit` (1.0s), `OnKnockdown` (1.5s), and `Respawn` (2.5s) keep working as today.

### 3. `CombatActorBase.DrawDebug` — recolor one existing rectangle

In `MonoGameLearning.Core/Entities/CombatActorBase.cs`, change the **inner** `DrawRectangle(Frame, Color.Blue)` call so its color argument becomes a single ternary expression:

```csharp
var frameColor = this is IInvulnerable inv && inv.IsInvulnerable
    ? Color.Yellow
    : Color.Blue;
context.SpriteBatch.DrawRectangle(Frame, frameColor);
```

The outer `Color.AntiqueWhite` frame is preserved as the always-on baseline so the entity is still framed when not invulnerable. No second rectangle is added. The yellow is a temporary local (no new field, no allocation, no GC pressure in the hot debug-draw path — debug-draw is not in the gameplay hot path but stays zero-alloc to match `AGENTS.md` GC rules).

**Color rationale:** `Color.Yellow` is unused by the existing debug palette (white text, red hitboxes, antiquewhite/blue frames, red/green/orange/cyan AI forces on enemies) and is the conventional "invulnerable" cue.

`DrawDebug` is already gated by `IsDebug` in `GameLoop.Draw` — no `IsDebug` check is needed inside the method itself.

### 4. Enemy override — out of scope (caveat)

`EnemyEntity.DrawDebug` (`MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs:181`) calls `base.DrawDebug(context)` and then draws an AI-force rectangle **on top**. If an enemy later gains invulnerability, the AI-force rectangle would visually overwrite the invulnerability signal.

This plan does **not** fix that. Enemies do not currently have invulnerability. The plan only extends the base behavior so the **player** (and any future non-enemy invulnerable actor) gets the cue correctly. If/when an enemy invulnerability is added, the override will need to be updated to skip or conditionalize its AI-force rectangle. Note this in code review of the override if invulnerability is ever added to enemies.

## Test Plan

Extend `MonoGameLearning.Game.Tests/TestPlayerEntity.cs` with a new fixture (e.g. `PlayerEntityInvulnerabilityTests`) using the existing `PlayerEntityTester` pattern. The constructor already accepts `null!` for sprite and a stub state controller, so timer-only logic is reachable.

Tests to add:

1. `IsInvulnerable_Default_False` — fresh player; `((IInvulnerable)player).IsInvulnerable` is `false`.
2. `IsInvulnerable_True_AfterHit` — drive `OnHit` via `TakeDamage` with a non-knockdown `DamageInfo`; assert `IsInvulnerable` is `true`. Uses `Update(GameTime)` with elapsed time zero (timer not yet ticked down) to avoid racing the assertion.
3. `IsInvulnerable_True_AfterRespawn` — call `Respawn()`; assert `IsInvulnerable` is `true`.
4. `IsInvulnerable_False_AfterTimerExpires` — call `Respawn()` (2.5s), `Update` with a `GameTime` representing ≥ 2.5s elapsed, assert `IsInvulnerable` is `false`.
5. `PlayerEntity_ImplementsIInvulnerable` — compile-time / reflection check that `PlayerEntity` implements `IInvulnerable` (defends against accidental interface removal during refactor).

The existing `IDamageable` test path and `IHudPlayerData` reporting tests in `PlayerHudTests.cs` already cover the timer plumbing from a different angle; the new tests cover the **interface surface** specifically.

## Files Changed (4)

1. **New** — `MonoGameLearning.Core/Entities/Interfaces/IInvulnerable.cs`
2. **Edit** — `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs` (add `IInvulnerable` to interface list + explicit interface impl)
3. **Edit** — `MonoGameLearning.Core/Entities/CombatActorBase.cs` (replace `DrawRectangle(Frame, Color.Blue)` color arg with ternary; or extract a small `frameColor` local for clarity)
4. **Edit** — `MonoGameLearning.Game.Tests/TestPlayerEntity.cs` (new test fixture with the 5 cases above)

No changes to `GameLoop.cs`, `HudService.cs`, `EnemyEntity.cs`, or any sprite class.

## Validation

- `dotnet build` — must compile clean.
- `dotnet test` — all existing tests pass; new `PlayerEntityInvulnerabilityTests` pass.
- **Manual smoke (in debug build):**
  1. Toggle debug overlay on (existing `InputAction.Debug`).
  2. Trigger a hit on the player (enemy contact or `InputAction.DebugKill` after damaging them down) — observe the inner frame color turn **yellow** for ~1.0s, then revert to **blue**.
  3. Respawn — observe yellow for ~2.5s.
  4. Toggle debug off — frame disappears entirely (no regression to non-debug rendering).
- **Regression check:** hitboxes, health text, and outer AntiqueWhite frame all still draw in the same order and positions as before for non-invulnerable frames.

## Risks & Mitigations

- **Risk:** Future enemy invulnerability gets visually clobbered by the AI-force rectangle.  
  **Mitigation:** Documented in this plan as out of scope; review during any future enemy-invulnerability PR.
- **Risk:** Interface surface added but only one consumer (the base debug draw).  
  **Mitigation:** Minimal cost (one interface, one explicit impl). Enables future enemies/props to opt in trivially without re-planning the visual treatment.
- **Risk:** `IsInvulnerable` could be confused with `IDamageable` semantics.  
  **Mitigation:** Interface name mirrors "invulnerable" (the visual concept), not `IsInvincible` (the player-specific property name). Existing `IsInvincible` API is unchanged.

## Out of Scope

- Sprite tinting or flash effect (AGENTS.md simplicity rule).
- Componentization of invincibility into a shared `InvincibilityComponent` (YAGNI — only one consumer today).
- Per-frame blink/pulse on the invulnerable frame (statics simpler; current 1.0s/1.5s/2.5s timers are already clearly bounded).
- `EnemyEntity` AI-force-vs-invulnerability stacking (no enemy invulnerability today).
