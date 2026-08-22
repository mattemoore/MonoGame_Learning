using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

class ArmedActorTester(string name) : PlayerEntityTester(name, Vector2.Zero, 1f)
{
    public AnimatedSprite? WeaponSpriteExposed => base.WeaponSprite;
    public void Equip(MeleeWeaponDef weapon) => EquipWeapon(weapon);
    public void Unequip() => UnequipWeapon();
}

[TestFixture]
public class BatSwingSyncTests
{
    // --- Weapon-sprite lifecycle (headless: sheet is null, sprite stays null, no throw) ---

    [Test]
    public void EquipWeapon_NoSheet_EquipsButNoWeaponSprite()
    {
        var player = new ArmedActorTester("Bat");

        player.Equip(BatWeapon.Bat);

        Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
        Assert.That(player.WeaponSpriteExposed, Is.Null,
            "Without a loaded SpriteSheet the weapon sprite must stay null so headless renders no-op");
    }

    [Test]
    public void UnequipWeapon_ClearsWeaponSprite()
    {
        var player = new ArmedActorTester("Bat");
        player.Equip(BatWeapon.Bat);

        player.Unequip();

        Assert.That(player.EquippedWeapon, Is.Null);
        Assert.That(player.WeaponSpriteExposed, Is.Null);
    }

    // --- Per-frame anchor selection ---

    [Test]
    public void AnchorSelection_NotAttacking_ReturnsCarryAnchor()
    {
        var (anchor, frame) = CombatActorBase.ResolveWeaponAnchorAndFrame(BatWeapon.Bat, isAttacking: false, frameIndex: 0);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.CarryAnchor));
        Assert.That(frame, Is.Zero);
    }

    [Test]
    public void AnchorSelection_Attacking_ReturnsPerFrameAnchor()
    {
        var (anchor, frame) = CombatActorBase.ResolveWeaponAnchorAndFrame(BatWeapon.Bat, isAttacking: true, frameIndex: 2);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.SwingAnchors[2]));
        Assert.That(frame, Is.EqualTo(2));
    }

    [Test]
    public void AnchorSelection_OutOfRangeFrame_ClampsToLastAnchor()
    {
        var (anchor, frame) = CombatActorBase.ResolveWeaponAnchorAndFrame(BatWeapon.Bat, isAttacking: true, frameIndex: 99);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.SwingAnchors[3]));
        Assert.That(frame, Is.EqualTo(3));
    }

    // --- Facing-left mirroring ---

    [Test]
    public void FacingLeft_NegatesAnchorX()
    {
        var anchor = new Vector2(20, 5);

        Assert.That(CombatActorBase.ApplyWeaponFacing(anchor, FacingDirection.Left), Is.EqualTo(new Vector2(-20, 5)));
        Assert.That(CombatActorBase.ApplyWeaponFacing(anchor, FacingDirection.Right), Is.EqualTo(anchor));
    }

    [Test]
    public void FacingLeft_FlipsSpriteHorizontally()
    {
        Assert.That(CombatActorBase.WeaponFacingEffect(FacingDirection.Left), Is.EqualTo(SpriteEffects.FlipHorizontally));
        Assert.That(CombatActorBase.WeaponFacingEffect(FacingDirection.Right), Is.EqualTo(SpriteEffects.None));
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