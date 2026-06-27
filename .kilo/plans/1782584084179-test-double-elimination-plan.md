# Eliminate Test-Double Duplication in Unit Tests

## Goal

Replace test doubles (`TestEnemyEntity`, `TestDamageableEntity`, `CameraClampTests.ComputeClampedX`) that **copy production logic** with tests against the **real implementations**, by extracting pure-logic seams that can be unit-tested without MonoGame graphics dependencies.

## Why

Current tests pass even when the real `EnemyEntity.Update()`, `OilDrumEntity.TakeDamage()`, and `CameraController.Update()` regress — because they test copies of the logic in test entities. The most gameplay-critical systems have no effective regression coverage.

## Design Decisions

| Decision | Choice |
|---|---|
| EnemyAI decision API | Single `AIAction` enum (`None` / `StartChase` / `StopChase` / `Attack`) |
| EnemyAI facing | AI reports `FacingChanged` bool + `NewFacingX`; `EnemyEntity` calls `Mover.UpdateFacingDirection` |
| Camera math location | Static method `CameraController.ComputeTargetX(...)` |
| Entity-level tests (TestEnemyEntity / TestDamageableEntity) | **Drop entirely** — rely on existing `EnemyStateTests` + `HitboxTests` for entity-level coverage |

## Seam 1: `EnemyAI` class

**New file**: `MonoGameLearning.Core/Entities/Helpers/EnemyAI.cs`

Encapsulates AI state and decisions currently inlined in `EnemyEntity.Update()` (lines 90–152).

```csharp
public enum AIAction { None, StartChase, StopChase, Attack }

public class EnemyAI(float attackRange, float minChaseDistance)
{
    private const float AttackDelayDuration = 1.0f;
    private const float DirectionUpdateInterval = 0.35f;

    private float _directionUpdateTimer;
    private float _lastFacingX;

    public float AttackCooldown { get; set; }
    public float AttackDelayTimer { get; set; }
    public Vector2 MovementDirection { get; private set; }
    public bool FacingChanged { get; private set; }
    public float NewFacingX { get; private set; }

    public AIAction Update(
        Vector2 selfPosition,
        Vector2 targetPosition,
        bool isIdleOrChasing,
        float deltaSeconds);
}
```

Behavior (preserves the 3-branch logic from current `EnemyEntity.Update`):

1. **In-range, idle/chasing, cooldown expired** → set `MovementDirection = Zero`, progress attack delay, return `Attack` (delay done) or `StopChase` (delay progressing).
2. **Out-of-range, idle/chasing** → reset attack delay, throttle direction update (0.35s), set `FacingChanged=true` when `_lastFacingX` sign flips, return `StartChase`.
3. **In-range, chasing** (covered by branch 1 when cooldown > 0 falls through to here) → set `MovementDirection = Zero`, return `StopChase`.
4. Otherwise → decay cooldown, return `None`.

`EnemyEntity.Update()` refactor (replaces lines 90–152):

```csharp
bool isIdleOrChasing = _stateController.State is EnemyState.Idle or EnemyState.Chasing;
var action = _ai.Update(Position, Target.Position, isIdleOrChasing, deltaSeconds);

switch (action)
{
    case AIAction.StartChase:
        if (_stateController.State == EnemyState.Idle) _stateController.Fire(EnemyTrigger.StartChase);
        break;
    case AIAction.StopChase:
        _stateController.Fire(EnemyTrigger.StopChase);
        break;
    case AIAction.Attack:
        _stateController.Fire(EnemyTrigger.AttackStart);
        break;
}

if (_ai.FacingChanged)
    Direction = Mover.UpdateFacingDirection(Sprite, new Vector2(_ai.NewFacingX, 0), Direction);

if (_stateController.State == EnemyState.Chasing && _ai.MovementDirection != Vector2.Zero)
    Position += _ai.MovementDirection * deltaSeconds * Speed;
```

`EnemyEntity` constructor: instantiate `_ai = new EnemyAI(AttackRange, MinChaseDistance)`. Existing properties `AttackRange` / `MinChaseDistance` remain (used by `EnemyAI`).

## Seam 2: `CameraController.ComputeTargetX`

**Modified file**: `MonoGameLearning.Game/GameLoop/CameraController.cs`

Extract the target-X computation into a public static method:

```csharp
public static float ComputeTargetX(float playerX, float? lockedCenterX, RectangleF levelBounds, int gameWidth)
{
    if (lockedCenterX.HasValue) return lockedCenterX.Value;
    float minX = levelBounds.Left + (gameWidth / 2f);
    float maxX = levelBounds.Right - (gameWidth / 2f);
    return Math.Clamp(playerX, minX, maxX);
}
```

`Update()` calls it internally:

```csharp
float targetX = ComputeTargetX(
    _player.Position.X,
    LockedCenter?.X,
    _levelBounds,
    _gameWidth);
```

## Seam 3: `OilDrumDamage.GetEffectiveDamage`

**New file**: `MonoGameLearning.Core/Combat/OilDrumDamage.cs`

```csharp
public static class OilDrumDamage
{
    public static int GetEffectiveDamage(AttackStrength strength) => strength switch
    {
        AttackStrength.Heavy => 6,
        AttackStrength.Medium => 3,
        _ => 2,
    };
}
```

`OilDrumEntity.TakeDamage()` (line 51) replaces inline switch with `OilDrumDamage.GetEffectiveDamage(info.Strength)`.

## Test Changes

### `MonoGameLearning.Game.Tests/EnemyEntityTests.cs` — **rewrite**

- Delete `TestEnemyEntity` (entire class, lines 12–146).
- Replace `EnemyEntityBehaviorTests` fixture with `EnemyAITests`.
- New tests cover all branches of `EnemyAI.Update()`:

  | Test | Verifies |
  |---|---|
  | `Update_OutOfRange_ReturnsStartChase_MovementTowardTarget` | Branch 2, rightward direction |
  | `Update_OutOfRange_TargetOnLeft_ReturnsLeftwardMovement` | Branch 2, leftward direction |
  | `Update_InRange_IdleOrChasing_CooldownExpired_ReturnsAttackAfterDelay` | Branch 1, attack delay progression |
  | `Update_InRange_DelayProgressing_ReturnsStopChase` | Branch 1 mid-delay |
  | `Update_ChasingInRange_OnCooldown_ReturnsStopChase` | Branch 3 |
  | `Update_AtMinChaseDistance_StopsMovement` | Branch 2 min-distance check |
  | `Update_DirectionUpdateThrottled_OnlyChangesAfterInterval` | 0.35s throttle |
  | `Update_FacingChanged_OnlyWhenDirectionSignFlips` | `_lastFacingX` tracking |
  | `Update_CooldownDecays` | Cooldown decay over time |
  | `Update_AttackCooldownBlocksImmediateReAttack` | Branch 1 gated by cooldown |

### `MonoGameLearning.Game.Tests/LifecycleTests.cs` — **rewrite `CameraClampTests`**

- Delete `CameraClampTests.ComputeClampedX` helper (lines 46–51).
- Replace `CameraClampTests` fixture with `CameraTargetXTests` testing `CameraController.ComputeTargetX(...)` directly:
  - Unlocked: clamp to level bounds (left edge, right edge, mid, past-left, past-right, exact boundary, level == gameWidth).
  - Locked: locked center returned regardless of player position.

- Delete `OilDrumLifecycleTests` fixture entirely (relies on `TestDamageableEntity`).

### `MonoGameLearning.Game.Tests/OilDrumStateTests.cs` — **rewrite**

- Delete `TestDamageableEntity` (entire class, lines 11–46).
- Delete `OilDrumEntityBehaviorTests` fixture.
- Replace with `OilDrumDamageTests`:

  | Test | Verifies |
  |---|---|
  | `GetEffectiveDamage_Light_ReturnsTwo` | strength=Light |
  | `GetEffectiveDamage_Medium_ReturnsThree` | strength=Medium |
  | `GetEffectiveDamage_Heavy_ReturnsSix` | strength=Heavy |

## Files Affected

**New**:
- `MonoGameLearning.Core/Entities/Helpers/EnemyAI.cs`
- `MonoGameLearning.Core/Combat/OilDrumDamage.cs`

**Modified**:
- `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs` — delegate AI to `EnemyAI`
- `MonoGameLearning.Game/GameLoop/CameraController.cs` — add static `ComputeTargetX`, call it from `Update()`
- `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs` — use `OilDrumDamage.GetEffectiveDamage`
- `MonoGameLearning.Game.Tests/EnemyEntityTests.cs` — full rewrite, remove `TestEnemyEntity`
- `MonoGameLearning.Game.Tests/LifecycleTests.cs` — remove `CameraClampTests.ComputeClampedX` reimpl, remove `OilDrumLifecycleTests`
- `MonoGameLearning.Game.Tests/OilDrumStateTests.cs` — full rewrite, remove `TestDamageableEntity`

## Open Risks

1. **`EnemyEntity.Update()` refactor** must preserve the exact behavior of the three-branch decision tree. Three branches interact with attack cooldown and `isIdleOrChasing` in subtle ways (branch 1 requires cooldown<=0; branch 3 handles cooldown>0 while chasing+in-range). Implementation agent should trace both original and refactored flows before submitting.
2. **Facing direction** in the original code only updates inside branch 2 (`OutOfRange`). The refactor must preserve this — facing should not flip during attack delay progression or other states.
3. **`Direction` initialization**: in original, facing is only updated when `Math.Sign(MovementDirection.X) != Math.Sign(_lastFacingX)`. The `EnemyAI` class must start with `_lastFacingX = 0`, so the first direction change (sign != 0) triggers facing update. Tests should verify this.
4. **`EnemyAI.MovementDirection` is read-only externally** — entity applies movement based on returned value and its own state checks (`if state == Chasing && MovementDirection != Zero`). This matches the original control flow where movement application happens after the if/else cascade.

## Validation

1. `dotnet build` — must succeed with no warnings introduced.
2. `dotnet test` — all rewritten tests pass. Existing `EnemyStateTests`, `HitboxTests`, `LevelDirectorTests`, `GameStateTests`, `CollisionLayerTests`, `ActorCollisionTests`, `AnimationFrameTrackerTests`, `HealthDisplayTests` must still pass.
3. `grep -r "TestEnemyEntity\|TestDamageableEntity\|ComputeClampedX" MonoGameLearning.Game.Tests/` returns no matches.