# Plan: Actor Hand Anchors Authored in Aseprite (Per-Animation `hand` Slice)

> **Status:** Implemented — Phases 0–5 complete. Phase 5's live run confirmed the weapon tracks
> the hand in every animation, after fixing the pose clock (it was indexing poses with
> `AnimationController.CurrentFrame`, which is the **atlas region index**, not
> animation-relative; the overlay and hand table now use `SpriteRenderer.AnimationFrame`).
> Build clean, `dotnet test` 519 passed / 3 skipped, converter python tests OK, sheet verifier
> PASS. Post-review follow-ups applied: pose-clock controller-identity guard (a completion
> handler can `SetAnimation` mid-update), a player/enemy hand-table equality guard, the
> sheet frame/tag spec single-sourced to `Utils/build_adventurer_sheet.py`, and
> `AnimationFrameTracker.FrameIndex` demoted to an internal test seam.

## Session runbook (resume here)

Work the plan top to bottom in the phases below, **one phase at a time**. Do not start a
phase until the previous one builds and tests green, and pause after each phase for a look
at the game. Phase 1 is a manual Aseprite step (the user); everything else is code.

| Phase | Work | Owner | Gate before moving on |
| --- | --- | --- | --- |
| 0. Core split with seeded tables | Steps 1–5, the `PlayerSprite`/`EnemySprite` seed tables + entity overrides from step 8, and the test rewrites from step 9 | assistant | **Done** — `dotnet build --warnaserror` clean; `dotnet test` 517 passed / 3 skipped |
| 1. Author the actor sheet | Step 6 | user (Aseprite); assistant can script the frame/tag assembly on request (step 6 fallback) | **Done** — `Utils/verify_adventurer_sheet.py` PASS: 37 frames, 9 tags, `hand` pivot on every frame |
| 2. Converter + python tests | Step 7 and the python tests from step 9 | assistant | **Done** — `hand` emitter added; `python3 -m unittest Utils.test_aseprite_to_monogame_extended` 12 tests OK |
| 3. Paste the real tables | Step 8 (replace the seeds) | assistant | **Done** — real `hand` anchors pasted into `PlayerSprite`/`EnemySprite`; build clean, `dotnet test` 517 passed / 3 skipped |
| 4. Table-count tests + docs | The remaining step 9 tests, step 10 | assistant | **Done** — docs/skill updates landed; markdown lint clean on changed docs |
| 5. Live tuning | Final validation — pose clock fixed to `SpriteRenderer.AnimationFrame` (see decision 8 correction) | both | **Done** — user confirmed the weapon tracks the hand in every animation |

Phase 0 seed values (so the ownership split lands with **no visual change**):

- `attack1`: `(-8, 3)`, `(2, 3)`, `(12, 3)`, `(22, 3)` — the values deleted from `BatWeapon`.
- `idle` and every other key: `(-8, 3)` (the deleted `BatWeapon.CarryHandAnchor`), repeated
  to the animation's frame count so the table length matches the art.
- The `getup` prefix rename and the real anchors wait for Phase 3 — until the new atlas
  exists, `adventurer-getup-*` regions do not.

Already done ahead of Phase 3 (so the sheet is playable while the slice is authored): the
`getup` prefix rename (`adventurer-stand` → `adventurer-getup` in `PlayerSprite` and
`EnemySprite`) and the copy of `adventurer-texture.png` into `Content/images/`, alongside a
converter run that wrote the 37-region `Content/images/adventurer.json`. Phase 3 therefore
only has to replace the seed tables with the converter's `hand` output.

Commands to reuse (do not rediscover):

- converter (Phase 2): `python3 Utils/aseprite_to_monogame_extended.py MonoGameLearning.Game/Sources/Adventurer/adventurer.json --name adventurer --out-dir MonoGameLearning.Game/Content/images`
- build the actor sheet (Phase 1, writes `adventurer.aseprite` + export): `python3 Utils/build_adventurer_sheet.py`
- re-export after authoring the `hand` slice in the GUI:
  `aseprite -b MonoGameLearning.Game/Sources/Adventurer/adventurer.aseprite --sheet MonoGameLearning.Game/Sources/Adventurer/adventurer-texture.png --data MonoGameLearning.Game/Sources/Adventurer/adventurer.json --format json --list-tags --list-slices`
- verify an actor export (Phase 1, read-only): `python3 Utils/verify_adventurer_sheet.py`
- python tests: `python3 -m unittest Utils.test_aseprite_to_monogame_extended`
- C#: `dotnet build --warnaserror` then `dotnet test`
- markdown lint on a changed `.md`: `npx --yes markdownlint-cli2 <file>`

Kickoff prompt for tomorrow: **"Resume the actor hand anchors plan
(`.kilo/plans/todo/1789676929150-actor-hand-anchors-from-aseprite-slices.md`). The `hand`
slice is authored and saved; re-export and verify, then do Phase 2 onward."**

## Goal

Make a held weapon stay in the actor's hand across **every** animation, not just the
carry pose. Today the overlay draws the weapon at a single static `CarryAnchor` per
weapon, so the moment the arms move the weapon detaches — visible while running,
during `attack2`/`attack3` (which keep the carry pose), and while hurt (hurt does not
unequip, so the weapon stays visible with a frozen anchor).

The actor owns its hand position per animation frame; the weapon owns only its own grip
offset and scale. Hand points are authored visually in Aseprite as a per-frame `hand`
slice pivot and exported by the converter, so the actor art stays the single source of
truth and no import quirk leaks into Core.

## Locked decisions

1. **Scope:** author anchors for the 9 animations the placeholder Adventurer defines
   (idle, attack1, attack2, attack3, run, hurt, die, fall, getup). The Adventurer is a
   placeholder; more animations are added later as needed, at which point they get a
   `hand` slice too.
2. **Per-actor tables.** Even though `PlayerSprite` and `EnemySprite` share the
   `adventurer` atlas today, each actor owns its own table — future sprites will have
   different art, sizes, and hand positions.
3. **Untrimmed export.** The actor sheet exports at a fixed 50x37 canvas, `Trim` off, so
   Aseprite frame coordinates equal atlas region coordinates. No `spriteSourceSize`
   correction, no trim math in Core.
4. **Snap per frame.** One hand point per animation frame; no interpolation.
5. **Knockdown/death still unequip** (`CombatActorBase.KnockdownEntryImpl`/`DyingEntryImpl`).
   The fall/getup/die anchors are authored now so the art is ready, but the weapon is not
   held through those states until a separate gameplay decision changes that.
6. **Fallback:** an animation/frame with no authored anchor resolves to `Vector2.Zero`
   with a one-per-animation `Debug.WriteLine`, so the game runs mid-authoring.
7. **Core seam:** a virtual resolver on `CombatActorBase` so future armed enemies author
   their own table; enemies are not authored beyond the shared Adventurer animations now.
8. **Frame index source:** an animation-relative pose clock — `SpriteRenderer.AnimationFrame`,
   reset to 0 by `SetAnimation` and incremented on each frame change — used for anchor and
   region indexing. **Correction (Phase 5):** the plan originally chose
   `AnimatedSprite.Controller.CurrentFrame`, wrongly believing it was animation-relative; it
   is actually the **atlas region index** (`SpriteSheetAnimationFrame.FrameIndex` ←
   `TextureAtlas.GetIndexOfRegion`), which read poses from the wrong frame whenever an
   animation's atlas offset did not divide its frame count (visible on `run`, invisible on the
   rest). See the pitfall in `AGENTS.md`.

## Initial defect (resolved by Phase 0)

- `WeaponDef.CarryAnchor` / `MeleeWeaponDef.SwingAnchors` are the only pose data and are
  single/four-frame constants on the **weapon**; `CombatActorBase.RenderWeaponOverlay`
  draws at `Position + anchor * actorScale` (`CombatActorBase.cs:128`).
- `BatWeapon.CarryHandAnchor`/`SwingHandAnchors` are actor-side data living in a weapon
  class; `KnifeWeapon.CarryHandAnchor` already aliases the bat's.
- `AnimationFrameTracker.FrameIndex` only ever increments (`AnimationFrameTracker.cs:20`),
  so on the looping idle/run it grows past the frame count — unusable as a table index
  without a per-animation modulo.

## The one formula

The overlay draws the weapon region centered at `Position + anchor * actorScale`. For the
weapon's grip point `handleOffset` (frame-local, from the Aseprite `handle` slice) to land
on the actor's hand `hand` (actor units, relative to `Position`, from the `hand` slice):

```text
anchor = hand - (handleOffset - weaponFrameCenter) * weaponScale
```

Everything is authored facing right; `WeaponDef.ApplyWeaponFacing` mirrors the final
anchor for left-facing. The weapon scale cancels out of the grip-on-hand invariant, so the
formula holds for any `Scale`.

Coordinates: the `hand` table is emitted **center-relative** by the converter
(`bounds.origin + pivot - frame size / 2`), which is exactly the "offset from `Position`"
space the old `CarryHandAnchor` used. This assumes the actor's sprite origin is the region
center, which `SpriteSheetAsset.Create` guarantees (`Origin = size / 2`).

## Ordered task list

### 1. Core — track the current animation key and frame

`MonoGameLearning.Core/Rendering/SpriteRenderer.cs`

- Add `public string? CurrentAnimationKey { get; private set; }`, set in `SetAnimation`
  (so it is only ever a key that was actually applied).
- Add `public int AnimationFrame { get; private set; }` — the animation-relative pose clock:
  reset to 0 in `SetAnimation`, incremented in `AdvanceFrame`/`Update` when the sprite's atlas
  frame actually changes. **Not** `Sprite?.Controller.CurrentFrame` (that is the atlas region
  index — the original Phase 0 mistake; see decision 8's correction).
- Leave `AnimationFrameTracker` untouched: it stays the *new frame event* source for
  hitbox registration and the throwable `SpawnFrame` hook. It is not a pose index.

### 2. Core — `HandAnchorTable` type

New file `MonoGameLearning.Core/Animation/HandAnchorTable.cs` (namespace-level,
non-nested):

```csharp
public sealed class HandAnchorTable
{
    private readonly (string Key, Vector2[] Frames)[] _entries;

    public HandAnchorTable(params (string Key, Vector2[] Frames)[] entries) => _entries = entries;

    public bool TryResolve(string? animationKey, int frameIndex, out Vector2 hand)
    {
        foreach (var (key, frames) in _entries)
        {
            if (key != animationKey || frames.Length == 0) continue;
            hand = frames[frameIndex % frames.Length];
            return true;
        }
        hand = Vector2.Zero;
        return false;
    }
}
```

`params` is startup-only (table construction), not a hot path; the lookup is an
allocation-free linear scan over a handful of entries.

### 3. Core — actor resolves hand anchors; overlay uses the formula

`MonoGameLearning.Core/Entities/Actor/CombatActorBase.cs`

- Add the virtual seam and resolver:

  ```csharp
  protected virtual HandAnchorTable? HandAnchors => null;

  protected Vector2 ResolveHandAnchor()
  {
      var frame = SpriteRenderer.AnimationFrame;
      if (HandAnchors is { } table && table.TryResolve(SpriteRenderer.CurrentAnimationKey, frame, out var hand))
          return hand;
      if (SpriteRenderer.CurrentAnimationKey != _lastWarnedHandKey)
      {
          _lastWarnedHandKey = SpriteRenderer.CurrentAnimationKey;
          Debug.WriteLine($"{GetType().Name} [{Name}] no hand anchor for '{SpriteRenderer.CurrentAnimationKey}' — actor hand slice/table missing or stale");
      }
      return Vector2.Zero;
  }
  ```

- `RenderWeaponOverlay`: replace the `ResolveAnchorAndFrame` call with

  ```csharp
  var actorFrame = SpriteRenderer.AnimationFrame;
  var anchor = WeaponDef.ComputeAnchor(
      ResolveHandAnchor(), weapon.ResolveHandleOffset(IsWeaponSwingActive, actorFrame),
      weapon.FrameCenter, weapon.Scale);
  var region = weapon.ResolveRegion(IsWeaponSwingActive, actorFrame);
  ```

- `DrawDebug`: use the same substitution, and draw the resolved **hand** point as its own
  marker (e.g. cyan) next to the existing orange region-center and green grip markers.
- Fix the off-sprite warning to test against the drawn sprite region size (the sprite is
  50x37) rather than `Frame` (the 48x60 collision box), so the warning is meaningful.

### 4. Core — weapon defs shed the actor-side anchors

`MonoGameLearning.Core/Combat/WeaponDef.cs`

- Delete `CarryAnchor` and `ResolveAnchorAndFrame`.
- Keep `Name`, `Scale`, `FrameCenter`, `CarryRegion`, `CarryHandleOffset`, `Texture`,
  `Sheet`, `HasHandleOffsets`, `ResolveRegion`, `ResolveHandleOffset`,
  `ApplyWeaponFacing`, `ComputeHandleScreenPoint`, `WeaponFacingEffect`.
- Add the formula in one place:

  ```csharp
  internal static Vector2 ComputeAnchor(Vector2 hand, Vector2 handleOffset, Vector2 frameCenter, float scale) =>
      hand - (handleOffset - frameCenter) * scale;
  ```

- Update the class doc: the actor owns the hand point; the weapon owns the grip offset.

`MonoGameLearning.Core/Combat/MeleeWeaponDef.cs`

- Delete `SwingAnchors` and `ResolveAnchorAndFrame`.
- `ResolveSwingFrame` now clamps to `SwingHandleOffsets.Length` (the single owner of the
  swing-frame clamp, so region and handle selection still cannot drift):
  `SwingHandleOffsets.Length > 0 ? Math.Clamp(actorFrameIndex, 0, SwingHandleOffsets.Length - 1) : 0`.
- `ResolveRegion`/`ResolveHandleOffset` keep their `(bool isAttacking, int actorFrameIndex)`
  signatures and existing caching.

### 5. Game — weapons drop their hand anchors

- `MonoGameLearning.Game/Weapons/BatWeapon.cs`: delete `CarryHandAnchor`, `SwingHandAnchors`,
  and the `CarryAnchor`/`SwingAnchors` initializers. Keep `Scale`, `FrameCenter`,
  `CarryHandleOffset`, `SwingHandleOffsets`, `SwingPrefix`, `CarryRegion`, `SwingMove`.
- `MonoGameLearning.Game/Weapons/KnifeWeapon.cs`: delete the `CarryHandAnchor = BatWeapon.CarryHandAnchor`
  line and the anchor derivation; keep `CarryHandleOffset`, `FrameCenter`, `Scale`.
- Seed note (temporary regression guard): the old `BatWeapon.CarryHandAnchor` `(-8, 3)` and
  `SwingHandAnchors` values become the first-pass `idle`/`attack1` entries until the
  Aseprite pivots are pasted, so the first run looks like today.

### 6. Aseprite — author the actor sheet (user step)

New file `MonoGameLearning.Game/Sources/Adventurer/adventurer.aseprite`, 50x37 canvas.

1. Import only the frames the placeholder uses: `idle-00..03`, `attack1-00..03`,
   `attack2-00..03`, `attack3-00..03`, `run-00..05`, `hurt-00..02`, `die-00..06`,
   `fall-00..01`, `stand-00..02` (37 frames).
2. Add frameTags named **exactly** the actor animation keys: `idle`, `attack1`, `attack2`,
   `attack3`, `run`, `hurt`, `die`, `fall`, `getup`. Name the last tag **`getup`** (not
   `stand`) so the exported region prefix matches `AnimationGetUp`; the subsequent region
   names are `adventurer-getup-00..02`.
3. Add a slice named `hand` (the converter matches case-insensitively). Enable **pivot**
   and place it on the actor's gripping hand, one key per frame — every frame of every
   animation, including hurt/fall/die/getup.
4. Export Sprite Sheet → **JSON Array**, `Trim` **off** (fixed 50x37), **Tags** and
   **Slices** checked, overwrite `Sources/Adventurer/adventurer.json` +
   `adventurer-texture.png`.

#### Fallback: scripted sheet assembly via the Aseprite CLI

Hand-assembling 37 frames + 9 tags in the GUI is error-prone. The first manual attempt
produced misordered frames (attack2 first), tag ranges offset from the art, a duplicated
`die-04` with `die-06` missing, a stray untagged frame, and no `idle`/`hurt` at all. If the
manual route drifts again — or is simply unwanted — the assistant can build the sheet by
script, leaving the user only the artistic step (the `hand` slice pivots).

The environment supports this: Aseprite 1.3.18 is on `PATH` with `-b/--batch`, `--script`,
`--save-as`, `--data`, `--format json`, `--list-tags`, and `--list-slices`. Verified in
batch mode: `app.command.NewFile`, `app.open`, `app.command.NewFrame`, and `SaveFileAs`
work; `app.command.ImportSpriteSheet` exists but does **not** yield a sprite in batch, so a
script assembles frames through the Lua object API rather than that command.

Recipe (run only if invoked):

1. Python concatenates the 37 named source PNGs, in canonical order
   (`idle-00..03`, `attack1-00..03`, `attack2-00..03`, `attack3-00..03`, `run-00..05`,
   `hurt-00..02`, `die-00..06`, `fall-00..01`, `stand-00..02`), into a 1850x37 RGBA strip.
2. `aseprite -b --script build_adventurer.lua` creates a 50x37 sprite with one frame per
   strip cell, names the 9 tags with ranges 0-3 / 4-7 / 8-11 / 12-15 / 16-21 / 22-24 /
   25-31 / 32-33 / 34-36 (`idle`, `attack1`, `attack2`, `attack3`, `run`, `hurt`, `die`,
   `fall`, `getup`), and saves `Sources/Adventurer/adventurer.aseprite`.
3. Re-export `adventurer.json` + `adventurer-texture.png` (JSON Array, Trim off, Tags and
   Slices checked) via `aseprite -b adventurer.aseprite --save-as <png> --data <json>
   --format json`.
4. The user opens the generated `.aseprite` in the GUI and adds the `hand` slice pivots.
5. Re-export, and the converter treats it exactly like a hand-authored sheet.

Scripted assembly fixes only the mechanical parts — frame order, frame counts, tag names
and ranges, and export flags; hand pivot positions remain a visual step for the user. The
exact cel/frame fill calls are finalized against the Aseprite object API when the script is
written; an untrimmed JSON Array export is the required output either way. This fallback
touches only `Sources/Adventurer/adventurer.aseprite` and its export, so it never affects
Core or the game code.

### 7. Converter — emit per-animation hand anchor tables

`Utils/aseprite_to_monogame_extended.py`

- Update the module docstring: `handle` = weapon grip (frame-local, untrimmed),
  `hand` = actor hand (center-relative, one key per frame).
- Add `slice_hand_anchors(doc, frame_names, names_by_tag)`:
  - find the `hand` slice (case-insensitive); absent → one-line note, skip;
  - per key compute `(bounds.x + pivot.x - frameW/2, bounds.y + pivot.y - frameH/2)`,
    where the frame size comes from `meta.size` (untrimmed) or that frame's
    `sourceSize` when trimmed;
  - group points by tag slug in source-frame order.
- Add `emit_hand_anchors(...)` printing a paste block:

  ```text
  C# hand anchors (paste into PlayerSprite.cs / EnemySprite.cs; offsets are from the sprite origin):
    new("idle", [ new Vector2(x, y), ... ]),
    new("attack1", [ ... ]),
  ```

  Keys without a pivot or tags without points degrade to a note and are skipped, mirroring
  the `handle` emitter. The atlas JSON output is unchanged.
- Keep the existing `handle`/weapon emitter exactly as-is.

### 8. Game — paste tables, override the resolver, fix the `getup` prefix

- `MonoGameLearning.Game/AnimatedSprites/PlayerSprite.cs`
  - add `public static readonly HandAnchorTable HandAnchors = new(...)` with all 9 pasted
    entries;
  - change the getup def prefix from `adventurer-stand` to `adventurer-getup`.
- `MonoGameLearning.Game/AnimatedSprites/EnemySprite.cs`
  - add `public static readonly HandAnchorTable HandAnchors` with the animations it
    defines (idle, run, attack1, hurt, die, fall, getup) — no unused entries;
  - change the getup def prefix to `adventurer-getup`.
- `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs`: `protected override HandAnchorTable? HandAnchors => PlayerSprite.HandAnchors;`
- `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs`: `protected override HandAnchorTable? HandAnchors => EnemySprite.HandAnchors;`
- Copy the exported `adventurer-texture.png` into `Content/images/` by hand (the converter
  writes only the atlas JSON); `Content.mgcb` already builds `images/adventurer.json` and
  `images/adventurer-texture.png`, so no pipeline entry changes.

### 9. Tests

- `MonoGameLearning.Game.Tests/BatSwingSyncTests.cs`: remove every `ResolveAnchorAndFrame` /
  `SwingAnchors` / `CarryAnchor` / `CarryHandAnchor` / `SwingHandAnchors` assertion. Keep
  lifecycle, facing, region-name, handle-clamp, `HasHandleOffsets`, and hitbox-timing
  tests. Rework the grip-point tests to build the anchor via
  `WeaponDef.ComputeAnchor(hand, ...)` with a hand value from the actor table.
- `MonoGameLearning.Game.Tests/ThrowableWeaponTests.cs`: rewrite the carry-pose assertions
  (lines ~35-38 and ~67-71) to the new formula instead of `knife.CarryAnchor` /
  `ResolveAnchorAndFrame`.
- New `MonoGameLearning.Game.Tests/HandAnchorTableTests.cs`: resolve a known key/frame;
  modulo wrap past the array length; unknown key returns `false` + `Vector2.Zero`; empty
  frame array does not throw.
- New `MonoGameLearning.Game.Tests/ActorHandAnchorTests.cs`: `PlayerSprite.HandAnchors`
  resolves every one of the 9 animation keys for the frame count the `SpriteAnimationDef`s
  declare, and `EnemySprite.HandAnchors` does the same for its 7 — this catches table/art
  count drift.
- `WeaponDef.ComputeAnchor` arithmetic test (pure, headless).
- `Utils/test_aseprite_to_monogame_extended.py`: add a synthetic `hand` slice case —
  center-relative grouping by tag, correct order, and a missing-pivot note that does not
  fail the conversion. Run
  `python3 -m unittest Utils.test_aseprite_to_monogame_extended`.
- Overlay drawing itself stays out of tests (no headless GraphicsDevice); it goes in
  `MANUAL_TESTING.md`.

### 10. Docs and skill check

- `AGENTS.md`: update the `Weapons` bullet (actor owns per-animation hand anchors via a
  `hand` slice + `HandAnchorTable`; weapon owns grip offset/scale; formula; attack2/3 keep
  the carry region but follow the hand now; knockdown/die still unequip) and the `Sprites`
  bullet (getup prefix).
- `LEARNING.md`: update the anchor-ownership pattern entry (split ownership, the one
  formula, `SpriteRenderer.AnimationFrame` vs `Controller.CurrentFrame` (atlas index) vs the
  monotonic `AnimationFrameTracker`).
- `MANUAL_TESTING.md`: rows for the weapon staying in hand while idling, running, hurt,
  and during attack2/attack3; bat swing still tracks the hand; knockdown/die still drop the
  weapon (authored anchors unused).
- `.kilo/skills/create-weapon/SKILL.md`: weapons no longer own actor hand anchors; a weapon
  only needs a `handle` slice, while the actor `hand` slice is a separate actor-art
  workflow. Grep the other skills for `SwingAnchors`/`CarryAnchor`/`CarryHandAnchor` and
  update any that reference them.
- Run the markdown linter on every changed `.md` file.

## Validation

1. `python3 -m unittest Utils.test_aseprite_to_monogame_extended` — converter green.
2. Converter run on `Sources/Adventurer/adventurer.json` prints 9 tag groups with one point
   per frame; paste into `PlayerSprite`/`EnemySprite`.
3. `dotnet build --warnaserror` — zero warnings.
4. `dotnet test` — all green, including the new table/anchor tests.
5. Run the game: weapon stays in hand while idle, running, and hurt; bat swing still tracks
   the hand on attack1; attack2/attack3 carry pose follows the moving arm; knockdown/die
   still drop the weapon (expected until the separate gameplay change).

## Risks

- **Frame-count drift.** Tags must be sized to the counts in the `SpriteAnimationDef`s
  (idle 4, attack1 4, attack2 4, attack3 4, run 6, hurt 3, die 7, fall 2, getup 3). The
  `ActorHandAnchorTests` guard this; a mismatch shows up as a modulo wrap.
- **`getup` naming.** If the tag is left as `stand`, the exported prefix changes and both
  sprite defs must be updated instead. Decide once in step 6 and keep tag slug == key.
- **Trim re-enabled by accident.** A trimmed export introduces `spriteSourceSize` offsets;
  the converter would need the trim-aware half-size path (already planned) and the anchors
  would shift. Keep `Trim` off.
- **Pivot coordinate space.** As in the bat plan, confirm `bounds.origin + pivot` lands on
  the hand pixels on the first export before wiring the game.
- **Tuning is iterative.** First-pass pivots will need a live run; the cyan hand marker and
  the green grip marker make the miss visible.

## Out of scope

- Changing the knockdown/die unequip behavior (anchors are authored but unused there).
- Enemy-specific sprites and enemy-only animations; the Core seam is in place for when they
  arrive.
- New placeholder animations (jump, crouch, slide, …) beyond the 9 in use.
- Interpolating between authored frames.
- Any gameplay change to throw timing, `SpawnFrame`, damage, or projectile flight.
