# Plan: Address Full Codebase Review Findings

## Priority Legend
- **CRITICAL**: Gameplay bug, visual bug, or compile-time failure
- **WARNING**: Crash risk, memory leak, performance, dead code, false-confidence tests, missing coverage
- **SUGGESTION**: Refactor opportunity

---

## CRITICAL (3 items)

### C1. `Level1.cs:19` — Both backgrounds load same texture

**Problem:** Both `background` and `background1` sprites load from `"backgrounds/background1"`. The asset `backgrounds/background.png` is never loaded. Both tiles render identical visuals.

**Fix:** Change line 19 to load `"backgrounds/background"`.

```diff
- Sprite background = new(content.Load<Texture2D>("backgrounds/background1"));
+ Sprite background = new(content.Load<Texture2D>("backgrounds/background"));
```

---

### C2. `AnimationFrameTracker.cs:14` — Spurious frame-0 hitbox on attack entry

**Problem:** `Reset()` sets `_lastRegisteredFrame = -1` while `_frameIndex = 0`. After `ResetAnimationFrameIndex()` is called in `OnAttackingEntry`, `TryGetNewFrame()` immediately returns true for frame 0 because `0 != -1`. This triggers a spurious `Clear(this)` + `RegisterFrameHitboxes(this, CurrentMove, 0, Direction)` on the entry frame before any visual frame advance.

**Fix:** Set `_lastRegisteredFrame = 0` in `Reset()`.

```diff
  public void Reset()
  {
      _frameIndex = 0;
-     _lastRegisteredFrame = -1;
+     _lastRegisteredFrame = 0;
  }
```

---

### C3. `TriggerEntity.cs:7` — Missing `ICollisionActor` (by design)

**Status:** Expected — skeleton forces consumers to opt in. XML doc comment added explaining the pattern.

> Consumer subclasses add `ICollisionActor` explicitly and implement custom overlap logic (e.g., enter/exit events). The skeleton intentionally omits it to stay minimal.

---

## WARNING (14 items)

### W1. `GameLoop.cs:31` — `_player1` dead code (by design)

**Status:** Reserved for future co-op support. Comment added on the field explaining intent.

---

### W2. `ActorEntity.cs:34,68` — `Draw` NRE risk with sprite-less constructor

**Problem:** Second constructor sets `Sprite = null!`. `Draw` accesses `Sprite` with no null guard, unlike `Update` which has both `Debug.Assert` and early return. Subclass using this constructor will NRE on `Draw`.

**Fix:** Add null guard in `Draw` matching the `Update` pattern, or remove the second constructor if unused. Verify no subclass uses the sprite-less path.

---

### W3. `ActorEntity.cs:68` — `Draw` non-virtual, `PropEntity.Draw` virtual

**Problem:** `ActorEntity.Draw` is `public void Draw(...)` (non-virtual). `PropEntity.Draw` is `public virtual void Draw(...)`. Inconsistent dispatch — subclass overriding through `ActorEntity` reference silently hides base method.

**Fix:** Change `ActorEntity.Draw` to `public virtual void Draw(...)`.

---

### W4. `OilDrumEntity.cs:38,48` — Lambda captures constructor param instead of field

**Problem:** State controller lambdas capture `sprite` (constructor parameter) instead of `_sprite` (field). If `_sprite` were ever reassigned, lambdas would call `SetAnimation` on the stale reference.

**Fix:** Replace `sprite` with `_sprite` in all lambda bodies within the constructor.

---

### W5. `HitboxService.cs:32` — `_resolvedThisFrame` entity key leak

**Problem:** `_resolvedThisFrame` dictionary uses `SpatialEntity` as keys. Entity references can accumulate if `Clear(owner)` is not called on removal. Already mitigated by `ClearAll()` in `ResetGame()`, but mid-game entity destruction without explicit `Clear()` leaks.

**Fix:** Ensure every entity removal path calls `HitboxService.Clear(owner)` before dropping the reference. Add `Debug.Assert` or logging in `ResolveHits` if an entity in the dictionary is not in the active game.

---

### W6. `HitboxService.cs:54,99` — List allocations per frame

**Problem:** `new List<HitResult>()` (line 54) and `new List<RectangleF>()` (line 99) allocated every frame. GC pressure in a 60fps game loop.

**Fix:** Pool the lists or pass a reusable buffer. Example: make `ResolveHits` accept an `ICollection<HitResult>` to fill, or cache a list and `Clear()` it.

---

### W7. `GameLoop.cs:144` — Array allocation per frame in `ResolveHits` call

**Problem:** `[.. _actorEntities, .. _props]` creates a new `SpatialEntity[]` every frame. Combined with W6's internal allocation, doubles per-frame GC pressure.

**Fix:** Cache a combined list or use `Enumerable.Concat()` which defers allocation.

---

### W8. `HitboxData.cs:10` — No guard for degenerate hitbox sizes

**Problem:** No `Debug.Assert` that `Size.X > 0 && Size.Y > 0`. A zero/negative hitbox silently whiffs all intersections.

**Fix:** Add `Debug.Assert(Size.X > 0 && Size.Y > 0, "Hitbox size must be positive")` in `CreateRectangle`.

---

### W9. `OilDrumSprite.cs:14` — Static `_spriteSheet` unsafe for double Load

**Problem:** Static `_spriteSheet` reassigned on every `Load()`. Existing sprites hold references to the old sheet which becomes orphaned. Currently only called once, but brittle.

**Fix:** Either guard `Load()` with a check that throws if called more than once, or change `_spriteSheet` to non-static.

---

### W10. `HitboxTests.cs:8` — `TestSpatialEntity` extends wrong base

**Problem:** `TestSpatialEntity : PropEntity, ICombatant`. Real `PlayerEntity : ActorEntity, ICombatant`. Combat/hitbox tests exercise through wrong hierarchy.

**Fix:** Change `TestSpatialEntity` to extend `ActorEntity` instead of `PropEntity`.

---

### W11. `OilDrumStateTests.cs:127` — `TestDamageableEntity` diverges from real `OilDrumEntity`

**Problem:** Test entity lacks `Destroyed` event, uses instant hitstun completion, adds `CanTakeDamage`, uses public `Health`. Oil drum destroy/cleanup path has zero coverage.

**Fix:** Remove `TestDamageableEntity`. Write tests directly against `OilDrumEntity` (with a real `AnimatedSprite` or minimal double preserving the event contract).

---

### W12. Missing tests — `ResetGame()` path

**Problem:** `GameLoop.ResetGame()` clears hitbox service, resets players, clears/repopulates props, rebuilds collision, creates new level. Zero tests for any of it.

**Fix:** Add `GameLoopResetTests` verifying: players at spawn, hitbox service empty, props repopulated, collision component rebuilt, event handlers reattached, level recreated.

---

### W13. Missing tests — Oil drum destroyed lifecycle

**Problem:** Subscribe → destroy → unsubscribe → remove from collections. Entire lifecycle untested.

**Fix:** Test that `Destroyed` fires on death, handler unsubscribes, and entity removed from owning collections.

---

### W14. Missing tests — Camera tracking

**Problem:** Camera position, player-following, multi-screen boundary clamping have zero coverage. AGENTS.md lists this as critical failure mode.

**Fix:** Add tests verifying camera tracks entity position, clamps to level bounds, and doesn't lose player during screen-boundary transitions.

---

## Implementation Order

| Step | Item(s) | Effort |
|------|---------|--------|
| 1 | C1, C2 — CRITICAL fixes | Small (2 single-line changes) |
| 2 | W2, W3 — `ActorEntity.Draw` null guard + virtual | Small |
| 3 | W4 — `OilDrumEntity` lambda param→field fix | Small |
| 4 | W5, W6, W7, W8 — HitboxService leaks + perf | Medium (pooling + assertions) |
| 5 | W9 — `OilDrumSprite` static guard | Small |
| 6 | W10, W11 — Test entity hierarchy fixes | Medium |
| 7 | W12, W13, W14 — Missing tests | Large |

---

## Verification

After each step:
1. `dotnet build` — must compile with zero errors
2. `dotnet test` — all 118 existing tests must pass; new tests must also pass