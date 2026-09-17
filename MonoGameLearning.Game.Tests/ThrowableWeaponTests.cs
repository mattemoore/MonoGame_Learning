using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class ThrowableWeaponTests
{
    private static PlayerEntityTester CreatePlayer() => new("Test", Vector2.Zero, 1f);

    private static GameTime Seconds(float seconds) => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    private static ThrowableWeaponDef CreateThrowable(int frameCount, bool loop = true) => new()
    {
        Name = "TestThrowable",
        ThrowMove = KnifeWeapon.Knife.ThrowMove,
        ProjectileRegionPrefix = "test-fly",
        ProjectileFrameCount = frameCount,
        ProjectileLoop = loop,
        Speed = 100f,
        MaxRange = 1000f,
    };

    // --- Weapon model ---

    [Test]
    public void Throwable_ResolvesCarryPose_WhenHeldOrAttacking()
    {
        var knife = KnifeWeapon.Knife;

        Assert.That(knife.ResolveAnchorAndFrame(isAttacking: false, actorFrameIndex: 0).anchor, Is.EqualTo(knife.CarryAnchor));
        Assert.That(knife.ResolveAnchorAndFrame(isAttacking: true, actorFrameIndex: 2).anchor, Is.EqualTo(knife.CarryAnchor));
        Assert.That(knife.ResolveAnchorAndFrame(isAttacking: true, actorFrameIndex: 2).frame, Is.Zero);
        Assert.That(knife.ResolveHandleOffset(isAttacking: true, actorFrameIndex: 2), Is.EqualTo(knife.CarryHandleOffset));
        Assert.That(knife.HasHandleOffsets, Is.True, "The new art exports a Handle slice");
        Assert.That(knife.CarryHandleOffset, Is.EqualTo(new Vector2(15, 26)), "Hold frame: bounds 12,21 + pivot 3,5");
    }

    [Test]
    public void Throwable_ThrowMove_HasNoPlayerHitboxes()
    {
        Assert.That(KnifeWeapon.Knife.ThrowMove.FrameHitboxes, Is.Empty,
            "The projectile lands the hit, not the thrower's animation");
    }

    [Test]
    public void Knife_Damage_OneShotsAnEnemy()
    {
        var enemy = new TestEnemyEntity("Enemy", Vector2.Zero);

        enemy.TakeDamage(new DamageInfo
        {
            Amount = KnifeWeapon.Knife.Damage,
            Knockdown = KnifeWeapon.Knife.Knockdown,
            Strength = KnifeWeapon.Knife.Strength,
            ImpactSfx = KnifeWeapon.Knife.ImpactSfx,
        });

        Assert.That(enemy.IsAlive, Is.False, "A thrown knife must kill an enemy in one hit");
    }

    [Test]
    public void Melee_StillResolvesSwingPosePerFrame()
    {
        var (anchor, frame) = BatWeapon.Bat.ResolveAnchorAndFrame(isAttacking: true, actorFrameIndex: 2);

        Assert.That(anchor, Is.EqualTo(BatWeapon.Bat.SwingAnchors[2]));
        Assert.That(frame, Is.EqualTo(2));
    }

    // --- Pickup ---

    [Test]
    public void WeaponPickup_WithThrowable_EquipsAnyWielder()
    {
        var pickup = new WeaponPickupEntity("Knife", Vector2.Zero, KnifeWeapon.Knife);
        var wielder = new WeaponWielderTrackerEntity();

        pickup.OnPickup(wielder);

        Assert.That(wielder.Equipped, Is.SameAs(KnifeWeapon.Knife));
    }

    // --- Primary attack selection ---

    [Test]
    public void PrimaryAttack_WithThrowable_UsesThrowMove_AndKeepsAttack1Move()
    {
        var player = CreatePlayer();
        var unarmed = player.Attack1Move;
        player.EquipWeapon(KnifeWeapon.Knife);

        player.PrimaryAttack();

        Assert.That(player.CurrentMove, Is.SameAs(KnifeWeapon.Knife.ThrowMove));
        Assert.That(player.Attack1Move, Is.SameAs(unarmed), "A throwable must not swap Attack1Move");
    }

    [Test]
    public void PrimaryAttack_Unarmed_UsesAttack1Move()
    {
        var player = CreatePlayer();

        player.PrimaryAttack();

        Assert.That(player.CurrentMove, Is.SameAs(player.Attack1Move));
    }

    // --- Throw spawn timing ---

    [Test]
    public void Throw_FiresAtSpawnFrame_ExactlyOnce_AndUnequips()
    {
        var player = CreatePlayer();
        player.EquipWeapon(KnifeWeapon.Knife);
        int thrown = 0;
        player.Thrown += (_, _, _) => thrown++;
        player.PrimaryAttack();

        player.SimulateFrameAdvanced(0);
        Assert.That(thrown, Is.Zero, "Must not throw before the spawn frame");

        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);
        Assert.That(thrown, Is.EqualTo(1));
        Assert.That(player.EquippedWeapon, Is.Null, "A throw consumes the weapon");

        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame + 1);
        Assert.That(thrown, Is.EqualTo(1), "Throw must fire exactly once");
    }

    [Test]
    public void ThrowEvent_Origin_IsSpawnOffsetMirroredByFacing()
    {
        var player = CreatePlayer();
        player.Position = new Vector2(100, 200);
        player.Direction = FacingDirection.Left;
        player.EquipWeapon(KnifeWeapon.Knife);
        Vector2 origin = default;
        FacingDirection facing = default;
        int count = 0;
        player.Thrown += (_, o, f) => { origin = o; facing = f; count++; };

        player.PrimaryAttack();
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);

        var offset = KnifeWeapon.Knife.SpawnOffset;
        Assert.That(count, Is.EqualTo(1));
        Assert.That(facing, Is.EqualTo(FacingDirection.Left));
        Assert.That(origin, Is.EqualTo(new Vector2(100 - offset.X, 200 + offset.Y)));
    }

    [Test]
    public void Throw_DoesNotPlayWeaponSwing()
    {
        var player = CreatePlayer();
        player.EquipWeapon(KnifeWeapon.Knife);
        player.PrimaryAttack();
        player.SimulateFrameAdvanced(2);

        Assert.That(player.CurrentMove, Is.SameAs(KnifeWeapon.Knife.ThrowMove), "Must be mid-throw");
        Assert.That(player.WeaponSwingActiveForTest, Is.False,
            "A throwable keeps the carry pose; only a melee SwingMove plays the swing overlay");
    }

    [Test]
    public void Throw_DoesNotFireOnLaterNormalAttack()
    {
        var player = CreatePlayer();
        player.EquipWeapon(KnifeWeapon.Knife);
        int thrown = 0;
        player.Thrown += (_, _, _) => thrown++;
        player.PrimaryAttack();
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);
        Assert.That(thrown, Is.EqualTo(1));

        player.Reset(Vector2.Zero); // back to Idle with no pending throw
        player.PrimaryAttack();
        Assert.That(player.CurrentMove, Is.SameAs(player.Attack1Move));
        for (int frame = 0; frame <= KnifeWeapon.Knife.SpawnFrame + 2; frame++)
            player.SimulateFrameAdvanced(frame);

        Assert.That(thrown, Is.EqualTo(1), "A stale pending throw must not fire on a later punch");
    }

    [Test]
    public void Throw_DoesNotFire_WhenCurrentMoveIsNotTheThrowMove()
    {
        var player = CreatePlayer();
        player.EquipWeapon(KnifeWeapon.Knife);
        int thrown = 0;
        player.Thrown += (_, _, _) => thrown++;
        player.PrimaryAttack();

        // Simulate a mismatched move (the guard that prevents stale throws from firing).
        player.CurrentMove = player.Attack1Move;
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);
        Assert.That(thrown, Is.Zero);

        player.CurrentMove = KnifeWeapon.Knife.ThrowMove;
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);
        Assert.That(thrown, Is.EqualTo(1));
    }

    [Test]
    public void Reset_ClearsPendingThrow()
    {
        var player = CreatePlayer();
        player.EquipWeapon(KnifeWeapon.Knife);
        int thrown = 0;
        player.Thrown += (_, _, _) => thrown++;
        player.PrimaryAttack();

        player.Reset(Vector2.Zero);
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);

        Assert.That(thrown, Is.Zero);
    }

    [Test]
    public void Throw_AfterDifferentWeaponEquipped_KeepsTheNewWeapon()
    {
        var player = CreatePlayer();
        player.EquipWeapon(KnifeWeapon.Knife);
        player.PrimaryAttack();

        // A pickup during the windup replaces the equipped weapon (GameLoop resolves pickups
        // every frame while attacking). The throw must not consume the newly acquired weapon.
        player.EquipWeapon(BatWeapon.Bat);
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);

        Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
    }

    // --- Projectile entity ---

    [Test]
    public void Projectile_MovesInFacingDirection()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(0.01f));

        Assert.That(projectile.Position.X, Is.GreaterThan(0));
        Assert.That(projectile.Position.Y, Is.EqualTo(0));
    }

    [Test]
    public void Projectile_MovesLeftWhenFacingLeft()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Left, Faction.Player);

        projectile.Update(Seconds(0.01f));

        Assert.That(projectile.Position.X, Is.LessThan(0));
    }

    [Test]
    public void Projectile_ExpiresPastMaxRange()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(1f));

        Assert.That(projectile.Expired, Is.True);
    }

    [Test]
    public void Projectile_MarkHit_Expires()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(KnifeWeapon.Knife, Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.MarkHit();

        Assert.That(projectile.Expired, Is.True);
    }

    // --- Projectile flight animation ---

    [Test]
    public void Knife_DeclaresAuthoredFlightRun()
    {
        Assert.That(KnifeWeapon.Knife.ProjectileRegionPrefix, Is.EqualTo(KnifeSprite.FlyPrefix));
        Assert.That(KnifeWeapon.Knife.ProjectileFrameCount, Is.EqualTo(KnifeSprite.FlyFrameCount));
        Assert.That(KnifeWeapon.Knife.SpinDegreesPerSecond, Is.Zero,
            "The authored flight run drives the motion; no extra code rotation");
    }

    [Test]
    public void Projectile_FlightAnimation_AdvancesAndLoops()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(CreateThrowable(frameCount: 3), Vector2.Zero, FacingDirection.Right, Faction.Player);

        Assert.That(projectile.FlightFrameIndex, Is.Zero);
        projectile.Update(Seconds(0.1f));
        Assert.That(projectile.FlightFrameIndex, Is.EqualTo(1));
        projectile.Update(Seconds(0.1f));
        Assert.That(projectile.FlightFrameIndex, Is.EqualTo(2));
        projectile.Update(Seconds(0.1f));
        Assert.That(projectile.FlightFrameIndex, Is.Zero, "A looping run wraps to the first frame");
    }

    [Test]
    public void Projectile_NonLoopingFlightAnimation_HoldsLastFrame()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(CreateThrowable(frameCount: 3, loop: false), Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(1f));

        Assert.That(projectile.FlightFrameIndex, Is.EqualTo(2));
    }

    [Test]
    public void Projectile_SingleFrameRun_DoesNotAdvance()
    {
        var projectile = new ProjectileEntity();
        projectile.Launch(CreateThrowable(frameCount: 1), Vector2.Zero, FacingDirection.Right, Faction.Player);

        projectile.Update(Seconds(1f));

        Assert.That(projectile.FlightFrameIndex, Is.Zero);
    }

    [Test]
    public void Projectile_Relaunch_ResetsFlightFrame()
    {
        var projectile = new ProjectileEntity();
        var def = CreateThrowable(frameCount: 3);
        projectile.Launch(def, Vector2.Zero, FacingDirection.Right, Faction.Player);
        projectile.Update(Seconds(0.25f));
        Assert.That(projectile.FlightFrameIndex, Is.EqualTo(2));

        projectile.Launch(def, Vector2.Zero, FacingDirection.Right, Faction.Player);

        Assert.That(projectile.FlightFrameIndex, Is.Zero);
    }
}
