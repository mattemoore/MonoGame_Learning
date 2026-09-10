using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Weapons;

public static class BatWeapon
{
    // Actor-side weapon handle anchors: where the bat's grip must land, in actor units
    // (unscaled, relative to the actor's Position — NOT relative to each other; the
    // sweep across the swing is authored explicitly). The overlay draws the 64x64
    // region centered at Position + anchor, so for the handle point to land on the
    // anchor: anchor = handAnchor - (handleOffset - FrameCenter) * Scale.
    internal static readonly Vector2 CarryHandAnchor = new(-8, 3);
    internal static readonly Vector2[] SwingHandAnchors =
    [
        new Vector2(-8, 3),
        new Vector2(2, 3),
        new Vector2(12, 3),
        new Vector2(22, 3),
    ];

    private static readonly StaticTextureAsset BatPickupTexture = new("images/bat-pickup");
    public static readonly MeleeWeaponDef Bat = CreateBat();

    private static MeleeWeaponDef CreateBat()
    {
        // Render at half the actor's scale. The anchors fold this in so the grip stays
        // on the hand: anchor = hand - (handleOffset - FrameCenter) * Scale.
        const float weaponScale = 0.5f;
        // Bat regions are 64x64, so the frame center the handle offsets express against
        // is (32, 32). RenderWeaponOverlay asserts region half-size == FrameCenter.
        var frameCenter = new Vector2(32, 32);

        // Bat-side grip point in frame-local pixels, authored as an Aseprite "handle"
        // slice pivot (bounds.origin + pivot) and re-exportable: re-running
        // Utils/aseprite_to_monogame_extended.py after a bat redraw prints fresh values.
        var carryHandleOffset = new Vector2(29, 54); // Hold frame
        Vector2[] swingHandleOffsets =
        [
            new Vector2(46, 16), // bat-swing-00
            new Vector2(29, 10), // bat-swing-01
            new Vector2(17, 16), // bat-swing-02
            new Vector2(8, 30),  // bat-swing-03
        ];

        return new MeleeWeaponDef
        {
            Name = "Bat",
            Scale = weaponScale,
            FrameCenter = frameCenter,
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
            SwingPrefix = BatSprite.SwingPrefix,
            CarryRegion = BatSprite.CarryRegion,
            CarryHandleOffset = carryHandleOffset,
            SwingHandleOffsets = swingHandleOffsets,
            CarryAnchor = CarryHandAnchor - (carryHandleOffset - frameCenter) * weaponScale,
            SwingAnchors =
            [
                SwingHandAnchors[0] - (swingHandleOffsets[0] - frameCenter) * weaponScale,
                SwingHandAnchors[1] - (swingHandleOffsets[1] - frameCenter) * weaponScale,
                SwingHandAnchors[2] - (swingHandleOffsets[2] - frameCenter) * weaponScale,
                SwingHandAnchors[3] - (swingHandleOffsets[3] - frameCenter) * weaponScale,
            ],
        };
    }

    public static MeleeWeaponDef Get(string key) => key switch
    {
        LevelContent.Bat => Bat,
        _ => throw new ArgumentException($"Unknown weapon: {key}", nameof(key)),
    };

    public static void Load(ContentManager content)
    {
        BatPickupTexture.Load(content);
        BatSprite.Load(content);
        Bat.Texture = BatPickupTexture.Texture;
        Bat.Sheet = BatSprite.Sheet;
    }
}
