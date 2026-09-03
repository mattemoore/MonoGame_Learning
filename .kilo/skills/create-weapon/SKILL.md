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

### 1a. User supplies real art

Instruct the user to drop these files into `MonoGameLearning.Game/Content/images/` (relative to that folder):

- `<slug>-texture.png` — the swing animation strip/atlas texture.
- `<slug>.json` — TexturePacker JSON atlas in `monogame-extended` dataformat, with frames named `<slug>-NN` (zero-padded, `NN` starting at `00`) in swing order, one named frame per sheet region. Copy region shapes from `images/bat.json` exactly.
- `<slug>-pickup.png` — a single static pickup icon texture (like `bat-pickup.png`).

Optional but encouraged: keep original sources under `MonoGameLearning.Game/Sources/<Name>/` and pack the atlas with TexturePacker (see `Sources/` examples). The JSON must be generated with `dataformat: monogame-extended` or the pipeline import will fail.

If the user has a `.achj` animation-chain file from their sprite tool instead of a hand-made atlas, run `Utils/achj_to_monogame_extended.py` to produce the `<slug>.json` from it — it maps each chain to a `<slug>-<chain>-NN` frame run, emits the matching `SpriteAnimationDef` lines, and notes any flips/offsets the atlas format can't hold. Remind the user to place the source texture next to the JSON under the referenced name (`<slug>-texture.png`).

WAIT for the user to place the files before continuing. Then validate as the assistant:

- Every filename has no path prefix or spaces — filenames ARE the content keys (`images/<slug>`).
- The JSON parses, lists exactly `FrameCount` frames named `<slug>-<NN>` in swing order, and the texture exists at the referenced width/height.
- Note: if you are replacing an existing weapon's placeholder (e.g. the bat), the user's JSON may change the sheet region layout but MUST keep frame names `<slug>-NN` and the same `FrameCount` (or you update `FrameCount` + `SwingAnchors` + `FrameHitboxes` to match).

### 1b. No art available (default for a new weapon)

Run the companion script (kept in the repo's `Utils/` tools folder) to synthesize a placeholder sheet, JSON atlas, and pickup icon straight into the content folder:

```bash
python3 Utils/placeholder_gen.py <slug> MonoGameLearning.Game/Content/images [frames=N] [frame_w=W] [frame_h=H]
```

Defaults match the bat: 4 frames of 12x40. Tell the user these are temporary placeholders they can replace later (re-run Phase 1/2/6 after swapping in real files, keeping frame count/naming to avoid code churn).

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
    private const int FrameCount = 4;

    private static readonly SpriteSheetAsset Asset = new(
        "pipe", "images/pipe",
        new SpriteAnimationDef(AnimationSwing, "pipe", FrameCount, false));

    public static SpriteSheet Sheet => Asset.Sheet;

    public static void Load(ContentManager content) => Asset.Load(content);

    public static AnimatedSprite Create() => Asset.Create(AnimationSwing);
}
```

The `SpriteSheetAsset` first argument is the `SpriteSheet` display name (used in load error messages); the `Prefix` ("pipe") must match the JSON frame-name prefix, and `FrameCount` must match the JSON frame run exactly. Never fewer `SwingAnchors` than there are swing frames.

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
    private static readonly StaticTextureAsset PickupTexture = new("images/pipe-pickup");
    public static readonly MeleeWeaponDef Pipe = new()
    {
        Name = "Pipe",
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
        SwingAnimation = PipeSprite.AnimationSwing,
        CarryAnchor = new Vector2(20, 0),
        SwingAnchors = [new Vector2(12, -15), new Vector2(25, -4), new Vector2(34, 0), new Vector2(30, -2)],
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

- `SwingMove.AnimationKey` must be one of `PlayerSprite.AnimationAttack*` — the swing overlay is frame-stepped against that animation, and `FrameCount` must equal that animation's frame count (`attack1` = 4).
- `BatWeapon.cs:33` holds the reference anchors; reuse them as a starting point then re-tune in-game.
- Hitboxes fire at swing apex (frames 2–3) only — see the bat pattern.
- `MeleeWeaponDef` TODO item 4 (AGENTS.md): assert `SwingAnchors.Length <= Sheet` frame count in Debug with `Debug.Assert`.
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

- The swing overlay is FRAME-STEPPED (`CombatActorBase.RenderWeaponOverlay` calls `SetFrame`), NOT time-driven — so `FrameCount` and `SwingAnchors` MUST match the player attack animation's frame count, or the overlay desyncs from the arm. `SetFrame` alone does not refresh `TextureRegion` (see AGENTS.md pitfall) — this is handled in `CombatActorBase`, don't add a fix for it.
- The JSON frame names are content keys: `images/<slug>` content path, `<slug>-NN` frame names, and the `SpriteAnimationDef` prefix must all line up, or Content loading throws.
- Do not add a new abstraction for weapon resolution when one switch keyed on `LevelContent.*` in a single `Get` suffices.
- Replacing placeholder art later: keep frame count + naming so only the two PNGs and the JSON change; if the new art's frames differ in count, update `FrameCount`, `SwingAnchors`, and `FrameHitboxes` in the same change and re-run Phase 6.
- The pickup icon is a plain texture (`StaticTextureAsset`), never part of the animation atlas.
