using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.AnimatedSprites;

namespace MonoGameLearning.Game.Weapons;

public static class KnifeWeapon
{
    // Knife regions are 32x32, so the frame center the handle offsets express against is
    // (16, 16); RenderWeaponOverlay asserts region half-size == FrameCenter.
    private static readonly Vector2 FrameCenter = new(16, 16);

    // Render at half the actor's scale (regions are 32x32), matching the bat's proportions.
    private const float WeaponScale = 0.5f;

    // Actor-side hand anchor for the held pose (absolute actor-unit offset from Position).
    // Shared with BatWeapon.CarryHandAnchor so every held weapon attaches at the same
    // hand point; the knife-side grip offset still differs per weapon.
    internal static readonly Vector2 CarryHandAnchor = BatWeapon.CarryHandAnchor;

    // Knife-side grip point in frame-local pixels from the Hold frame's "Handle" slice
    // (bounds 12,21 + pivot 3,5). Re-export with the slice to refresh.
    private static readonly Vector2 CarryHandleOffset = new(15, 26);

    private static readonly StaticTextureAsset PickupTexture = new("images/knife-pickup");
    public static readonly ThrowableWeaponDef Knife = CreateKnife();

    private static ThrowableWeaponDef CreateKnife() => new()
    {
        Name = "Knife",
        Scale = WeaponScale,
        FrameCenter = FrameCenter,
        CarryRegion = KnifeSprite.HoldRegion,
        CarryHandleOffset = CarryHandleOffset,
        CarryAnchor = CarryHandAnchor - (CarryHandleOffset - FrameCenter) * WeaponScale,
        ThrowMove = new()
        {
            Name = "Throw",
            AnimationKey = PlayerSprite.AnimationAttack1,
            AttackSfx = SfxId.AttackSwing1,
            FrameHitboxes = [],
        },
        SpawnFrame = 1,
        ProjectileHitbox = new() { Offset = Vector2.Zero, Size = new Point(18, 10) },
        Damage = 30, // one-shots a 30-HP enemy
        Knockdown = false,
        Strength = AttackStrength.Light,
        ImpactSfx = SfxId.HitLight,
        ProjectileRegionPrefix = KnifeSprite.FlyPrefix,
        ProjectileFrameCount = KnifeSprite.FlyFrameCount,
        ProjectileFrameDuration = KnifeSprite.FlyFrameDuration,
        ProjectileScale = WeaponScale,
        Speed = 650f,
        MaxRange = 550f,
        // The flight run already animates the spin; no extra code rotation.
        SpinDegreesPerSecond = 0f,
        SpawnOffset = new Vector2(18, -4),
    };

    public static void Load(ContentManager content)
    {
        PickupTexture.Load(content);
        KnifeSprite.Load(content);
        Knife.Texture = PickupTexture.Texture;
        Knife.Sheet = KnifeSprite.Sheet;
    }
}
