# Plan: Improve Enemy AI — World-Aware Steering

## Goal

Rewrite `EnemyAI` so it can "see" the level (player, all enemies, all props, walkable bounds) and proactively steer around props and spread from other enemies — replacing the current naive "move straight at player" logic and removing the need for reactive stuck detection.

## Decisions (locked)

- **Weighted steering (boids-style)** — each frame sums forces: seek (player), separate (nearby enemies), avoid (props in sensing radius), bounds (walkable edges). No A* / waypoints. (User choice.)
- **Full world snapshot passed in** — `EnemyAI.Update` receives a single `WorldSnapshot` containing the player position, walkable bounds, all active enemies, and all props. AI never reaches into an entity manager.
- **Drop reactive stuck detection** — the previous plan's stuck-window/recovery logic is removed. Steering forces naturally curve enemies around props before contact.
- **Keep current combat AI** — attack-range check, `AttackDelayTimer`, `AttackCooldown`, attack action emission, and `DirectionUpdateInterval` throttling remain as today. Only movement logic changes.
- **Debug visualization** — when `IsDebug`: draw separation radius and sensing radius around each enemy, color frame by current dominant force (avoid=red, separate=orange, seek=green, bounds=blue).

## Affected Files

- `MonoGameLearning.Core/Entities/Helpers/EnemyAI.cs` — full rewrite of `Update` body around steering.
- `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs` — build `WorldSnapshot` per frame, pass to AI, apply Y-bearing `MovementDirection`, add debug output.
- `MonoGameLearning.Game/Levels/LevelDirector.cs` — accept props at construction; expose `ActiveEnemies` and `Props` to `GameLoop`.
- `MonoGameLearning.Game/GameLoop/GameLoop.cs` — pass props to `LevelDirector` constructor; call new debug draw pass.
- `MonoGameLearning.Game.Tests/EnemyEntityTests.cs` — update existing tests to new API; add steering tests.

## API shape (allocation-free)

In `EnemyAI.cs`:

```csharp
public readonly record struct ActorSnapshot(Vector2 Position, float HalfWidth, float HalfHeight);
public readonly record struct WorldSnapshot(
    Vector2 PlayerPosition,
    RectangleF WalkableBounds,
    IReadOnlyList<ActorSnapshot> Enemies,   // pre-built by GameLoop, reusable List<>
    IReadOnlyList<ActorSnapshot> Props);    // pre-built by GameLoop, reusable List<>

public AIAction Update(
    Vector2 selfPosition,
    float selfHalfWidth,
    float selfHalfHeight,
    in WorldSnapshot world,
    bool isIdleOrChasing,
    float deltaSeconds);
```

`List<ActorSnapshot>` buffers are pre-allocated by `GameLoop` (one pair reused across frames; cleared each build). Zero per-frame heap allocation after warmup.

## Steering forces (per-frame, when `isIdleOrChasing`)

Tunables (private const in `EnemyAI`):

- `SeekWeight = 1.0f`
- `SeparationRadius = 50f`, `SeparationWeight = 1.5f`
- `AvoidRadius = 90f`, `AvoidWeight = 3.0f` (stronger than seek so prop avoidance dominates)
- `BoundsMargin = 30f`, `BoundsWeight = 2.0f`
- `MaxSteeringForce = 600f` (units/sec²)
- `MaxSpeed = 120f` (matches `EnemyEntity.Speed`)

Algorithm:

1. Decay `AttackCooldown` (unchanged).
2. Handle attack/cooldown branches **before** steering, but emit `MovementDirection = Vector2.Zero` when in attack range (current behavior). Apply separation only in this case to keep the enemy slightly offset from neighbors during the attack wind-up.
3. Otherwise compute steering vector `steer = Vector2.Zero`:
   - **Seek**: `normalize(player - self) * SeekWeight` if `distance > MinChaseDistance`; else zero.
   - **Separate**: for each `en` in `Enemies` where `en != self && (en.Position - self).LengthSquared() < SeparationRadius²`: add `normalize(self - en.Position) * ((SeparationRadius - dist) / SeparationRadius) * SeparationWeight`.
   - **Avoid**: for each `prop` in `Props`: compute axis-aligned overlap test between self rect and prop rect (treating positions as centers, half-extents given). If overlap exists or center-distance < `AvoidRadius`: compute `desiredOffset = closestPointOnPropBounds - propCenter` (or `selfCenter - propCenter` if outside but within radius), normalize, weight by `(AvoidRadius - dist) / AvoidRadius * AvoidWeight`. This produces a vector pushing away from the prop — strongest along the axis of closest approach. When enemy is to the side of the prop, this naturally biases Y over X.
   - **Bounds**: if `self.X < WalkableBounds.Left + BoundsMargin`, add `(right, 0) * weight`; right edge mirror; top/bottom edges similarly for Y.
4. Clamp `steer.Length()` to `MaxSteeringForce`.
5. Convert to `MovementDirection`: `MovementDirection = steer.LengthSquared() > ε² ? steer.Normalized() : Vector2.Zero`. (No `PreventDiagonal` — diagonals are now meaningful, as avoidance frequently produces ±X + ±Y.)
6. Apply existing `DirectionUpdateInterval` throttling and facing-flip detection (unchanged).

`AIAction` outputs remain identical (`StartChase`/`StopChase`/`Attack`/`None`).

## Caller wiring

`EnemyEntity.Update`:

1. Build `world` snapshot once at the top of `GameLoop.Update`:
   - Pre-allocated `List<ActorSnapshot> _enemyBuf` and `List<ActorSnapshot> _propBuf` (private fields in `GameLoop`).
   - Clear each; populate from `_levelDirector.ActiveEnemies` and the props reference (`_levelDirector.Props`).
2. `GameLoop` calls `LevelDirector.SetWorldSnapshot(...)` (or `LevelDirector` exposes the buffers and `EnemyEntity.Update` reads them). Simplest: add `public IReadOnlyList<ActorSnapshot> EnemySnapshots { get; }` and `PropSnapshots { get; }` on `LevelDirector` backed by the reusable buffers. `EnemyEntity.Update` calls into them. (No need to thread through constructor — `EnemyEntity` already holds no reference to `LevelDirector` and we want to keep it that way; resolve via a small `IEnemyWorld` interface implemented by `LevelDirector` and injected into `EnemyEntity` constructor. **Alternative**: keep the existing pattern of passing things through `Update` parameters — matches the previous plan. **Pick**: pass via parameters in `EnemyEntity.Update` for testability and to avoid adding a new dependency to `EnemyEntity`. `GameLoop` becomes the single point that knows about `LevelDirector`.)
3. `EnemyEntity.Update` builds the `WorldSnapshot` from `LevelDirector` snapshots each frame and calls `_ai.Update(...)`.
4. After AI call: `Position += _ai.MovementDirection * deltaSeconds * Speed`. Y component now naturally affects position because we no longer use `PreventDiagonal` for movement. `Mover.ClampToBounds` in `GameLoop` still hard-clamps at the end.

## `LevelDirector` prop access

Add constructor parameter `IEnumerable<PropBase> props` to `LevelDirector`. `GameLoop.ReinitLevel` passes `_currentLevel.Props.Select(...)`. Cache as a `PropBase[]` field, expose `public IReadOnlyList<PropBase> Props => _props;`. `EnemyEntity` does not need to know about `PropBase` — only `GameLoop` builds the `ActorSnapshot` list.

## Debug drawing

Add `DrawDebug` override in `EnemyEntity` (in addition to the base call):

- Yellow circle (radius `SeparationRadius`) around `Position`.
- Cyan ring (radius `AvoidRadius`).
- If `IsDebug` in `GameLoop`, a small text label of the dominant force name (avoid/separate/seek/bounds) above the enemy.

Color the enemy frame rectangle based on dominant force (avoid=red, separate=orange, seek=green, bounds=blue, none=antiquewhite — the existing default).

## Tests (`EnemyEntityTests.cs`)

Update existing 11 tests to new API. Add:

1. **Steer avoids prop ahead** — prop between enemy and player; expect `MovementDirection` with a Y component away from prop's vertical center.
2. **Steer avoids prop on side** — prop to the right of player, enemy approaches from left; expect negative-Y or positive-Y bias depending on prop center vs enemy.
3. **Separation between two close enemies** — both enemies in `WorldSnapshot`, close to each other; expect non-zero Y component in each `MovementDirection`.
4. **No separation beyond radius** — same setup with enemies far apart; expect pure seek toward player.
5. **Bounds force pushes enemy inward** — self near left edge of `WalkableBounds`; expect positive X in `MovementDirection`.
6. **Steering respects attack range** — enemy within `attackRange`; expect `MovementDirection == Vector2.Zero` and `AIAction.Attack` after delay.
7. **Steering respects min chase distance** — enemy just outside `attackRange` but inside `minChaseDistance`; expect zero `MovementDirection`.
8. **Allocation check** — call `Update` many times in a loop, assert no heap allocation (use `GC.GetAllocatedBytesForCurrentThread`).
9. **No regressions** — existing attack/cooldown/throttling tests rewritten to new API still pass.

All tests use `ActorSnapshot[]` (no per-test heap beyond the array). `WorldSnapshot` takes the arrays as `IReadOnlyList<ActorSnapshot>`.

## GC / performance discipline

- All new types are `readonly record struct`.
- No LINQ in `Update` or in `GameLoop`'s snapshot build.
- Snapshot lists cleared and reused — no `new List<>` in hot path.
- No captured lambdas per frame.
- Indexed `for` loops only.

## Edge cases & invariants

- **Prop destroyed mid-frame**: next snapshot simply lacks it; forces recompute. No state to clean up.
- **Bounds force vs prop force**: bounds force clamps the position via `Mover.ClampToBounds`. If both pull the same way (e.g., enemy near left bounds, prop to the right), they cooperate. If opposite, prop avoidance wins because `AvoidWeight` (3.0) > `BoundsWeight` (2.0). Document this in the code.
- **Enemy on top of player**: seek distance ≈ 0 → seek vector zero; separation from other enemies and attack behavior dominates. Acceptable.
- **Empty `Enemies` snapshot**: skip separation pass; no NRE.
- **Combat hit (Hurt/KnockedDown/Dying)**: AI never runs because `TryHandleIncapacitatedUpdate` early-returns. No state to reset.

## Validation (per AGENTS.md checklist)

1. `dotnet build` — must succeed with 0 warnings, 0 errors.
2. `dotnet test` — all existing + new tests pass.
3. Manual playtest (`dotnet run --project MonoGameLearning.Game/MonoGameLearning.Game.csproj`, press `~` for debug):
   - 3+ enemies chasing player → visibly spread around the player, frames colored by dominant force.
   - Enemy between player and an oil drum → enemy curves around (Y) before contact; frame shows red (avoid dominant).
   - Wave-clear edge case: enemy hits bounds clamp → frame shows blue (bounds dominant), enemy does not grind against the lock.
   - Side-by-side with previous build (revert local) to confirm bunching is reduced.

## Out of scope

- A* / waypoint pathfinding.
- Jump-over-prop animations/states.
- Discrete attack slots.
- Spatial-hash neighbor query (current wave sizes ≤ ~6 — O(n²) over enemies is fine).
- Player AI, prop collision rules, or `GameLoop.ResolveCollisions` changes.

## Open questions

None remaining.
