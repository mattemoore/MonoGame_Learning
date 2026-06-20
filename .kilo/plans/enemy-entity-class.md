# Milestone 3: Enemy Entity Class — Implementation Plan

## Summary

Implement the **Enemy Entity Class** (including Chase AI and Combat AI) as specified in Milestone 3 of ROADMAP.md. This covers the first three checkboxes of the milestone; the Enemy Wave/Spawner Trigger will be done separately.

## Files to Create

### 1. `MonoGameLearning.Game/AnimatedSprites/EnemySprite.cs`

Placeholder sprite definitions for enemy animations. Use the same `adventurer` texture atlas (tinted differently at draw time via `Sprite.Color`, e.g. `Color.Red`) to avoid requiring a new asset until real enemy art is available.

- Define animations: `idle` (looping, 4 frames), `run` (looping, 6 frames), `attack1` (non-looping, 4 frames), `hurt` (non-looping, 3 frames), `die` (non-looping, 7 frames), `fall` (non-looping, 2 frames), `getup` (non-looping, 3 frames).
- Follow the exact `PlayerSprite.cs` pattern: static Load/Create, const animation keys, `DefineAnimation` helper.

**Alternative considered:** A separate set of animation frames (e.g., colored rectangles) — rejected because reusing the adventurer atlas with color tinting avoids creating new assets while still being clearly visually distinct at runtime.

### 2. `MonoGameLearning.Game/Entities/Enemy/EnemyStateController.cs`

State machine using `Stateless`, following the `PlayerStateController` / `OilDrumStateController` pattern exactly.

**States:** `Dummy` → `Idle` → `Chasing` → `Attacking` → `Hurt` → `KnockedDown` → `Dying` → `Dead`

**Triggers:** `Activate`, `StartChase`, `StopChase`, `AttackStart`, `AttackCompleted`, `TakeDamage`, `TakeKnockdown`, `KnockdownCompleted`, `Die`, `HurtCompleted`, `DeathCompleted`

**Transitions:**
- `Dummy` → `Idle` (on Activate)
- `Idle` → `Chasing` (StartChase), → `Attacking` (AttackStart), → `Hurt` (TakeDamage), → `KnockedDown` (TakeKnockdown), → `Dying` (Die)
- `Chasing` → `Idle` (StopChase), → `Attacking` (AttackStart), → `Hurt` (TakeDamage), → `KnockedDown` (TakeKnockdown), → `Dying` (Die)
- `Attacking` → `Idle` (AttackCompleted), → `Hurt` (TakeDamage), → `KnockedDown` (TakeKnockdown), → `Dying` (Die)
- `Hurt` → `Idle` (HurtCompleted), → `KnockedDown` (TakeKnockdown), → `Dying` (Die)
- `KnockedDown` → `Idle` (KnockdownCompleted), → `Dying` (Die)
- `Dying` → `Dead` (DeathCompleted)
- `Dead` — terminal, ignores all

**Config class** (`EnemyStateControllerConfig`): OnIdleEntry, OnChasingEntry, OnAttackingEntry/Exit, OnHurtEntry/Exit, OnKnockdownEntry/Exit, OnDyingEntry/Exit, OnDeadEntry callbacks.

### 3. `MonoGameLearning.Game/Entities/Enemy/EnemyMoves.cs`

Define a single enemy attack move (e.g., "Punch" — 5 damage, no knockdown) with hitboxes on frames 1-2. Follows the `PlayerMoves.cs` pattern exactly.

### 4. `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs`

```csharp
public class EnemyEntity : ActorEntity, ICombatant
```

**Fields/Properties:**
- `EnemyStateController _stateController`
- `int Health { get; private set; }`
- `int MaxHealth { get; } = 30` (enemies have less HP than player)
- `bool IsAlive => Health > 0`
- `event EventHandler Died`
- `ActorEntity Target` — reference to the player to chase/attack
- `float AttackRange = 70f` — distance at which enemy attacks
- `float MinChaseDistance = 60f` — stops moving when this close to avoid overlap
- `int _knockdownPhase` — same 2-phase knockdown pattern as PlayerEntity
- `float _attackCooldown = 0f` — prevents attack spam (e.g., 0.5s after attack completes)

**Constructor:** Takes name, position, scale, AnimatedSprite. Creates state controller with callbacks matching PlayerEntity pattern (subscribe/unsubscribe animation events around non-looping animations).

**Update(GameTime):**
1. If state is Dead, Dying, Hurt, or KnockedDown — set `MovementDirection = Vector2.Zero`, call `base.Update(gameTime)`, return early.
2. If `Target` is null, skip AI.
3. **Chase AI:** Calculate X distance to target. If distance > `AttackRange` AND state is Idle or Chasing → fire `StartChase`, set `MovementDirection` toward target (clamped to `Speed`). If distance <= `MinChaseDistance` → set `MovementDirection = Vector2.Zero`.
4. **Combat AI:** If distance <= `AttackRange` AND state is Idle or Chasing AND `_attackCooldown <= 0` → fire `AttackStart`.
5. Decrement `_attackCooldown` by deltaTime.
6. Call `base.Update(gameTime)`.

**TakeDamage(int amount, bool knockdown):** Same pattern as `PlayerEntity.TakeDamage` (guard for dead/knockedDown, reduce health, fire appropriate trigger).

**OnAnimationCompleted:** Same two-phase knockdown pattern and state-based trigger dispatch as PlayerEntity.

**Reset(Vector2 position, ActorEntity target):** Reset health, position, direction, state controller, etc.

**UpdateFacingDirection(Vector2):** Same as PlayerEntity.

### 5. `MonoGameLearning.Game.Tests/EnemyStateTests.cs`

Unit tests for `EnemyStateController` covering:
- Initial state is Idle (post-activation)
- All valid state transitions (Idle→Chasing, Idle→Attacking, Chasing→Idle, etc.)
- Invalid/ignored transitions (e.g., AttackCompleted while Idle)
- Entry/exit callback invocation
- CanFire behavior for valid vs ignored triggers
- Cover: Idle, Chasing, Attacking, Hurt, KnockedDown, Dying, Dead

Follow the `PlayerStateTests.cs` pattern exactly (~30 tests).

### 6. `MonoGameLearning.Game.Tests/EnemyEntityTests.cs`

Unit tests for `EnemyEntity` AI and damage behavior using test subclass pattern (see `OilDrumEntityBehaviorTests`/`TestDamageableEntity`):
- Entity starts alive with full health
- TakeDamage reduces health
- Health hits zero → entity dies
- Dead entity ignores further damage
- Chase AI moves toward target (verify position changes with mock/stub target)
- Combat AI triggers attack when target is in range
- Enemy stops at min chase distance (doesn't overlap)
- After attack completes, cooldown prevents immediate re-attack
- Knockdown handling (2-phase) works correctly

## Files to Modify

### `MonoGameLearning.Game/GameLoop/GameLoop.cs`

1. **Remove `_player1`** — field, instantiation, collision registration, and `Reset()` call. Player1 was reserved for future co-op but now enemies provide the challenge. The comment explaining its purpose is no longer needed.
2. Change `_actorEntities` from `[_player, _player1]` to just `[_player]`.
3. **LoadContent:** Load `EnemySprite`, create test enemies (e.g., 2 enemies at fixed positions), assign player as their target. Add enemies to collision system.
4. Add `List<EnemyEntity> _enemies` field.
5. **Update:** Update enemies, resolve hits against enemies (add enemies to `_hitTargets`), clamp enemy bounds, handle enemy death events.
6. **Draw:** Draw and debug-draw enemies in camera bounds.
7. **ResetGame:** Reset enemy list on game restart. Remove `_player1.Reset()` call.
8. **Methods:** `CreateEnemy(name, position)` factory method, `OnEnemyDied` handler that removes dead enemy from lists/collision.

### `MonoGameLearning.Core/Entities/ActorEntity.cs`

No changes needed — ActorEntity already provides everything EnemyEntity needs (Sprite, HitboxService, MovementDirection, Speed, MovementBounds, ClampToBounds, AnimationFrameTracker, FacingDirection, CurrentMove, Draw, DrawDebug, OnCollision).

## Design Decisions

1. **No separate ICombatant interface changes needed** — `ICombatant` already has Health/MaxHealth/IsAlive/Died. EnemyEntity implements it directly.
2. **Reuse `adventurer` atlas** for enemy placeholder sprites with `Sprite.Color = Color.Red` tint at construction time. This avoids needing a new asset.
3. **Chase AI is X-axis only** (horizontal side-scroller). No Y-axis pathfinding needed.
4. **Attack cooldown** prevents infinite attack loops. Set to 0.5s after each attack completes.
5. **Enemy speed** starts at 120fps (slower than player's 200fps) — enemies are threatening but outrunable.
6. **Remove `_player1`** — was reserved for future co-op but now enemies provide the opponent gameplay. Cleaning it up reduces dead code and avoids confusion. Co-op can be reintroduced properly later with a dedicated system.

## Checklist

- [ ] Create `EnemySprite.cs`
- [ ] Create `EnemyStateController.cs`
- [ ] Create `EnemyMoves.cs`
- [ ] Create `EnemyEntity.cs`
- [ ] Create `EnemyStateTests.cs`
- [ ] Create `EnemyEntityTests.cs`
- [ ] Integrate enemies into `GameLoop.cs` (including `_player1` removal)
- [ ] Run `dotnet build`
- [ ] Run `dotnet test`