---
name: create-weapon
description: Wizard for adding a new melee weapon to MonoGameLearning (or replacing an existing weapon's placeholder art). Walks the user through supplying or generating sprite art, registers the animation-atlas and pickup-icon assets in the content pipeline, creates the sprite/weapon classes, wires GameLoop/factory/level content, then verifies with build + tests. Use when the user wants a new weapon (bat is the reference example) or wants to swap a weapon's placeholder art for real art.
---

# Create Weapon Wizard

Walk the user through adding a melee weapon to this MonoGame project, OR replacing an existing weapon's placeholder art with real art. Drive the wizard interactively — one question at a time, recommending defaults (like the grill-me style). Explore the codebase before proposing so the wizard matches current structure; the bat weapon (`BatWeapon.cs`, `BatSprite.cs`, `bat-texture.png`) is the reference implementation to mirror.

## 0. Identify the goal

Ask the user to pick one scenario (recommend defaults):

- **New weapon, no art yet** — generate placeholder art now (assistant runs the companion `placeholder_gen.py`), user swaps real art in later.
- **New weapon, user has real art** — user places art files, assistant wires the rest.
- **Replace an existing weapon's placeholder/art** (e.g. the bat) — user supplies the new files; helper keeps frame count/naming or updates code and JSON accordingly.

Then gather the weapon spec before touching anything:

1. Display name and asset slug (kebab-case), e.g. `Pipe` → `pipe`. Class names derive from the PascalCase name (`PipeSprite`, `PipeWeapon`).
2. Swing frame count (`FrameCount`). Default 4 — must equal both the player attack animation frame count used by `SwingMove.AnimationKey` (check `PlayerSprite.AnimationAttack1` etc.) and the JSON frame run, and the `SwingAnchors`/hitbox frames must stay inside it.
3. Damage, `AttackStrength` (Light/Medium/Heavy), `AttackSfx`/`ImpactSfx` keys (see `AudioManifest.cs`/`SfxId`).
4. Which `PlayerSprite.AnimationAttack*` the swing overlays (default `AnimationAttack1`).
5. `SwingAnchors` per frame + `CarryAnchor` (defaults from `BatWeapon.cs:33` as a starting point; fine-tune after in-game check).
6. `FrameHitboxes` — frames + `Offset`/`Size` per frame (bat: frames 2–3 only at swing apex).
7. Whether it drops as a pickup, spawns on minions, or both (drives `LevelContent` key + `Level1.cs` spawns + `LevelEntityFactory.CreatePickup`).
8. Cross-check every choice against the ORIGINAL: after wiring, run the game (see Phase 6) and tune anchors, hitboxes, and frame timing visually.

## 1. Art intake

Pick one branch based on Phase 0.

### 1a. User supplies real art as an Aseprite sprite sheet

Ask the user to author the animations in **Aseprite** under `MonoGameLearning.Game/Sources/Weapons/<Name>/`, tagging each animation with an Aseprite **frame tag** (e.g. tag `swing`), then export the sprite sheet (*File → Export Sprite Sheet*) to a `<Name>.json` + `<Name>-texture.png` pair using either the **JSON Array** or **JSON Hash** data format.

1. Run the converter to produce the monogame-extended atlas:

   ```bash
   python3 Utils/aseprite_to_monogame_extended.py Sources/Weapons/<Name>/<Name>.json --name <slug> --out-dir MonoGameLearning.Game/Content/images
   ```

   (or `--texture-name <slug>-texture.png` to rename the referenced art.) The converter handles both Aseprite JSON shapes, derives each tag's frame run directly (overlapping/out-of-range tags are rejected), and prints the exact `SpriteAnimationDef` lines — use the tag-named prefix `<slug>-<tag>` verbatim. Untagged frames become `<slug>-frame-NN`.
2. Verify the emitted `<slug>.json` in Content: `dataformat: monogame-extended`, one `<slug>-<tag>-NN` frame per source frame per tag, single-texture reference. Aseprite `rotated`/trim are preserved so the pipeline writer emits them; the atlas format still cannot express per-frame `flipHorizontal`/`flipDiagonal` or relative offsets, so bake flips into the art (the assistant never regenerates/edits textures). Offsets are sourced from art instead of hand-typed: the user authors a `handle` slice (pivot per frame on the grip) in Aseprite, exports with **Slices** checked in the JSON meta, and the converter prints `CarryHandleOffset`/`SwingHandleOffsets` to paste into the weapon def. Note: Aseprite 1.3.x hides per-frame slice keys behind *Preferences → Editor → Slices → "Prefer slice keyframes (obsolete behavior)"* — without it, slice moves on later frames silently overwrite the single global slice instead of recording a key.
3. Wire the code after conversion: when replacing an existing weapon whose JSON used different naming (`<slug>-NN` or an older chain prefix), update the sprite class `SpriteAnimationDef` Prefix to `<slug>-<tag>` (e.g. `"bat-swing"`) and re-derive `SwingAnchors`. `FrameCount` usually stays.
4. Copy the exported source texture into `MonoGameLearning.Game/Content/images/` under the JSON's filename, replacing any old placeholder (assistant may perform this copy — e.g. `cp Sources/Weapons/Bat/bat-texture.png Content/images/bat-texture.png`). `dotnet build` does NOT validate that the JSON frame regions fit the texture, so only a live in-game run confirms the frames line up.

### 1b. User supplies real art as a packed atlas

Instruct the user to drop these files into `MonoGameLearning.Game/Content/images/` (relative to that folder):

- `<slug>-texture.png` — the swing animation strip/atlas texture.
- `<slug>.json` — TexturePacker JSON atlas in `monogame-extended` dataformat, with frames named `<slug>-NN` (zero-padded, `NN` starting at `00`) in swing order, one named frame per sheet region. Copy region shapes from `images/bat.json` exactly.
- `<slug>-pickup.png` — a single static pickup icon texture (like `bat-pickup.png`).

Optional but encouraged: keep original sources under `MonoGameLearning.Game/Sources/<Name>/` and pack the atlas with TexturePacker (see `Sources/` examples). The JSON must be generated with `dataformat: monogame-extended` or the pipeline import will fail.

### 1c. No art available (default for a new weapon)

Run the companion script (kept in the repo's `Utils/` tools folder) to synthesize a placeholder sheet, JSON atlas, and pickup icon straight into the content folder:

```bash
python3 Utils/placeholder_gen.py <slug> MonoGameLearning.Game/Content/images [frames=N] [frame_w=W] [frame_h=H]
```

Defaults match the bat: 4 frames of 12x40. Tell the user these are temporary placeholders they can replace later (re-run Phase 1/2/6 after swapping in real files, keeping frame count/naming to avoid code churn).

### Validation (all branches)

After placing the files (WAIT for the user), validate as the assistant:

- Every filename has no path prefix or spaces — filenames ARE the content keys (`images/<slug>`).
- The JSON parses, lists exactly `FrameCount` frames named `{slug}-{frame}` in swing order where each animation frame maps to the source runs, and the referenced texture exists at the width/height in the JSON.
- If you are replacing an existing weapon's placeholder (e.g. the bat), the user's JSON may change the sheet region layout but MUST keep the same `FrameCount` (or you update `FrameCount` + `SwingAnchors` + `FrameHitboxes` to match).

## 2. Register assets in the content pipeline

Add the three build entries to `MonoGameLearning.Game/Content/Content.mgcb`, mirroring the bat block at lines 77–104:

```text
#begin images/<slug>.json
/importer:TexturePackerJsonImporter
/processor:TexturePackerProcessor
/build:images/<slug>.json

#begin images/<slug>-texture.png
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=True
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:images/<slug>-texture.png

#begin images/<slug>-pickup.png
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyColor=255,0,255,255
/processorParam:ColorKeyEnabled=True
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:images/<slug>-pickup.png
```

A normal `dotnet build` rebuilds these through `MonoGame.Content.Builder.Task`.

## 3. Create the sprite asset class

Create `MonoGameLearning.Game/AnimatedSprites/<Name>Sprite.cs` mirroring `BatSprite.cs` (single non-looping `"swing"` animation):

```csharp
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.AnimatedSprites;

public static class PipeSprite
{
    public const string AnimationSwing = "swing";
    public const string SwingPrefix = "pipe-swing";
    public const string CarryRegion = "pipe-hold-00";
    private const int FrameCount = 4;

    private static readonly SpriteSheetAsset Asset = new(
        "pipe", "images/pipe",
        new SpriteAnimationDef(AnimationSwing, SwingPrefix, FrameCount, false));

    public static SpriteSheet Sheet => Asset.Sheet;

    public static void Load(ContentManager content) => Asset.Load(content);
}
```

The `SpriteSheetAsset` first argument is the `SpriteSheet` display name (used in load error messages); the `Prefix` (`<slug>-<tag>`, e.g. "pipe-swing") must match the JSON frame-name prefix, and `FrameCount` must match the JSON frame run exactly. Never fewer `SwingAnchors`/`SwingHandleOffsets` than there are swing frames.

## 4. Create the weapon definition class

Create `MonoGameLearning.Game/Weapons/<Name>Weapon.cs` mirroring `BatWeapon.cs`:

```csharp
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Weapons;

public static class PipeWeapon
{
    private const float WeaponScale = 1f;
    private static readonly Vector2 FrameCenter = new(32, 32);

    // Bat-side grip points (frame-local) pasted from the converter's handle-slice output.
    private static readonly Vector2 CarryHandleOffset = new(29, 54);
    private static readonly Vector2[] SwingHandleOffsets =
    [
        new Vector2(46, 16),
        new Vector2(29, 10),
        new Vector2(17, 16),
        new Vector2(8, 30),
    ];

    // Actor-side hand anchors (absolute offsets from Position; not relative to each other).
    private static readonly Vector2 CarryHandAnchor = new(-8, 3);
    private static readonly Vector2[] SwingHandAnchors =
    [
        new Vector2(-8, 3),
        new Vector2(2, 3),
        new Vector2(12, 3),
        new Vector2(22, 3),
    ];

    private static readonly StaticTextureAsset PickupTexture = new("images/pipe-pickup");
    public static readonly MeleeWeaponDef Pipe = new()
    {
        Name = "Pipe",
        Scale = WeaponScale,
        FrameCenter = FrameCenter,
        SwingMove = new()
        {
            AnimationKey = PlayerSprite.AnimationAttack1,
            Damage = 6,
            Strength = AttackStrength.Light,
            AttackSfx = SfxId.AttackSwing1,
            ImpactSfx = SfxId.HitHeavy,
            FrameHitboxes = new()
            {
                [2] = [new() { Offset = new Vector2(45, 0), Size = new Point(70, 45) }],
                [3] = [new() { Offset = new Vector2(45, 0), Size = new Point(70, 45) }],
            }
        },
        SwingPrefix = PipeSprite.SwingPrefix,
        CarryRegion = PipeSprite.CarryRegion,
        CarryHandleOffset = CarryHandleOffset,
        SwingHandleOffsets = SwingHandleOffsets,
        CarryAnchor = CarryHandAnchor - (CarryHandleOffset - FrameCenter) * WeaponScale,
        SwingAnchors =
        [
            SwingHandAnchors[0] - (SwingHandleOffsets[0] - FrameCenter) * WeaponScale,
            SwingHandAnchors[1] - (SwingHandleOffsets[1] - FrameCenter) * WeaponScale,
            SwingHandAnchors[2] - (SwingHandleOffsets[2] - FrameCenter) * WeaponScale,
            SwingHandAnchors[3] - (SwingHandleOffsets[3] - FrameCenter) * WeaponScale,
        ],
    };

    public static MeleeWeaponDef Get(string key) => key switch
    {
        LevelContent.Pipe => Pipe,
        _ => throw new ArgumentException($"Unknown weapon: {key}", nameof(key)),
    };

    public static void Load(ContentManager content)
    {
        PickupTexture.Load(content);
        PipeSprite.Load(content);
        Pipe.Texture = PickupTexture.Texture;
        Pipe.Sheet = PipeSprite.Sheet;
    }
}
```

Key rules from the existing implementation:

- `SwingMove.AnimationKey` must be one of `PlayerSprite.AnimationAttack*` — the swing overlay is keyed against that animation, and `SwingAnchors.Length` must equal that animation's frame count (`attack1` = 4).
- `SwingPrefix`/`CarryRegion` must match the sprite atlas region names (`<slug>-<tag>-NN` from the Aseprite tags) — the overlay resolves regions directly by name via `MeleeWeaponDef.ResolveWeaponRegionName`, never by frame-stepping a sprite.
- Author the grip point in Aseprite: add a slice named `handle` (lowercase; enable its pivot and place it on the hand-grip pixel, one slice key per hold/swing/attack frame), then run the converter and paste the printed `CarryHandleOffset`/`SwingHandleOffsets` into the weapon class. Keep the actor-side `CarryHandAnchor`/`SwingHandAnchors` hand-tuned in the def and compute anchors with the formula `anchor = hand - (handleOffset - FrameCenter) * Scale`, where `Scale` is the weapon's render scale (default 1; bat uses 0.5). Hand anchors are absolute actor-unit offsets from `Position` (not relative to each other). A weapon redraw needs only re-export + re-paste the offsets.
- Hitboxes fire at swing apex (frames 2–3) only — see the bat pattern.
- `MeleeWeaponDef` TODO item 4 (TODO.md): assert `SwingAnchors.Length` against the sheet's swing-region count in Debug with `Debug.Assert`.
- Naming: one static `MeleeWeaponDef` per weapon, referenced via a `Get(string key)` dispatcher and `LevelContent` constant.

## 5. Wire it into the game

Add the `LevelContent` key (so pools/levels can reference it):

```csharp
// MonoGameLearning.Game/Levels/LevelContent.cs
public const string Pipe = "Pipe";
```

Then wire the pieces in this order:

1. `GameLoop.cs`:

   - In `LoadContent()`, after `BatWeapon.Load(Content);`: `PipeWeapon.Load(Content);`
   - Pass the def to the `LevelEntityFactory` constructor (line ~140) as an extra argument.

2. `LevelEntityFactory.cs`:

   - Add the constructor parameter (e.g. `MeleeWeaponDef pipeWeapon`) and store it.
   - Add a case in `CreatePickup`:

     ```csharp
     LevelContent.Pipe => new WeaponPickupEntity(def.Type, def.Position, pipeWeapon),
     ```

3. Weapon resolution dispatcher — extend the existing `getWeapon` delegate. In `GameLoop.InitLevelSystems` it is `BatWeapon.Get`; switch it to a combined dispatcher that resolves every registered weapon key (bat + your new one), OR rename `BatWeapon.Get` to a general `WeaponCatalog.Get` respected by both `GameLoop` and `TestLevelContent`. Whichever you pick, keep ONE switch that keys on `LevelContent.*` so wrenching in future weapons is a single case add.

4. `Level1.cs` (only if the weapon should appear in the level): add a `PickupSpawnDef(LevelContent.Pipe, ...)` to the `Pickups` list, and/or a `Weapon: LevelContent.Pipe` on a wave `EnemySpawnDef`, and/or a `Drops` entry on a prop.

5. Tests — mirror the bat coverage:

   - `LevelDirectorTests.TestLevelContent`: add `LevelContent.Pipe => PipeWeapon.Pipe` to `GetWeapon` and a `Pipe` case to `CreatePickup`.
   - Add weapon-specific tests modeled on `BatSwingSyncTests.cs` and `MeleeWeaponTests.cs` (equip/unequip with and without a Sheet, anchor/frame resolution at rest vs attacking, hitbox registration on the swing frames).
   - If you changed the dispatcher shape, update existing tests to the new `Get`.

## 6. Verify the wizard close

Nothing is done until the game actually shows it:

1. `dotnet build --warnaserror`
2. `dotnet test`
3. Run `dotnet run --project MonoGameLearning.Game/MonoGameLearning.Game.csproj`, equip the weapon from the pickup, and attack. Confirm: the overlay sprite tracks the player arm sweep (no lag/desync), swings apex around attack frames 2–3, hitboxes appear at the right frames, the dropped pickup icon is the `-pickup.png` texture, and facing-left flips correctly.
4. Update `MANUAL_TESTING.md` rows (combat/pickups sections) to include the new weapon so the manual run covers it.

## Pitfalls to watch

- The swing overlay is REGION-KEYED, NOT time-driven: `CombatActorBase.RenderWeaponOverlay` resolves the atlas region by name (`MeleeWeaponDef.ResolveWeaponRegionName` → `SwingPrefix-<NN>` while attacking, `CarryRegion` at rest), so `SwingPrefix`, `SwingAnchors.Length`, and the player attack animation's frame count MUST all line up, or the overlay desyncs from the arm / throws `KeyNotFoundException` at draw time. There is no `SetFrame`/`TextureRegion` workaround needed — don't reintroduce frame-stepping.
- The JSON frame names are content keys: `images/<slug>` content path, `<slug>-<tag>-NN` frame names, and the `SpriteAnimationDef`/`SwingPrefix` must all line up, or Content loading throws.
- Do not add a new abstraction for weapon resolution when one switch keyed on `LevelContent.*` in a single `Get` suffices.
- Replacing placeholder art later: keep frame count + naming so only the two PNGs and the JSON change; if the new art's frames differ in count, update `FrameCount`, `SwingAnchors`, and `FrameHitboxes` in the same change and re-run Phase 6.
- The pickup icon is a plain texture (`StaticTextureAsset`), never part of the animation atlas.
