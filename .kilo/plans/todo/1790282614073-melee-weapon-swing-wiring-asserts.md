# Melee weapon swing-run ↔ sheet region wiring guard

- **Origin**: `TODO.md` item 4 ("Add `SwingHandleOffsets.Length` ↔ atlas frame-count wiring asserts").
- **Goal**: A Debug-time guard tying `MeleeWeaponDef.SwingHandleOffsets.Length` (and the carry pose region) to the regions actually present in the weapon's loaded `SpriteSheet`, so a mismatch fails loudly at load with the offending region named — instead of an obscure `KeyNotFoundException` during `Draw`.

## Context / current gap

- `MeleeWeaponDef.ResolveRegion` (`MonoGameLearning.Core/Combat/MeleeWeaponDef.cs:63-71`) clamps the actor frame to `SwingHandleOffsets.Length - 1`, then does `Sheet.TextureAtlas[$"{SwingPrefix}-{frame:00}"]`. `Texture2DAtlas.GetRegion(string)` throws `KeyNotFoundException` when the region is absent.
- `WeaponDef.ResolveCarryRegion` (`MonoGameLearning.Core/Combat/WeaponDef.cs:61-62`) does the same throwing lookup for `CarryRegion`.
- `SpriteSheetAsset.Load` → `DefineFrames` → `SpriteSheetAnimationBuilder.AddFrame(name)` → `Texture2DAtlas.GetIndexOfRegion(name)` returns **-1** for a missing region (it does *not* throw). So the sheet's declared `SpriteAnimationDef` count does **not** validate the atlas: a `bat-swing-03` region missing from the art surfaces only at draw time.
- Existing coverage is the *actor* side only: `ActorHandAnchorTests` (hand table vs `AnimationDefs`), the `Debug.Assert` at `CombatActorBase.cs:166` (actor pose index vs offsets length), and `BatSwingSyncTests` (grip/hand math, region-name formatting, `SwingHandleOffsets.Length == 4`). Nothing ties `SwingHandleOffsets.Length` to the sheet's swing-region run.
- Both weapon sheets attach at load and `WeaponDef.Sheet`'s setter calls `ResetRegionCache()` — the single point where the cache and the sheet are both known:
  - `BatWeapon.Load` → `Bat.Sheet = BatSprite.Sheet` (`MonoGameLearning.Game/Weapons/BatWeapon.cs:75`)
  - `KnifeWeapon.Load` → `Knife.Sheet = KnifeSprite.Sheet` (`MonoGameLearning.Game/Weapons/KnifeWeapon.cs:62`)

## Tasks

### 1. Validate region data when the sheet attaches (Core, `MeleeWeaponDef`)

`ResetRegionCache()` already runs from the `Sheet` setter; add a Debug-only validation there.

```csharp
using System.Diagnostics; // new

protected override void ResetRegionCache()
{
    base.ResetRegionCache();
    _swingRegions = null;
    ValidateRegions();
}

/// <summary>
/// Debug-only guard run when the sheet attaches (Load): every region the weapon looks up by
/// name must exist. ResolveRegion clamps the actor frame to SwingHandleOffsets.Length and then
/// indexes TextureAtlas by name, which throws KeyNotFoundException on a miss — report the
/// mismatch here, at load, with the offending region named.
/// </summary>
[Conditional("DEBUG")]
private void ValidateRegions()
{
    if (Sheet is null) return;
    var atlas = Sheet.TextureAtlas;

    Debug.Assert(CarryRegion is null || atlas.ContainsRegion(CarryRegion),
        $"{Name}: carry region '{CarryRegion}' missing from the sheet");

    if (SwingPrefix is null || SwingHandleOffsets.Length == 0) return;
    for (int i = 0; i < SwingHandleOffsets.Length; i++)
    {
        string region = $"{SwingPrefix}-{i:00}";
        Debug.Assert(atlas.ContainsRegion(region),
            $"{Name}: SwingHandleOffsets has {SwingHandleOffsets.Length} entries but the sheet has no region '{region}'");
    }
}
```

- `[Conditional("DEBUG")]` keeps Release zero-cost (matches the project's Debug-warning convention; note `Debug.Assert` is already DEBUG-only).
- The message names the missing region so an oversized run is obvious.

### 2. Make the region lookups non-throwing (Core)

A `Debug.Assert` prints and continues, so it alone does not stop the draw-time throw. Route misses to `null` so the existing loud path handles them and Release never throws:

- `WeaponDef.ResolveCarryRegion`:
  `Sheet.TextureAtlas[CarryRegion]` → `Sheet.TextureAtlas.TryGetRegion(CarryRegion, out var region) ? region : null`.
- `MeleeWeaponDef.ResolveRegion`:
  `Sheet.TextureAtlas[name]` → `TryGetRegion(name, out var region) ? region : null`, keeping the existing `_swingRegions[frame] ??=` caching for a good region.
- A miss stays `null` and the existing `Debug.Assert(false, "{weapon} weapon is missing SwingPrefix/CarryRegion region mapping")` (`CombatActorBase.cs:176`) reports it and skips the draw.
- GC note: a miss retries one dictionary lookup per draw instead of caching a null sentinel; only reachable on a broken invariant and allocation-free — acceptable (avoids per-weapon "resolved" state).

### 3. Tie the offsets length to the declared swing run (Game, headless-testable)

- `BatSprite`: add `public static SpriteAnimationDef[] AnimationDefs => Asset.Defs;` (mirrors `PlayerSprite`/`EnemySprite`).
- Add to `BatSwingSyncTests`:

```csharp
[Test]
public void Bat_SwingHandleOffsets_MatchDeclaredSheetSwingFrameCount()
{
    var swing = BatSprite.AnimationDefs.Single(d => d.Name == BatSprite.AnimationSwing);
    Assert.That(BatWeapon.Bat.SwingHandleOffsets, Has.Length.EqualTo(swing.FrameCount),
        "An offsets run longer than the sheet's swing regions throws KeyNotFoundException at draw time");
}
```

This is the automated leg (offsets length vs the sheet's declared swing run). The atlas-existence leg needs a `GraphicsDevice` and is covered by the Debug guard + the manual test below; the region-name rule is already covered by `RegionName_Attacking_ReturnsPerFrameSwingRegion` / `..._ClampsToLastSwingRegion`.

### 4. Docs & skill check

- `AGENTS.md` (Weapons section): note that a melee weapon's `SwingHandleOffsets`/`CarryRegion` are validated against the loaded sheet in Debug when `Sheet` attaches.
- `MANUAL_TESTING.md`: add a validation row — remove/rename a `bat-swing-NN` region in the bat atlas, run Debug, confirm a clear assert naming the missing region and that the weapon is not drawn (no crash).
- `.kilo/skills/create-weapon/SKILL.md`: state that `SwingHandleOffsets.Length` must equal the swing `SpriteAnimationDef` frame count (now asserted by `Bat_SwingHandleOffsets_MatchDeclaredSheetSwingFrameCount`) and that the Debug guard validates the regions at load.
- `LEARNING.md`: extend the weapon-overlay pattern's "Where" with the load-time region guard.
- `TODO.md`: remove item 4 (superseded by this plan).
- Run the markdown linter on every created/modified `.md`.

## Validation

1. `dotnet build --warnaserror` — zero warnings (`[Conditional]` and `TryGetRegion` must not add unused-member/nullability warnings).
2. `dotnet test` — all pass, including the new `Bat_SwingHandleOffsets_MatchDeclaredSheetSwingFrameCount` and existing `BatSwingSyncTests` / `ActorHandAnchorTests`.
3. Manual (live, Debug): the new `MANUAL_TESTING.md` row (temporarily break a bat swing region).
4. Markdown lint on changed `.md`.

## Simplification / scope notes

- No new abstraction: the guard reuses the existing `ResetRegionCache` hook and `Texture2DAtlas.ContainsRegion`; no validation class and no duplicate "expected count" field to keep in sync.
- Load-time validation chosen over a per-frame `RenderWeaponOverlay` assert (runs once, earliest point both are known).
- Explicitly NOT doing: a weapon-sheet verifier script (the `verify_adventurer_sheet.py` pattern) — overkill for one melee weapon; revisit if a second melee weapon lands.
- Keeping the `??=` region cache rather than adding a "resolved" flag, to avoid per-weapon state for a broken-invariant path.

## Risks

- `[Conditional("DEBUG")]` on a method called from a virtual `ResetRegionCache` override: the call is elided in Release while the override still compiles — verify with the Release build if convenient.
- `TryGetRegion` miss → a broken weapon silently does not draw in Release while Debug asserts: intended (loud in Debug, degrade instead of crash in Release).
- `ContainsRegion` runs once per offsets entry per `Sheet` assignment — negligible.
