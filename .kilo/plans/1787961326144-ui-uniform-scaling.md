# Uniform UI Scaling for GO Indicator, HUD, and Gum Menus

**Status: NOT IMPLEMENTED**

## Problem

The GO indicator sprite and sprite-batch HUD do not scale with game resolution;
only the world and Gum menus do. On resolution change, all three UI layers
behave differently:

| Layer | Behavior | Reason |
|---|---|---|
| World (player, enemies) | Scales | `SpriteBatch.Begin(transformMatrix: Camera.GetViewMatrix())` maps virtual 800×600 → actual px (GameLoop.cs:227) |
| Gum menus | Scales | `EnableExpandToWindow(1f)` (GumUiService.cs:22) |
| GO indicator + SpriteBatch HUD | **Fixed pixel** | UI pass uses identity transform `SpriteBatch.Begin()` (GameLoop.cs:269); `SCALE = 0.3f` is a fixed pixel multiplier (GoIndicatorEntity.cs:20) |

Concrete effect: at 1024×768 the camera-scale is ×1.28, at 1600×1200 it is ×2.0.
The GO indicator (512×512 source → 153.6 actual px) shrinks from ~15% to ~9.6%
of screen width relative to the world between those two resolutions. The world
looks bigger while the GO flag and health bars stay small.

The camera renders the same 800×600 world bigger at higher resolution — there is
no additional play area. So the fixed-pixel model (the justification used by RTS/
MOBA games) does not apply. The generated fix: render UI in virtual 800×600
units scaled uniformly, matching the world and Gum layers.

This also fixes a latent bug: a non-4:3 window defeats the BoxingViewportAdapter's
letterboxing for the identity-transform UI pass. The scale matrix places UI
correctly in the letterboxed region; the identity transform does not.

## Solution (chosen)

Render the SpriteBatch UI pass with the viewport scale matrix:

```csharp
// GameLoop.cs Draw(), UI pass (line ~269)
SpriteBatch.Begin(transformMatrix: ViewportAdapter.GetScaleMatrix());
```

Then GoIndicator, PlayerBar, EnemyBar, and HudRoot all operate in virtual
800×600 coordinate space and scale identically to the world and Gum menus.
`GoIndicatorEntity` needs no changes — its `SCALE`, `MARGIN`, and viewport-based
`Position` computation remain valid once the transform is in place.

## Bounds / decisions

- **Do NOT touch** `GetViewMatrix()` world pass or Gum (`EnableExpandToWindow`).
  Only the identity-transform UI `SpriteBatch.Begin()` at GameLoop.cs:269 gains
  the scale matrix (and its `IsDebug` twin, if any, for the debug UI pass).
- **Do NOT rework fixed constants** in `HudLayoutConstants` or
  `GoIndicatorEntity.SCALE/MARGIN`. They become *virtual* units automatically via
  the transform — no constant churn.
- `GoIndicatorTests` / `HudServiceTests` are pure-domain and do not exercise the
  matrix; expect zero test churn. Add a small test only if a seam makes the
  transform testable (see Risks).

## Tasks

1. In `GameLoop.Draw`, change the UI pass `SpriteBatch.Begin()` (GameLoop.cs:269)
   to `SpriteBatch.Begin(transformMatrix: ViewportAdapter.GetScaleMatrix())`.
2. Update the `IsDebug` UI debug pass (GameLoop.cs:275-283) — it calls
   `DrawString`/`DrawRectangle` in the same batch; keep it consistent
   (StartDebugOverlay on same transform, which it already shares via the same
   sprite batch).
3. *(Optional)* Add a regression test that asserts the UI pass uses a non-identity
   scale matrix. If no clean seam exists, skip — do not over-engineer.

## Risks

- UI coordinates are currently designed in actual-pixel space. Switching to the
  scale matrix converts them to virtual units. Verify HUD layout visually at 800×
  600 (and at least one higher resolution, e.g. 1024×768) to confirm no overlap
  or misalignment. All constants (fonts, bars, margins) scale proportionally, so
  proportional layout is preserved — but pixel-measured `DrawString` offsets must
  be eyeballed.
- `ViewportAdapter.GetScaleMatrix()` may be identity at default 800×600 backing
  — the fix is only observable at non-default resolution. Visual check must use a
  changed resolution (e.g. the resolution option in the settings menu).
- The debug UI text and the `[GO]` debug overlay may now appear larger at higher
  resolution. Acceptable.

## Validation

- `dotnet build --warnaserror` (0 warnings).
- `dotnet test` (all green).
- Manual: launch game, change resolution in settings to e.g. 1024×768 and
  1600×1200, verify GO indicator, player health bar, enemy bar, and debug text
  all scale with the world. Verify a non-4:3 window (if available) no longer
  misplaces UI.

## Assumptions

- The game should render all on-screen UI uniformly scaled to a virtual 800×600
  canvas (the console beat-em-up convention), NOT fixed-pixel like strategy games.
- Gum menus already behave correctly (they scale); only the SpriteBatch UI pass
  is out of line.

## Open questions

- Should the HUD/GO indicator scale be *capped*, so on very large monitors the
  UI does not dominate the screen? (Option: cap at a max matrix scale like
  Gum's `EnableExpandToWindow`, or clamp to a max effective resolution.) Most
  beat-em-ups simply scale linearly; recommend linear, revisit only if a
  specific large-display issue emerges.