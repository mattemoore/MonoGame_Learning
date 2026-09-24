using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

class ArmedActorTester(string name) : PlayerEntityTester(name, Vector2.Zero, 1f)
{
    public void Equip(MeleeWeaponDef weapon) => EquipWeapon(weapon);
    public void Unequip() => UnequipWeapon();
}

[TestFixture]
public class BatSwingSyncTests
{
    // --- Weapon lifecycle (headless: sheet is null, equipping stays safe) ---

    [Test]
    public void EquipWeapon_WithoutSheet_EquipsSafely()
    {
        var player = new ArmedActorTester("Bat");

        player.Equip(BatWeapon.Bat);

        Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
    }

    [Test]
    public void UnequipWeapon_ClearsWeapon()
    {
        var player = new ArmedActorTester("Bat");
        player.Equip(BatWeapon.Bat);

        player.Unequip();

        Assert.That(player.EquippedWeapon, Is.Null);
    }

    // --- Facing-left mirroring ---

    [Test]
    public void FacingLeft_NegatesAnchorX()
    {
        var anchor = new Vector2(20, 5);

        Assert.That(MeleeWeaponDef.ApplyWeaponFacing(anchor, FacingDirection.Left), Is.EqualTo(new Vector2(-20, 5)));
        Assert.That(MeleeWeaponDef.ApplyWeaponFacing(anchor, FacingDirection.Right), Is.EqualTo(anchor));
    }

    [Test]
    public void FacingLeft_FlipsSpriteHorizontally()
    {
        Assert.That(MeleeWeaponDef.WeaponFacingEffect(FacingDirection.Left), Is.EqualTo(SpriteEffects.FlipHorizontally));
        Assert.That(MeleeWeaponDef.WeaponFacingEffect(FacingDirection.Right), Is.EqualTo(SpriteEffects.None));
    }

    // --- Region selection: carried pose vs per-frame swing ---

    [Test]
    public void RegionName_NotAttacking_ReturnsCarryRegion()
    {
        var region = MeleeWeaponDef.ResolveWeaponRegionName(BatWeapon.Bat, isAttacking: false, actorFrameIndex: 0);

        Assert.That(region, Is.EqualTo(BatWeapon.Bat.CarryRegion));
        Assert.That(region, Is.EqualTo(BatSprite.CarryRegion), "Carry region must come from the sprite's atlas naming");
    }

    [Test]
    public void RegionName_Attacking_ReturnsPerFrameSwingRegion()
    {
        var region = MeleeWeaponDef.ResolveWeaponRegionName(BatWeapon.Bat, isAttacking: true, actorFrameIndex: 2);

        Assert.That(region, Is.EqualTo($"{BatWeapon.Bat.SwingPrefix}-02"));
    }

    [Test]
    public void RegionName_AttackingOutOfRange_ClampsToLastSwingRegion()
    {
        var region = MeleeWeaponDef.ResolveWeaponRegionName(BatWeapon.Bat, isAttacking: true, actorFrameIndex: 99);

        Assert.That(region, Is.EqualTo($"{BatWeapon.Bat.SwingPrefix}-03"));
    }

    [Test]
    public void RegionName_MissingSwingPrefix_ReturnsNull()
    {
        var bare = new MeleeWeaponDef { Name = "Bare", SwingMove = new() { AnimationKey = "attack" } };
        var region = MeleeWeaponDef.ResolveWeaponRegionName(bare, isAttacking: true, actorFrameIndex: 0);

        Assert.That(region, Is.Null, "Weapon with no swing regions must not resolve a swing frame region");
    }

    [Test]
    public void ResolveWeaponRegion_WithoutSheet_ReturnsNull()
    {
        Assert.That(BatWeapon.Bat.ResolveRegion(isAttacking: true, actorFrameIndex: 2), Is.Null);
        Assert.That(BatWeapon.Bat.ResolveRegion(isAttacking: false, actorFrameIndex: 2), Is.Null);
    }

    // --- Weapon carries only its own grip data; the actor owns the hand point ---

    [Test]
    public void Bat_CarriesGripOffsets_NotActorHandAnchors()
    {
        Assert.That(BatWeapon.Bat.SwingHandleOffsets, Has.Length.EqualTo(4));
        Assert.That(BatWeapon.Bat.Scale, Is.EqualTo(0.5f), "Bat renders at half the actor's scale");
        Assert.That(BatWeapon.Bat.FrameCenter, Is.EqualTo(new Vector2(32, 32)));
    }

    [Test]
    public void ComputeAnchor_PlacesGripOnHand()
    {
        var weapon = BatWeapon.Bat;
        Assert.That(PlayerSprite.HandAnchors.TryResolve(PlayerSprite.AnimationAttack1, 2, out var hand), Is.True);
        var handleOffset = weapon.ResolveHandleOffset(isAttacking: true, actorFrameIndex: 2);

        var anchor = WeaponDef.ComputeAnchor(hand, handleOffset, weapon.FrameCenter, weapon.Scale);

        var grip = WeaponDef.ComputeHandleScreenPoint(
            Vector2.Zero, FacingDirection.Right, anchor, handleOffset, weapon.FrameCenter, 1f, weapon.Scale);
        Assert.That(grip, Is.EqualTo(hand), "A grip offset that lands on FrameCenter must sit exactly on the hand");
    }

    [Test]
    public void ComputeAnchor_WithNoHandleOffset_IsTheHand()
    {
        var hand = new Vector2(7, -3);

        Assert.That(WeaponDef.ComputeAnchor(hand, new Vector2(16, 16), new Vector2(16, 16), 0.5f), Is.EqualTo(hand));
    }

    [Test]
    public void HandleOffsets_StayWithinFrameBounds()
    {
        const int frameSize = 64;
        bool InFrame(Vector2 v) =>
            v.X >= 0 && v.Y >= 0 && v.X <= frameSize && v.Y <= frameSize;

        Assert.That(InFrame(BatWeapon.Bat.CarryHandleOffset), Is.True, "Carry handle must sit in the 64x64 frame");
        foreach (var offset in BatWeapon.Bat.SwingHandleOffsets)
            Assert.That(InFrame(offset), Is.True, "Swing handle must sit in the 64x64 frame");
    }

    [Test]
    public void ResolveHandleOffset_UsesCarryWhenNotAttacking_ClampsWhenAttacking()
    {
        Assert.That(BatWeapon.Bat.ResolveHandleOffset(isAttacking: false, actorFrameIndex: 0),
            Is.EqualTo(BatWeapon.Bat.CarryHandleOffset));

        Assert.That(BatWeapon.Bat.ResolveHandleOffset(isAttacking: true, actorFrameIndex: 99),
            Is.EqualTo(BatWeapon.Bat.SwingHandleOffsets[^1]),
            "Out-of-range swing frame must clamp to the last swing handle offset");
    }

    [Test]
    public void HasHandleOffsets_FalseWithoutSliceData()
    {
        var bare = new MeleeWeaponDef { Name = "Bare", SwingMove = new() { AnimationKey = "attack" } };
        Assert.That(bare.HasHandleOffsets, Is.False);
        Assert.That(BatWeapon.Bat.HasHandleOffsets, Is.True);
    }

    // --- Handle grip point must land on the actor hand (on-sprite invariant) ---

    private static Vector2 ComputeHandle(MeleeWeaponDef weapon, bool attacking, int frame, float scale = 1f, float? weaponScale = null)
    {
        // 64x64 bat region: regionHalf == FrameCenter, no sheet needed for the math.
        var attackKey = attacking ? PlayerSprite.AnimationAttack1 : PlayerSprite.AnimationIdle;
        PlayerSprite.HandAnchors.TryResolve(attackKey, frame, out var hand);
        var handleOffset = weapon.ResolveHandleOffset(attacking, frame);
        var anchor = WeaponDef.ComputeAnchor(hand, handleOffset, weapon.FrameCenter, weapon.Scale);
        return WeaponDef.ComputeHandleScreenPoint(
            Vector2.Zero, FacingDirection.Right, anchor, handleOffset, weapon.FrameCenter, scale, weaponScale ?? weapon.Scale);
    }

    [Test]
    public void HandlePoint_EqualsActorHand_WhenCarried()
    {
        PlayerSprite.HandAnchors.TryResolve(PlayerSprite.AnimationIdle, 0, out var hand);

        Assert.That(ComputeHandle(BatWeapon.Bat, attacking: false, frame: 0), Is.EqualTo(hand));
    }

    [Test]
    public void HandlePoint_EqualsActorHand_OnEachSwingFrame()
    {
        for (int i = 0; i < BatWeapon.Bat.SwingHandleOffsets.Length; i++)
        {
            PlayerSprite.HandAnchors.TryResolve(PlayerSprite.AnimationAttack1, i, out var hand);
            Assert.That(ComputeHandle(BatWeapon.Bat, attacking: true, frame: i), Is.EqualTo(hand),
                $"Swing frame {i} grip must track the actor hand");
        }
    }

    [Test]
    public void HandlePoint_MirrorsHorizontallyWhenFacingLeft()
    {
        PlayerSprite.HandAnchors.TryResolve(PlayerSprite.AnimationIdle, 0, out var hand);
        var handleOffset = BatWeapon.Bat.ResolveHandleOffset(isAttacking: false, actorFrameIndex: 0);
        var anchor = WeaponDef.ComputeAnchor(hand, handleOffset, BatWeapon.Bat.FrameCenter, BatWeapon.Bat.Scale);
        var left = MeleeWeaponDef.ComputeHandleScreenPoint(
            Vector2.Zero, FacingDirection.Left, anchor, handleOffset, BatWeapon.Bat.FrameCenter, 1f, BatWeapon.Bat.Scale);

        Assert.That(left, Is.EqualTo(new Vector2(-hand.X, hand.Y)));
    }

    [Test]
    public void HandlePoint_ScalesWithActorScale_LikeTheOverlayDraw()
    {
        // Player runs at scale 2; the overlay draw positions the region at anchor*scale and
        // scales the texture by scale*weaponScale. The weapon scale cancels out of the grip-
        // on-hand invariant, so the grip must sit at Position + scale*hand at any weapon scale.
        PlayerSprite.HandAnchors.TryResolve(PlayerSprite.AnimationIdle, 0, out var hand);

        Assert.That(ComputeHandle(BatWeapon.Bat, attacking: false, frame: 0, scale: 2f), Is.EqualTo(hand * 2f));
    }

    // --- Apex-only hitbox timing ---

    [Test]
    public void Hitbox_ApexOnly_LateFramesHit_EarlyFramesMiss()
    {
        var service = new HitboxService();
        bool FrameHits(int frame)
        {
            var owner = new TestSpatialEntity("owner", Vector2.Zero, 50, 50, Faction.Player);
            var target = new TestSpatialEntity("target", new Vector2(75, 0), 10, 10, Faction.Enemy);
            service.RegisterFrameHitboxes(owner, owner.Faction, BatWeapon.Bat.SwingMove, frame, FacingDirection.Right);
            var hits = service.ResolveHits([owner, target]);
            service.ClearAll();
            return hits.Count == 1;
        }

        Assert.That(FrameHits(2), Is.True, "Swing apex (frame 2) should connect");
        Assert.That(FrameHits(3), Is.True, "Swing follow-through (frame 3) should connect");
        Assert.That(FrameHits(0), Is.False, "Swing wind-up (frame 0) must not hit");
        Assert.That(FrameHits(1), Is.False, "Swing wind-up (frame 1) must not hit");
    }
}