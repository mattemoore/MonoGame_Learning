# [NOT IMPLEMENTED] Lock Actor Facing During Attack

**Verdict: VALID bug** — the player can change facing direction mid-attack, which
flips the weapon overlay and attack hitbox to the opposite side mid-swing.

## Root cause

`PlayerEntity.Update` (`PlayerEntity.cs:159-180`):

```csharp
else
{
    Vector2 movementDirectionNoDiagonal = Mover.PreventDiagonal(MovementDirection);
    _stateController.Fire(PlayerTrigger.MoveStart);                          // ignored in Attacking (OK)
    Direction = Mover.UpdateFacingDirection(SpriteRenderer,
        movementDirectionNoDiagonal, Direction);                             // ← NOT gated by state
    if (_stateController.IsInState(PlayerState.Moving))
        Move(movementDirectionNoDiagonal, ...);                              // correctly gated
}
```

`Move()` is correctly gated so the player does not translate while attacking, but
the facing update runs on **any** non-zero `MovementDirection`, including during
`Attacking`. `Mover.UpdateFacingDirection` (`Mover.cs:25-38`) flips `Direction` and
the sprite `Effect`. Since `Direction` feeds `RenderWeaponOverlay`
(`CombatActorBase.cs:140-153`) and `HitboxService.RegisterFrameHitboxes`
(`CombatActorBase.cs:190`, `HitboxService.cs:33`), a mid-attack keypress teleports
the active hitbox/weapon to the other side.

Enemy is currently protected indirectly: `EnemyEntity.Update:202-203` only updates
facing on `result.FacingChanged`, and `EnemyAI` sets that flag only in the chase
branch when `isIdleOrChasing` is true (`EnemyAI.cs:45-49, 73-101`) — never while
attacking. The invariant is implicit in AI code, not entity state.

## Tasks

1. **Player (primary fix)** — move the facing update inside the movement guard so
   facing only changes when actually moving:

   ```csharp
   _stateController.Fire(PlayerTrigger.MoveStart);
   if (_stateController.IsInState(PlayerState.Moving))
   {
       Direction = Mover.UpdateFacingDirection(SpriteRenderer,
           movementDirectionNoDiagonal, Direction);
       Move(movementDirectionNoDiagonal, (float)gameTime.ElapsedGameTime.TotalSeconds);
   }
   ```

   (Idle → `MoveStart` transitions to Moving synchronously inside `Fire`, so a
   first-direction press still flips facing correctly on the same frame.)
2. **Enemy (harden)** — make the invariant explicit. In `EnemyEntity.Update`,
   gate the facing update on `!IsInAttackingState`:

   ```csharp
   if (result.FacingChanged && !IsInAttackingState)
       Direction = Mover.UpdateFacingDirection(SpriteRenderer, new Vector2(result.NewFacingX, 0), Direction);
   ```

   This preserves current behavior exactly and protects against future AI changes.
3. **Tests** (add to `PlayerStateTests.cs`):
   - `WhileAttacking_MovementInput_DoesNotChangeDirection` — construct the headless
     `PlayerEntityTester`, call `Attack(Attack1Move)` (enters `Attacking`), set
     `MovementDirection = new Vector2(-1, 0)`, `Update(ZeroGameTime)`, assert
     `Direction == FacingDirection.Right` (default, unchanged).
   - Regression guard: a normal move (not attacking) still flips `Direction` to
     `FacingDirection.Left`. (`SpriteRenderer.SetEffect` is a no-op with a null
     sprite in headless tests — `SpriteRenderer.cs:33-36`.)

## Validation

- `dotnet build --warnaserror`; `dotnet test` (existing `PlayerStateTests`,
  `EnemyStateTests`, plus the new tests green).

## Assumptions

- "Combat actor" refers to both player and enemy; the observable bug is in the
  player, and the enemy only needs defensive hardening.
- Keeping facing *frozen* during attacks (rather than delayed/queued) is the
  desired behavior, consistent with the fixed-direction hitboxes.

## Follow-up questions (for a dedicated planning session)

- Should a queued direction apply *after* the attack completes (turn after the
  swing), or is fully frozen-until-done acceptable?
- Do you want an analogous lock during `Hurt`/`KnockedDown` for the player, or is
  the existing `TryHandleIncapacitatedUpdate` early-return sufficient?
