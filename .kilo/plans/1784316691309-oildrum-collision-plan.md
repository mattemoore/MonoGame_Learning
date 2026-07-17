# Oil Drum — Decouple Collision Box From Frame

## Goal
Player can walk past an oil drum at the same Y without colliding with the drum's body. Visual / hit-detection frame stays unchanged so the drum remains hittable in its full silhouette.

## Decisions
- **Three anchor modes** are required (Top / Center / Bottom) so future props can sit on shelves, mid-air, or floors without per-prop math.
- Oil drum uses **Top-anchored** collision.
- **Collision height fraction = 0.5** of visual height (~40 px of the 79 px sprite). Tunable as a `const` on `OilDrumEntity` for fast iteration.
- **Hit-detection box stays equal to the visual frame.** `HitboxService.ResolveHits` already uses `target.Frame` and is unchanged. Only the world-collision shape is shortened.
- Debug overlay draws both rectangles with distinct colors so the decoupling is visible while iterating.

## Affected Boundaries

| Layer | File | Change |
|-------|------|--------|
| Core  | `MonoGameLearning.Core/Entities/CollisionAnchor.cs` (new) | `enum CollisionAnchor { Top, Center, Bottom }` |
| Core  | `MonoGameLearning.Core/Entities/PropBase.cs` | Add `Anchor`, `CollisionHeightFraction` virtuals; `CollisionBounds` derived rect; `Shape` uses it; `DrawDebug` renders both |
| Game  | `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs` | Override `Anchor = Top`, `CollisionHeightFraction = 0.5f` via private constants |
| Tests | `MonoGameLearning.Game.Tests/OilDrumCollisionTests.cs` (new) | Unit + `CollisionWorld2D` integration tests |

No changes to `Entity.cs`, `HitboxService.cs`, `EntityManager.cs`, `LevelDirector.cs`, or `EnemyAI.cs`.

## API Additions

### `CollisionAnchor.cs`
```csharp
namespace MonoGameLearning.Core.Entities;
public enum CollisionAnchor { Top, Center, Bottom }
```

### `PropBase.cs` — additions only
```csharp
public virtual CollisionAnchor Anchor => CollisionAnchor.Top;
public virtual float CollisionHeightFraction => 1.0f;

protected RectangleF CollisionBounds => ComputeCollisionBounds(Frame, CollisionHeightFraction, Anchor);

private static RectangleF ComputeCollisionBounds(RectangleF frame, float heightFraction, CollisionAnchor anchor)
{
    Debug.Assert(heightFraction is > 0f and <= 1f, $"CollisionHeightFraction must be in (0,1], got {heightFraction}");
    float h = frame.Height * heightFraction;
    float y = anchor switch
    {
        CollisionAnchor.Top    => frame.Y,
        CollisionAnchor.Center => frame.Y + (frame.Height - h) * 0.5f,
        CollisionAnchor.Bottom => frame.Bottom - h,
        _ => frame.Y,
    };
    return new RectangleF(frame.X, y, frame.Width, h);
}
```

Replace the `Shape` body:
```csharp
public CollisionShape2D Shape => new(new BoundingBox2D(
    new Vector2(CollisionBounds.X, CollisionBounds.Y),
    new Vector2(CollisionBounds.Right, CollisionBounds.Bottom)));
```

Replace `DrawDebug` to render both rectangles:
```csharp
public void DrawDebug(DebugDrawContext context)
{
    context.SpriteBatch.DrawRectangle(Frame, Color.AntiqueWhite);        // hit / visual frame
    context.SpriteBatch.DrawRectangle(CollisionBounds, Color.Yellow);   // shortened collision
    // ... existing health text
}
```

### `OilDrumEntity.cs` — additions only
```csharp
private const float DrumCollisionHeightFraction = 0.5f;
public override CollisionAnchor Anchor => CollisionAnchor.Top;
public override float CollisionHeightFraction => DrumCollisionHeightFraction;
```

## Tests (`OilDrumCollisionTests.cs`)

Build a `TestProp` subclass of `PropBase` with a real `AnimatedSprite` or a test double exposing `Sprite` size; reuse the existing `Entity` test fixtures pattern (no GraphicsDevice needed — supply a stub sprite whose `Size` is set in the test).

Required cases:
1. `TopAnchor_CollisionBounds_TopMatchesFrameTop`
2. `TopAnchor_CollisionBounds_HeightIsShorterThanFrame` (uses 0.5f fraction; verifies height = 50%)
3. `CenterAnchor_CentersVerticallyWithinFrame`
4. `BottomAnchor_BottomEdgeMatchesFrameBottom`
5. `FullFraction_CollisionBoundsEqualsFrame` (default case stays backward-compatible)
6. `Shape_IsDerivedFromCollisionBounds_NotFrame` — assert `Shape.Bounds` matches the shortened rect when fraction < 1.
7. **Integration**: build a `CollisionWorld2D`, insert `OilDrumEntity` and a test actor, query pairs at Y positions *above*, *inside*, and *below* the collision band — only the inside-Y actor must report a collision. Confirms the player can walk past at floor level.

## Manual Verification
1. `dotnet build` (Core + Game + Tests).
2. `dotnet test` — all suites green, including new file.
3. Run the game, walk up to an oil drum from the side at floor Y; the player should pass under without snagging. Whack the drum from above — it still takes damage (hitbox = visual frame).
4. Toggle debug (`F1` or whatever the current binding is) and confirm the **yellow** shortened rect sits at the drum's top while the **antique-white** rect spans the full sprite.

## Out of Scope
- Shortening AI avoidance radius (enemies will still treat the full sprite as a no-walk zone via `ActorSnapshot`).
- Per-frame hurtboxes or state-driven collision shapes.
- Re-running the debug-draw duplication pass already scheduled in TODO.

## Risks
- If `CollisionHeightFraction = 0.5f` ends up too generous visually, raise it — keep as `const` for one-touch tuning.
- The `ActorSnapshot` path keeps the full drum footprint for enemies. If desired later, `LevelDirector.PopulateSnapshots` can be pointed at `CollisionBounds` — leave for a follow-up.