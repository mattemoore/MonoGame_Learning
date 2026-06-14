# Implement Knockdown State

## Goal

Implement a "Knockdown" state where the player is knocked to the floor, becomes briefly invulnerable, then stands back up — as described in ROADMAP.md.

## Current State

A `Hurt` state already provides a brief hitstun with invulnerability (via `.Ignore(PlayerTrigger.TakeDamage)`). Knockdown extends this with a multi-phase animation sequence: a fall animation, a brief grounded period, then a get-up animation before returning to idle.

Available sprite atlas frames:

- `adventurer-fall-00`, `adventurer-fall-01` (2 frames — falling backward)
- `adventurer-stand-00`, `adventurer-stand-01`, `adventurer-stand-02` (3 frames — standing back up)

## Design Decisions

### Trigger mechanism

Add a `knockdown` parameter to `TakeDamage(int amount, bool knockdown = false)` with a default of `false`. Existing callers are unaffected. Knockdown is triggered by heavy attacks. A `Knockdown` bool property is added to `MoveData` and propagated through `HitResult`, so the GameLoop can pass `knockdown` based on the attack data. For the initial implementation, attack3 (Punch 3, damage 12) is flagged as a knockdown attack.

An alternative simpler approach would use a damage threshold (e.g., damage >= 10), but adding the flag to `MoveData` is more explicit, composable (each attack decides its own knockdown property), and scales better when enemies get their own knockdown-capable moves.

### Two-phase animation within a single state

The `KnockedDown` state plays two non-looping animations sequentially:

1. Phase 0: `AnimationFall` (adventurer-fall, 2 frames)
2. Phase 1: `AnimationGetUp` (adventurer-stand, 3 frames)

The `OnAnimationCompleted` handler checks a `_knockdownPhase` field to distinguish which animation just finished. After the first completion, it switches to the get-up animation and increments the phase. After the second, it fires `KnockdownCompleted`.

### No knockback physics

This initial implementation does not include knockback velocity/impulse. The player falls in place. Knockback can be added later as a separate feature.

## Changes

### 1. `PlayerSprite.cs` — add fall and get-up animation constants

Add two new animation constants and their definitions:

```csharp
public const string AnimationFall = "fall";
public const string AnimationGetUp = "getup";
```

Define animations:

- `fall`: 2 frames, non-looping
- `getup`: 3 frames, non-looping

### 2. `PlayerStateController.cs` — add KnockedDown state

**`PlayerState` enum**: Add `KnockedDown`.

**`PlayerTrigger` enum**: Add `TakeKnockdown` and `KnockdownCompleted`.

**`PlayerStateControllerConfig`**: Add `OnKnockdownEntry` and `OnKnockdownExit` callbacks.

**State configuration** (`PlayerState.KnockedDown`):

```csharp
OnEntry → config.OnKnockdownEntry
OnExit → config.OnKnockdownExit
Permit(KnockdownCompleted, Idling)
Permit(Die, Dying)                    // can die during knockdown
Ignore(TakeDamage, TakeKnockdown)     // invulnerable during knockdown
Ignore(AttackStart, AttackCompleted)
Ignore(MoveStart, MoveStop)
Ignore(HurtCompleted)
Ignore(Activate)
```

**Permit `TakeKnockdown` from**: `Idling`, `Moving`, `Attacking`, `Hurt` (same states as `TakeDamage`, plus `Hurt` — meaning a second heavy hit during hitstun causes knockdown).

### 3. `PlayerEntity.cs` — wire up knockdown

- Add a `_knockdownPhase` int field (tracks which animation phase, 0 or 1)
- Add `OnKnockdownEntry` callback:
  - Set animation to `AnimationFall`
  - Reset `_knockdownPhase = 0`
  - Subscribe to animation event
- Add `OnKnockdownExit` callback:
  - Unsubscribe from animation event
  - Reset `_knockdownPhase = 0`
- Modify `TakeDamage(int amount, bool knockdown = false)`:
  - If `knockdown`: fire `TakeKnockdown`
  - Else: fire `TakeDamage` as before
- Modify `OnAnimationCompleted`:
  - Add case for `PlayerState.KnockedDown`:
    - Phase 0: set animation to `AnimationGetUp`, `_knockdownPhase = 1`, subscribe again (SetAnimation replaces controller)
    - Phase 1: fire `KnockdownCompleted`
- Modify `Update()` early-return guard: add `PlayerState.KnockedDown`
- Modify `Reset()`: reset `_knockdownPhase = 0`

### 4. `PlayerMoves.cs` — mark attack3 as knockdown

Add `Knockdown = true` to the attack3 `MoveData` entry:

```csharp
["attack3"] = new()
{
    Name = "Strong Punch",
    AnimationKey = PlayerSprite.AnimationAttack3,
    Damage = 12,
    Knockdown = true,
    ...
}
```

### 5. `Core/Combat/MoveData.cs` — add `Knockdown` property

```csharp
public bool Knockdown { get; init; }
```

### 6. `Core/Combat/HitboxService.cs` — propagate knockdown through `HitResult`

Add a `Knockdown` field to `ActiveHitbox` and propagate it to `HitResult`:

```csharp
public record struct HitResult
{
    public SpatialEntity Target { get; init; }
    public int Damage { get; init; }
    public SpatialEntity Source { get; init; }
    public bool Knockdown { get; init; }    // new
}
```

In `RegisterFrameHitboxes`, read `move.Knockdown` and store it on the `ActiveHitbox`.
In `ResolveHits`, propagate it to the `HitResult`.

### 7. `GameLoop.cs` — pass knockdown flag through

```csharp
combatant.TakeDamage(hit.Damage, knockdown: hit.Knockdown);
```

### 8. `ICombatant.cs` — update interface

```csharp
void TakeDamage(int amount, bool knockdown = false);
```

### 9. `ROADMAP.md` — mark item complete

Change:

```markdown
- [ ] STRETCH: Implement a "Knockdown" state
```

to:

```markdown
- [X] Implement a "Knockdown" state (actor is knocked onto the floor, becomes invulnerable, then stands back up).
```

Remove the "STRETCH:" prefix.

## Tests

### New state machine tests (`PlayerStateTests.cs`)

| Test | Description |
|------|-------------|
| `FromIdling_TakeKnockdown_TransitionsToKnockedDown` | Fire TakeKnockdown from Idling → KnockedDown |
| `FromMoving_TakeKnockdown_TransitionsToKnockedDown` | MoveStart → TakeKnockdown → KnockedDown |
| `FromAttacking_TakeKnockdown_InterruptsAttack` | AttackStart → TakeKnockdown → KnockedDown (not Idling) |
| `FromHurt_TakeKnockdown_TransitionsToKnockedDown` | TakeDamage → TakeKnockdown → KnockedDown |
| `FromKnockedDown_KnockdownCompleted_TransitionsToIdling` | TakeKnockdown → KnockdownCompleted → Idling |
| `FromKnockedDown_Die_TransitionsToDying` | Can die during knockdown |
| `WhileKnockedDown_AttackAndMoveTriggers_AreIgnored` | Ignores AttackStart, MoveStart, MoveStop |
| `WhileKnockedDown_TakeDamage_IsIgnored` | Invulnerable during knockdown |
| `KnockdownEntryCallback_IsInvoked` | Verifies entry callback fires |
| `KnockdownExitCallback_IsInvoked` | Verifies exit callback fires |

### PlayerEntity tests

If a test framework for PlayerEntity with mock sprite exists, add test verifying that `TakeDamage(amount, knockdown: true)` transitions to KnockedDown. Otherwise, note this as a gap.

No animation integration tests needed beyond what the state machine covers, since animation sequencing is tested by verifying the correct triggers are fired at the right phase.

## No changes needed

- **ActorEntity.cs**: The `TakeDamage` override at the base class level is a no-op implementation; the override in `PlayerEntity` handles the logic.
- **Content pipeline**: Animations already exist in the sprite atlas (`adventurer-fall-*`, `adventurer-stand-*`), no new assets needed.
