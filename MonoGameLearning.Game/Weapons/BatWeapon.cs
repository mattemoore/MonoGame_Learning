using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.AnimatedSprites;

namespace MonoGameLearning.Game.Weapons;

public static class BatWeapon
{
    private static readonly StaticTextureAsset BatPickupTexture = new("images/bat-pickup");
    public static readonly MeleeWeaponDef Bat = new()
    {
        Name = "Bat",
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
        SwingAnimation = BatSprite.AnimationSwing,
        CarryAnchor = new Vector2(20, 0),
        SwingAnchors = [new Vector2(12, -15), new Vector2(25, -4), new Vector2(34, 0), new Vector2(30, -2)],
    };

    public static MeleeWeaponDef Get(string key) => key switch
    {
        "Bat" => Bat,
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
