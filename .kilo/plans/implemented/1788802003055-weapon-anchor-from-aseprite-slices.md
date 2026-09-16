# Weapon Overlay Anchors Authored in Aseprite (Slice Pivots)

## Goal

Author the bat's per-frame anchor positions visually in Aseprite instead of blind
hand-tuning `SwingAnchors` in `BatWeapon.cs`. The bat handle should track the actor's
hand across the 4 attack frames and sit on the hand when carried.

**Approach (chosen):** split the anchor into two sources and recombine them in code —
the *bat-side* handle point ("where the hand grips the bat, in frame-local pixels") comes
from an Aseprite **slice pivot** per frame and is re-exportable; the *actor-side* hand
position stays a hand-tuned array in `BatWeapon`. `CarryAnchor` is derived from the Hold
frame (not hardcoded), so a bat redraw stays in sync.

## The math (one formula)

The overlay draws the 64x64 region **centered** at `Position + anchor`. For the handle
point `handleLocal` (frame-local) to land on the actor hand `hand` (actor space):

```text
anchor = hand + frameCenter - handleLocal        frameCenter = (32, 32)  // 64/2
```

Applied twice:

- `CarryAnchor   = CarryHand   + frameCenter - CarryHandleOffset`
- `SwingAnchors[i] = SwingHands[i] + frameCenter - SwingHandleOffsets[i]`

## Coordinate-space note (verify once, before wiring the game)

Aseprite stores the slice `pivot` **relative to the slice `bounds` origin** (Slice window
seeds pivot at `0,0`). The exporter writes `bounds {x,y,w,h}` then `pivot {x,y}`, so:

```text
handleLocal = (bounds.x + pivot.x, bounds.y + pivot.y)
```

Frames are untrimmed (`spriteSourceSize = 0,0,64,64`), so these values are already in the
same 0-based frame coords the region uses. Confirm once: place a pivot on the bat handle
in frame 0, export, and check `bounds + pivot` lands on the handle pixels. If Aseprite is
reporting canvas-absolute pivots instead (drag and compare against the canvas ruler line),
flip the formula to `handleLocal = pivot` and adjust the converter. Do this before wiring.

## Ordered task list

### 1. Aseprite — the user, on `Sources/Weapons/Bat/bat.aseprite`

1. **Slice tool** (K) → drag a box over frame 1 (`Hold`).
2. Double-click the slice → enable **pivot** → drag the crosshair onto the **bat handle**.
3. Move to each of the 4 `Swing` frames and re-place slice + pivot on the handle (slices
   are keyframe-animated; Aseprite records a key per edited frame).
4. Export Sprite Sheet → overwrite `Sources/Weapons/Bat/bat.json` + texture, with
   **Meta → Slices** (and Tags) checked.

### 2. Converter — read slices, print handle offsets

File: `Utils/aseprite_to_monogame_extended.py`

- Parse `doc["meta"]["slices"]`; find the slice named `handle` (case-insensitive).
  Absent → print a one-line warning, skip anchors, atlas conversion proceeds unchanged.
- From its `keys`, map `frame -> (bounds.origin + pivot)`; skip keys lacking `pivot`.
- After the existing prints, emit a paste block:

  ```text
  C# handle offsets (paste into BatWeapon.cs):
    CarryHandleOffset  = new Vector2(x, y),   // Hold frame
    SwingHandleOffsets = [ new Vector2(x,y),  new Vector2(x,y),  new Vector2(x,y),  new Vector2(x,y) ],
  ```

  The 4 `SwingHandleOffsets` are in `bat-swing-00..03` order. Atlas JSON output is
  unchanged (offsets ride in C#, not the XNB).

### 3. Game code — single edit site: `Weapons/BatWeapon.cs`

- Add a private `FrameCenter` constant = `new Vector2(32, 32)` (or derive from a
  `BatFrameSize = 64` const).
- Add paste targets (from converter): `CarryHandleOffset`, `SwingHandleOffsets` (4).
- Add hand-tuned actor data: `CarryHand`, `SwingHands` (4). Seed from today's
  `CarryAnchor`/`SwingAnchors` so the first run looks close, then retune in-game.
- Replace the two literal assignments with the formula inline:

  ```csharp
  CarryAnchor = CarryHand + FrameCenter - CarryHandleOffset,
  SwingAnchors = [
      SwingHands[0] + FrameCenter - SwingHandleOffsets[0],
      SwingHands[1] + FrameCenter - SwingHandleOffsets[1],
      SwingHands[2] + FrameCenter - SwingHandleOffsets[2],
      SwingHands[3] + FrameCenter - SwingHandleOffsets[3],
  ],
  ```

No other source files change. (`BatSprite.Create()` is dead code — if convenient in the
same commit, delete it; otherwise leave it out of scope.)

### 5. Tests

- Unit (`MonoGameLearning.Game.Tests/BatSwingSyncTests.cs`): assert `BatWeapon.Bat.SwingAnchors.Length == 4`
  and `CarryAnchor` is finite; a small arithmetic check that `SwingAnchors[i] == SwingHands[i] + FrameCenter - SwingHandleOffsets[i]`.
- Converter smoke test: run against a synthetic `bat.json` with a `handle` slice and
  assert the printed `SwingHandleOffsets` equal `bounds.origin + pivot`.

### 6. Docs

- `AGENTS.md` — `Weapons` bullet: note anchors split bat-handle (`BatWeapon` offsets from
  an Aseprite slice) vs actor-hand (`SwingHands`/`CarryHand`), combined by the one-line
  formula above.
- `.kilo/skills/create-weapon/SKILL.md` — replace "reuse bat anchors" with: author a
  `handle` slice, run the converter, paste `CarryHandleOffset`/`SwingHandleOffsets`,
  keep `SwingHands`/`CarryHand` in the def, compute anchors with the formula.
- `MANUAL_TESTING.md` — bat swing row: handle tracks the hand across the swing; a bat
  redraw needs only re-export + re-paste (offsets), no actor-hand retune.

## Validation

1. Converter on `Sources/Weapons/Bat/bat.json` prints sensible offsets.
2. Paste offsets into `BatWeapon.cs`.
3. `dotnet build --warnaserror` — zero warnings.
4. `dotnet test` — all green.
5. Run: carry shows the Hold frame; handle tracks the hand across the 4 swing frames;
   hitboxes still fire at frames 2-3.

## Risks

- **Pivot coord space** (bounds-relative vs canvas-absolute) — resolve in step 1.
- **Split drift** — keep the two arrays (`*Hands` actor-side + `*HandleOffsets` bat-side)
  as constants and condition on the formula; never paste a blended value.
- **`slices: []` until authored** — converter warns, does not fail.

## Out of scope

- Re-authoring the Adventurer actor art in Aseprite (so `SwingHands` could also come from
  slices) — the actor hand stays typed in code for now.
- Any cleanup of `bat-texture.json` (stale hash export).
