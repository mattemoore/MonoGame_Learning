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

    // --- Per-frame anchor selection ---

    [Test]
    public void AnchorSelection_NotAttacking_ReturnsCarryAnchor()
    {
        var (anchor, frame) = BatWeapon.Bat.ResolveAnchorAndFrame(isAttacking: false, actorFrameIndex: 0);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.CarryAnchor));
        Assert.That(frame, Is.Zero);
    }

    [Test]
    public void AnchorSelection_Attacking_ReturnsPerFrameAnchor()
    {
        var (anchor, frame) = BatWeapon.Bat.ResolveAnchorAndFrame(isAttacking: true, actorFrameIndex: 2);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.SwingAnchors[2]));
        Assert.That(frame, Is.EqualTo(2));
    }

    [Test]
    public void AnchorSelection_OutOfRangeFrame_ClampsToLastAnchor()
    {
        var (anchor, frame) = BatWeapon.Bat.ResolveAnchorAndFrame(isAttacking: true, actorFrameIndex: 99);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.SwingAnchors[3]));
        Assert.That(frame, Is.EqualTo(3));
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
        var (_, frame) = BatWeapon.Bat.ResolveAnchorAndFrame(isAttacking: true, actorFrameIndex: 2);

        Assert.That(BatWeapon.Bat.ResolveRegion(isAttacking: true, actorFrameIndex: frame), Is.Null);
        Assert.That(BatWeapon.Bat.ResolveRegion(isAttacking: false, actorFrameIndex: frame), Is.Null);
    }

    // --- Anchors derived from Aseprite handle slice + actor hand ---

    [Test]
    public void Anchors_AreSeededFromHandsAndSliceHandleOffsets()
    {
        Assert.That(BatWeapon.Bat.SwingAnchors, Has.Length.EqualTo(4));
        Assert.That(BatWeapon.SwingHandAnchors, Has.Length.EqualTo(4));
        Assert.That(BatWeapon.Bat.SwingHandleOffsets, Has.Length.EqualTo(4));
        Assert.That(BatWeapon.Bat.Scale, Is.EqualTo(0.5f), "Bat renders at half the actor's scale");
        Assert.That(BatWeapon.CarryHandAnchor, Is.EqualTo(BatWeapon.SwingHandAnchors[0]),
            "Carry pose uses the same hand anchor as the first swing frame (both are the forward grip)");
        Assert.That(BatWeapon.SwingHandAnchors, Is.Ordered.Ascending.By("X"),
            "Swing hand anchors must sweep steadily from carry to the right (facing right)");

        var scale = BatWeapon.Bat.Scale;
        var carryExpected = BatWeapon.CarryHandAnchor - (BatWeapon.Bat.CarryHandleOffset - BatWeapon.Bat.FrameCenter) * scale;
        Assert.That(BatWeapon.Bat.CarryAnchor, Is.EqualTo(carryExpected));

        for (int i = 0; i < BatWeapon.SwingHandAnchors.Length; i++)
        {
            var expected = BatWeapon.SwingHandAnchors[i] - (BatWeapon.Bat.SwingHandleOffsets[i] - BatWeapon.Bat.FrameCenter) * scale;
            Assert.That(BatWeapon.Bat.SwingAnchors[i], Is.EqualTo(expected));
        }
    }

    [Test]
    public void Anchors_AreFinite()
    {
        Assert.That(float.IsFinite(BatWeapon.Bat.CarryAnchor.X), Is.True);
        Assert.That(float.IsFinite(BatWeapon.Bat.CarryAnchor.Y), Is.True);
        foreach (var anchor in BatWeapon.Bat.SwingAnchors)
        {
            Assert.That(float.IsFinite(anchor.X), Is.True);
            Assert.That(float.IsFinite(anchor.Y), Is.True);
        }
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
        // 64x64 region, no sheet needed: only the half-size matters for the math.
        var regionHalf = new Vector2(32, 32);
        return MeleeWeaponDef.ComputeHandleScreenPoint(
            Vector2.Zero, FacingDirection.Right,
            weapon.ResolveAnchorAndFrame(attacking, frame).anchor,
            weapon.ResolveHandleOffset(attacking, frame), regionHalf, scale, weaponScale ?? weapon.Scale);
    }

    [Test]
    public void HandlePoint_EqualsCarryHandAnchor_WhenCarried()
    {
        Assert.That(ComputeHandle(BatWeapon.Bat, attacking: false, frame: 0), Is.EqualTo(BatWeapon.CarryHandAnchor));
    }

    [Test]
    public void HandlePoint_EqualsSwingHandAnchor_OnEachSwingFrame()
    {
        for (int i = 0; i < BatWeapon.SwingHandAnchors.Length; i++)
        {
            Assert.That(ComputeHandle(BatWeapon.Bat, attacking: true, frame: i), Is.EqualTo(BatWeapon.SwingHandAnchors[i]),
                $"Swing frame {i} handle must track the actor hand");
        }
    }

    [Test]
    public void HandlePoint_MirrorsHorizontallyWhenFacingLeft()
    {
        var regionHalf = new Vector2(32, 32);
        var anchor = BatWeapon.Bat.ResolveAnchorAndFrame(isAttacking: false, actorFrameIndex: 0).anchor;
        var handleOffset = BatWeapon.Bat.ResolveHandleOffset(isAttacking: false, actorFrameIndex: 0);
        var left = MeleeWeaponDef.ComputeHandleScreenPoint(
            Vector2.Zero, FacingDirection.Left, anchor, handleOffset, regionHalf, 1f, BatWeapon.Bat.Scale);

        Assert.That(left, Is.EqualTo(new Vector2(-BatWeapon.CarryHandAnchor.X, BatWeapon.CarryHandAnchor.Y)));
    }

    [Test]
    public void HandlePoint_ScalesWithActorScale_LikeTheOverlayDraw()
    {
        // Player runs at scale 2; the overlay draw positions the region at anchor*scale and
        // scales the texture by scale*weaponScale. The weapon scale cancels out of the grip-
        // on-hand invariant, so the grip must sit at Position + scale*hand at any weapon scale.
        Assert.That(ComputeHandle(BatWeapon.Bat, attacking: false, frame: 0, scale: 2f),
            Is.EqualTo(BatWeapon.CarryHandAnchor * 2f));
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