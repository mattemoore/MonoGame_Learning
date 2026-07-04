# Plan: Move inline level debug from GameLoop into LevelDirector

## Context

`GameLoop.Draw` (lines 197-211) contains ~12 lines of inline `SpriteBatch.DrawLine` calls for level-related debug visualization: per-wave trigger/end lines, the level's end-trigger line, scroll-lock lines (brighter), and the walkable-top line. These are interleaved with the iteration of entity debug drawables.

`Level` already declares `public virtual void DrawDebug(...) { }` (empty, never overridden) and `LevelDirector` already declares `DrawDebug(...)` (draws spawn circles) — but `GameLoop` calls neither. The result is that two debug methods exist as dead/incomplete code while the actual drawing lives inline in `GameLoop`.

**Goal:** Move the inline level debug lines into `LevelDirector.DrawDebug`. Remove the dead `Level.DrawDebug` virtual. `GameLoop`'s debug section then becomes: iterate entity debug drawables → call `_levelDirector.DrawDebug(debugCtx)`.

## Decision

**Consolidation (Option B):** `LevelDirector.DrawDebug` becomes the single home for all level-related debug (waves, end trigger, walkable top, scroll-lock, spawn circles). `Level.DrawDebug` is removed entirely.

**Rationale:**
- `Level.DrawDebug` is dead code (empty virtual, no overrides). Per AGENTS.md "Solution Simplification" guidance, remove speculative hooks.
- `LevelDirector` already reads `_level.WaveDefs` for gameplay (`LevelDirector.cs:110, 142`). Reading `Level.EndTriggerX` / `WalkableTopY` is not new coupling.
- `LevelDirector` is the orchestrator of level flow (waves, scrolling, spawns) — debug representation is a natural extension.
- One method to read for "what does level debug look like." Reduces `GameLoop` debug code from ~12 lines to 1 call.
- Future levels only provide data; `LevelDirector` renders it. No need to override `Level.DrawDebug` per level.

**Trade-off accepted:** `LevelDirector` now knows about `Level.WalkableTopY` and `EndTriggerX`. Acceptable because (a) it already accesses Level data, and (b) debug drawing is a presentation of state `LevelDirector` already manages.

## Scope

**In scope:**
- Inline level debug lines in `GameLoop.Draw:197-211`
- Dead `Level.DrawDebug` (empty virtual)
- Adding tests for `LevelDirector.DrawDebug`

**Out of scope (intentionally):**
- `Gum.DebugOverlay.SetText(...)` call at `GameLoop.cs:216-223` — this is HUD overlay text (FPS, state, wave count), a different concern. Belongs in a future DebugOverlay refactor.
- `_numBackgroundsDrawn` / `_numEntitiesDrawn` frame metrics — also overlay-related.
- Changing any entity's existing `DrawDebug` implementations.
- Z-order: current order (waves → end trigger → scroll-lock → walkable top → spawn circles) is preserved. No visual change.

## Implementation Tasks

1. **Remove `Level.DrawDebug`** in `MonoGameLearning.Game/Levels/Level.cs:54`. Also remove the now-unused `using MonoGameLearning.Core.Rendering;` import if it becomes unused.

2. **Extend `LevelDirector.DrawDebug`** in `MonoGameLearning.Game/Levels/LevelDirector.cs:223-250` to draw, in this order:
   - All wave trigger/end lines from `_level.WaveDefs` (cyan @ 0.4f, yellow @ 0.4f) — replaces `GameLoop.cs:197-201`
   - End trigger line at `_level.EndTriggerX` (orange @ 0.4f) — replaces `GameLoop.cs:203`
   - Walkable top line at `_level.WalkableTopY` (lime @ 0.5f) — replaces `GameLoop.cs:211`
   - Scroll-lock lines from `_levelDirector.WaveTriggerX/WaveEndX` when `IsScrollLocked` (cyan @ 0.7f, yellow @ 0.7f) — replaces `GameLoop.cs:205-209`
   - Existing spawn circles (already in method) — keep last for visibility
   - Use the same color values currently in `GameLoop.cs` to preserve visual fidelity.

3. **Update `GameLoop.Draw` debug section** at `MonoGameLearning.Game/GameLoop/GameLoop.cs:188-211`:
   - Keep the entity debug drawables iteration block (`GameLoop.cs:191-195`).
   - Remove the inline wave/end-trigger/scroll-lock/walkable-top lines (`GameLoop.cs:197-211`).
   - After the entity iteration, call `_levelDirector.DrawDebug(debugCtx);` inside the `if (IsDebug)` block.
   - Verify `DebugDrawContext` is already in scope (it is — `GameLoop.cs:190` constructs it).
   - Leave the `Gum.DebugOverlay.SetText(...)` block (`GameLoop.cs:216-223`) untouched.

4. **Add tests** in `MonoGameLearning.Game.Tests/LevelDirectorTests.cs` (or new file `LevelDirectorDebugTests.cs`):
   - Test 1: `DrawDebug_DoesNotThrow_WhenGameNotStarted` — call `_director.DrawDebug(ctx)` with a stub `DebugDrawContext` (null SpriteBatch acceptable, or a no-op test double if needed) and assert no exception.
   - Test 2: `DrawDebug_DoesNotThrow_WhenScrollLocked` — drive `Update` until `IsScrollLocked` is true, then call `DrawDebug`. Assert no exception.
   - Test 3: `DrawDebug_DoesNotThrow_AfterAllWavesComplete` — assert no exception when `_currentWaveIndex >= waves.Count`.
   - Note: asserting that lines were actually drawn requires a `SpriteBatch` mock. If `SpriteBatch` is sealed/unmockable, scope tests to "no exception" only and document the limitation in a comment.

5. **Remove unused imports** in `Level.cs` (if `MonoGameLearning.Core.Rendering` becomes unused after removing `DrawDebug`).

## Validation

- `dotnet build` — must succeed with no warnings.
- `dotnet test` — all existing + new tests pass.
- Manual sanity check (optional, in debug mode): open game, toggle debug, confirm wave/end-trigger/walkable-top/scroll-lock lines render in the same positions and colors as before.

## Risks

- **Visual regression:** z-order or color mismatch. Mitigation: preserve exact color values and order from `GameLoop.cs:197-211`.
- **`SpriteBatch` instantiation in tests:** may be hard. Mitigation: scope tests to "no exception" with a minimal `DebugDrawContext` (use `null!` for SpriteBatch if existing tests do — check `TestLevel.CreateBackgroundRenderer` pattern at `LevelDirectorTests.cs:27` which already uses `null!`).

## Open Questions

None. All design decisions resolved.
