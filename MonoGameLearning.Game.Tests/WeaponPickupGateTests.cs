using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class WeaponPickupGateTests
{
    private static EntityService CreateManager() =>
        new(CollisionWorldFactory.Create(new RectangleF(0, 0, 2000, 2000)));

    private static PlayerEntityTester CreatePlayer(float x, float y) => new("player", new Vector2(x, y));

    private static WeaponPickupEntity CreateBatPickup(float x, float y) =>
        new(BatWeapon.Bat.Name, new Vector2(x, y), BatWeapon.Bat);

    // --- Gate predicate ---

    [Test]
    public void CanBeCollectedBy_UnarmedWielder_True()
    {
        var pickup = CreateBatPickup(0, 0);
        var player = CreatePlayer(0, 0);

        Assert.That(pickup.CanBeCollectedBy(player), Is.True);
    }

    [Test]
    public void CanBeCollectedBy_ArmedWielder_False()
    {
        var pickup = CreateBatPickup(0, 0);
        var player = CreatePlayer(0, 0);
        player.EquipWeapon(BatWeapon.Bat);

        Assert.That(pickup.CanBeCollectedBy(player), Is.False);
    }

    [Test]
    public void CanBeCollectedBy_NonWielder_True()
    {
        var pickup = CreateBatPickup(0, 0);
        var target = new HealTrackerEntity(100);

        Assert.That(pickup.CanBeCollectedBy(target), Is.True);
    }

    // --- Service integration ---

    [Test]
    public void ResolveOverlaps_ArmedPlayer_LeavesWeaponPickup()
    {
        var mgr = CreateManager();
        var player = CreatePlayer(100, 100);
        player.EquipWeapon(BatWeapon.Bat);
        var pickup = CreateBatPickup(100, 100);
        mgr.Register(player);
        mgr.Register(pickup);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.Multiple(() =>
        {
            Assert.That(mgr.All, Does.Contain(pickup), "An armed actor must not grab a second weapon.");
            Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
        });
    }

    [Test]
    public void ResolveOverlaps_ArmedWithThrowable_LeavesThrowablePickup()
    {
        var mgr = CreateManager();
        var player = CreatePlayer(100, 100);
        player.EquipWeapon(KnifeWeapon.Knife);
        var pickup = new WeaponPickupEntity(KnifeWeapon.Knife.Name, new Vector2(100, 100), KnifeWeapon.Knife);
        mgr.Register(player);
        mgr.Register(pickup);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.Multiple(() =>
        {
            Assert.That(mgr.All, Does.Contain(pickup),
                "An actor armed with a throwable must not grab a second weapon until it is thrown.");
            Assert.That(player.EquippedWeapon, Is.SameAs(KnifeWeapon.Knife));
        });
    }

    [Test]
    public void ResolveOverlaps_UnarmedPlayer_CollectsWeaponPickup()
    {
        var mgr = CreateManager();
        var player = CreatePlayer(100, 100);
        var pickup = CreateBatPickup(100, 100);
        mgr.Register(player);
        mgr.Register(pickup);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.Multiple(() =>
        {
            Assert.That(mgr.All, Does.Not.Contain(pickup));
            Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
        });
    }

    [Test]
    public void ResolveOverlaps_TwoWeaponPickups_UnarmedPlayerCollectsOnlyOne()
    {
        var mgr = CreateManager();
        var player = CreatePlayer(100, 100);
        var first = CreateBatPickup(100, 100);
        var second = CreateBatPickup(105, 105);
        mgr.Register(player);
        mgr.Register(first);
        mgr.Register(second);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        var remaining = mgr.All.OfType<WeaponPickupEntity>().ToList();
        Assert.That(remaining, Has.Count.EqualTo(1), "Only one weapon may be collected per pass.");
    }

    [Test]
    public void ResolveOverlaps_ArmedPlayer_StillCollectsFood()
    {
        var mgr = CreateManager();
        var player = CreatePlayer(100, 100);
        player.EquipWeapon(BatWeapon.Bat);
        var food = new FoodPickupEntity("Apple", new Vector2(100, 100), null!);
        mgr.Register(player);
        mgr.Register(food);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.That(mgr.All, Does.Not.Contain(food), "Food is not a weapon; an armed actor may still eat.");
    }

    [Test]
    public void ResolveOverlaps_AfterThrowConsumed_CollectsWeaponPickup()
    {
        var mgr = CreateManager();
        var player = CreatePlayer(100, 100);
        player.EquipWeapon(KnifeWeapon.Knife);
        player.PrimaryAttack();
        player.SimulateFrameAdvanced(KnifeWeapon.Knife.SpawnFrame);
        Assert.That(player.EquippedWeapon, Is.Null, "A throw consumes the held throwable.");

        var pickup = CreateBatPickup(100, 100);
        mgr.Register(player);
        mgr.Register(pickup);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat),
            "A thrown weapon no longer blocks a pickup.");
    }
}
