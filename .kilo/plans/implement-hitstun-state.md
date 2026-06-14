# Implement HitStun State

## Goal

Implement a "HitStun" state where the player is temporarily unable to move or attack after taking damage, as described in ROADMAP.md.

## Current State

A `Hurt` state already exists in `PlayerStateController` and is functionally a hitstun state:

| Behavior | Status |
|---|---|
| Plays hurt animation (non-looping, 3 frames) | ✅ `OnHurtEntry` |
| Blocks movement (`MoveStart`/`MoveStop` triggers ignored) | ✅ `.Ignore()` |
| Blocks attacking (`AttackStart`/`AttackCompleted` triggers ignored) | ✅ `.Ignore()` |
| Prevents double-hit (`TakeDamage` ignored during hurt) | ✅ `.Ignore()` |
| Returns to `Idling` when hurt animation completes | ✅ `HurtCompleted` → `Idling` |
| Can die during hurt | ✅ `Die` → `Dying` |
| Movement code doesn't run during hurt | ❌ `PlayerEntity.Update()` doesn't guard for `Hurt` |

## Changes

### 1. `PlayerEntity.Update()` — add `Hurt` to the early-return guard

Currently the early return only checks `Dead` and `Dying`. During `Hurt` (hitstun), the movement code still runs every frame — it fires `MoveStart`/`MoveStop` (ignored by state machine) and calls `UpdateFacingDirection()` (which can change sprite facing during hitstun).

Add `Hurt` to the guard so movement direction is zeroed and the movement block is skipped entirely during hitstun:

```csharp
if (_stateController.State is PlayerState.Dead or PlayerState.Dying or PlayerState.Hurt)
{
    MovementDirection = Vector2.Zero;
    base.Update(gameTime);
    return;
}
```

### 2. ROADMAP.md — mark item complete

Change `- [ ]` to `- [X]` for:

```markdown
- [X] Implement a "HitStun" state (actor is temporarily unable to move/attack).
```

### No changes needed

- **PlayerStateController.cs**: The existing `Hurt` state and its transitions already implement hitstun correctly (movement/attack blocked, hurt animation, auto-return to idle).
- **PlayerEntity.cs (elsewhere)**: `TakeDamage()` already fires `PlayerTrigger.TakeDamage` which transitions to `Hurt`. `OnAnimationCompleted` already fires `HurtCompleted` to return to Idling.
- **PlayerSprite**: `AnimationHurt` animation already exists (non-looping, 3 frames).
- **PlayerStateControllerConfig**: `OnHurtEntry`/`OnHurtExit` callbacks already wired in `PlayerEntity.CreateStateController()`.

## Tests

Existing `PlayerStateTests` already cover `Hurt` transitions:

| Test | Status |
|---|---|
| `FromIdling_TakeDamage_TransitionsToHurt` | ✅ |
| `FromMoving_TakeDamage_TransitionsToHurt` | ✅ |
| `FromAttacking_TakeDamage_InterruptsAttack` | ✅ |
| `FromHurt_HurtCompleted_TransitionsToIdling` | ✅ |
| `FromHurt_Die_TransitionsToDying` | ✅ |
| `WhileHurt_AttackAndMovementTriggers_AreIgnored` | ✅ |

No new tests needed — the state machine behavior is already fully covered. The `Update()` early-return change is a defensive guard that doesn't affect observed state machine transitions.
