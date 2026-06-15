# OilDrumEntity State Controller (init-prop-statemachine)

## Problem

`OilDrumEntity` uses a 0.3s `_hitCooldown` timer in `TakeDamage` to prevent double-hits from multi-frame attacks. This is ad-hoc, fragile, and inconsistent with how `PlayerEntity` manages state via a Stateless-based controller.

## Solution

Create an `OilDrumStateController` (using Stateless, same pattern as `PlayerStateController`) that manages `Normal` ↔ `HitStun` transitions. Replace `_hitCooldown` with the controller's state-based hit gating. Animation changes move into the controller's entry/exit callbacks.

## Changes

### 1. Create `MonoGameLearning.Game/Entities/Props/OilDrumStateController.cs`

States:
- `Normal` — default; can take damage
- `HitStun` — invulnerable post-hit window; ignores further `Hit` triggers

Triggers:
- `Hit` — when damage is taken (only valid in `Normal`)
- `HitStunCompleted` — when hit-stun timer expires (only valid in `HitStun`)

Config callbacks:
- `OnNormalEntry` — resets to idle animation
- `OnHitStunEntry` — sets animation based on current health level, starts hit-stun timer

Controller exposes `CanFire(trigger)` and `Fire(trigger)` (same pattern as `PlayerStateController`).

### 2. Modify `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs`

- Remove `_hitCooldown` field
- Add `_stateController: OilDrumStateController` and `_hitStunTimer: float`
- Build controller config in constructor with callbacks that set animations (moved from `TakeDamage`)
- In `TakeDamage`: check `_stateController.CanFire(Hit)` instead of `_hitCooldown > 0`; fire `Hit` trigger on damage
- In `Update`: if in `HitStun` state, decrement timer and fire `HitStunCompleted` when expired

### 3. Update Tests

- `OilDrumEntity_HitStunState_PreventsDoubleHit` — simulate two `TakeDamage` calls within hit-stun window, verify only one applies
- `OilDrumEntity_HitStunCompleted_AllowsNextHit` — simulate hit-stun expiration then a new hit, verify both apply
- `OilDrumEntity_Destroyed_StillWorks` — verify health ≤ 0 still fires `Destroyed` and sets `IsAlive = false`

### 4. Append note to `TODO.md`

Add the following after the last entry in `TODO.md`:

> - `ActorEntity.TakeDamage` is a `virtual` no-op that is never called by `PlayerEntity.TakeDamage` or `OilDrumEntity.TakeDamage` (neither calls `base.TakeDamage()`). Decide whether: (a) the base should be `abstract` instead, (b) overrides should call `base.TakeDamage()` for shared logic, or (c) the current pattern is fine.