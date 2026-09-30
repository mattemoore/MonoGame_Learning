using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

#pragma warning disable CS9107 // Primary constructor params flow only to the base constructor
internal sealed class PlainPickupEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), IPickup
{
    public void OnPickup(IDamageable target) { }
}

internal sealed class PlainPickupLevelDirector(
    EntityService entityManager, LevelData level, Entity player, Func<PickupSpawnDef, Entity> createPickup)
    : LevelDirector(entityManager, level, player, null!,
        TestLevelContent.CreateProp,
        createPickup,
        TestLevelContent.GetWeapon,
        TestLevelContent.CreateEnemy,
        TestLevelContent.OnEnemySpawned,
        () => TestLevelContent.CameraView)
{
}
#pragma warning restore CS9107

[TestFixture]
public class WeaponDropActorTests
{
    private static readonly RectangleF Bounds = new(0, 0, 2000, 600);

    [Test]
    public void EnemyKnockdown_WhileHoldingWeapon_RaisesDropOnceAndClearsWeapon()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        enemy.EquipWeapon(BatWeapon.Bat);
        var drops = new List<(WeaponDef Weapon, Vector2 Origin, Vector2 Landing)>();
        enemy.WeaponDropped += (weapon, origin, landing) => drops.Add((weapon, origin, landing));

        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        Assert.Multiple(() =>
        {
            Assert.That(drops, Has.Count.EqualTo(1));
            Assert.That(drops[0].Weapon, Is.SameAs(BatWeapon.Bat));
            Assert.That(drops[0].Origin, Is.EqualTo(new Vector2(124, 100)), "Launches ahead of the actor (hands).");
            Assert.That(enemy.EquippedWeapon, Is.Null);
            Assert.That(drops[0].Landing.X, Is.LessThan(enemy.Frame.Left),
                "Lands behind the actor.");
            Assert.That(drops[0].Landing.Y, Is.EqualTo(enemy.Frame.Bottom),
                "Headless fallback: level with the actor's frame bottom.");
        });
    }

    [Test]
    public void Landing_FacingLeft_LaunchesInFrontAndDropsBehind()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        enemy.Direction = FacingDirection.Left;
        enemy.EquipWeapon(BatWeapon.Bat);
        var drops = new List<(WeaponDef Weapon, Vector2 Origin, Vector2 Landing)>();
        enemy.WeaponDropped += (weapon, origin, landing) => drops.Add((weapon, origin, landing));

        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        Assert.Multiple(() =>
        {
            Assert.That(drops, Has.Count.EqualTo(1));
            Assert.That(drops[0].Origin.X, Is.LessThan(enemy.Position.X), "Launches in front when facing left.");
            Assert.That(drops[0].Landing.X, Is.GreaterThan(enemy.Frame.Right), "Lands behind when facing left.");
        });
    }

    [Test]
    public void EnemyKnockdown_SecondKnockdownWithoutWeapon_DoesNotRaise()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        enemy.EquipWeapon(BatWeapon.Bat);
        int dropCount = 0;
        enemy.WeaponDropped += (_, _, _) => dropCount++;

        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });
        // Finish the knockdown so a second one can start.
        enemy.StateController!.Fire(EnemyTrigger.KnockdownCompleted);
        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        Assert.That(dropCount, Is.EqualTo(1));
    }

    [Test]
    public void EnemyDeath_WhileHoldingWeapon_RaisesDropOnce()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        enemy.EquipWeapon(BatWeapon.Bat);
        int dropCount = 0;
        enemy.WeaponDropped += (_, _, _) => dropCount++;

        enemy.TakeDamage(new DamageInfo { Amount = 9999 });

        Assert.Multiple(() =>
        {
            Assert.That(dropCount, Is.EqualTo(1), "DyingEntryImpl must drop the held weapon.");
            Assert.That(enemy.EquippedWeapon, Is.Null);
        });
    }

    [Test]
    public void UnarmedKnockdownAndDeath_RaiseNothing()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        int dropCount = 0;
        enemy.WeaponDropped += (_, _, _) => dropCount++;

        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });
        enemy.StateController!.Fire(EnemyTrigger.KnockdownCompleted);
        enemy.TakeDamage(new DamageInfo { Amount = 9999 });

        Assert.That(dropCount, Is.Zero);
    }

    [Test]
    public void Reset_WithWeaponEquipped_DoesNotRaiseDrop()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        enemy.EquipWeapon(BatWeapon.Bat);
        int dropCount = 0;
        enemy.WeaponDropped += (_, _, _) => dropCount++;

        enemy.Reset(new Vector2(50, 50), null!);

        Assert.Multiple(() =>
        {
            Assert.That(dropCount, Is.Zero, "Pool return / respawn must not spawn a pickup.");
            Assert.That(enemy.EquippedWeapon, Is.Null);
        });
    }

    [Test]
    public void PlayerKnockdown_WhileHoldingWeapon_RaisesDrop()
    {
        var player = new PlayerEntityTester("player", new Vector2(100, 100), 1f);
        player.EquipWeapon(BatWeapon.Bat);
        int dropCount = 0;
        player.WeaponDropped += (_, _, _) => dropCount++;

        player.TakeDamage(new DamageInfo { Amount = 0, Knockdown = true });

        Assert.Multiple(() =>
        {
            Assert.That(dropCount, Is.EqualTo(1));
            Assert.That(player.EquippedWeapon, Is.Null);
        });
    }

    [Test]
    public void PlayerDeath_WhileHoldingWeapon_RaisesDrop()
    {
        var player = new PlayerEntityTester("player", new Vector2(100, 100), 1f);
        player.EquipWeapon(BatWeapon.Bat);
        int dropCount = 0;
        player.WeaponDropped += (_, _, _) => dropCount++;

        player.TakeDamage(new DamageInfo { Amount = 9999 });

        Assert.That(dropCount, Is.EqualTo(1));
    }
}

[TestFixture]
public class WeaponDropArcTests
{
    private static GameTime Elapsed(float seconds) => new(TimeSpan.Zero, TimeSpan.FromSeconds(seconds));

    [Test]
    public void Arc_NotCollectibleInFlight_ThenCollectibleAtRest()
    {
        var pickup = new FoodPickupEntity("Food", new Vector2(100, 100), null!);
        var target = new HealTrackerEntity(100);
        var start = new Vector2(100, 100);
        var end = new Vector2(200, 200);

        pickup.BeginDropArc(start, end, 0.35f, 40f, 1f);

        Assert.Multiple(() =>
        {
            Assert.That(pickup.CanBeCollectedBy(target), Is.False);
            Assert.That(pickup.Position, Is.EqualTo(start));
        });

        pickup.Update(Elapsed(0.1f));
        Assert.Multiple(() =>
        {
            Assert.That(pickup.CanBeCollectedBy(target), Is.False);
            Assert.That(pickup.Position, Is.Not.EqualTo(start));
            Assert.That(pickup.Position, Is.Not.EqualTo(end));
        });

        pickup.Update(Elapsed(0.3f));
        Assert.Multiple(() =>
        {
            Assert.That(pickup.CanBeCollectedBy(target), Is.True);
            Assert.That(pickup.Position, Is.EqualTo(end));
            Assert.That(pickup.Rotation, Is.EqualTo(0f));
        });
    }

    [Test]
    public void PickupService_SkipsUncollectiblePickup_ThenCollectsAfterArc()
    {
        var world = CollisionWorldFactory.Create(new RectangleF(0, 0, 2000, 2000));
        var mgr = new EntityService(world);
        var player = new TestActorForPickup("player", new Vector2(100, 100), 50, 50);
        var pickup = new WeaponPickupEntity("Bat", new Vector2(100, 100), BatWeapon.Bat);
        mgr.Register(player);
        mgr.Register(pickup);

        // Overlapping the whole time, but mid-flight — the gate, not geometry, is under test.
        pickup.BeginDropArc(new Vector2(100, 100), new Vector2(100, 100), 0.35f, 40f, 1f);

        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.That(mgr.All, Does.Contain(pickup), "A pickup mid-arc must not be collected.");

        pickup.Update(Elapsed(0.5f));
        PickupService.ResolveOverlaps(mgr, player, _ => { });
        mgr.ProcessPending();

        Assert.That(mgr.All, Does.Not.Contain(pickup), "A landed, collectible pickup must be collected.");
    }
}

[TestFixture]
public class WeaponDropDirectorTests
{
    private static CollisionWorld2D CreateTestWorld() =>
        CollisionWorldFactory.Create(new RectangleF(0, 0, 2000, 600));

    private static (TestLevelDirector Director, EntityService Manager, Entity Player) Setup(List<EnemySpawnDef> enemies)
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = new TestLevel([new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies: enemies)], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);
        player.Position = new Vector2(300, 0);
        director.Update(new GameTime());
        return (director, mgr, player);
    }

    [Test]
    public void EnemyKnockdown_WhileArmed_SpawnsWeaponPickup_AndEnemySurvives()
    {
        var (director, mgr, _) = Setup(
        [
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle, Weapon: LevelContent.Bat),
        ]);

        var enemy = (EnemyEntity)director.SpawnedEnemies[0];
        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        Assert.Multiple(() =>
        {
            Assert.That(mgr.All.OfType<WeaponPickupEntity>().Count(), Is.EqualTo(1));
            Assert.That(director.ActiveEnemyCount, Is.EqualTo(1), "A knocked-down enemy is not a dead one.");
        });
    }

    [Test]
    public void SpawnWeaponPickup_ArcsFromOriginToGround_ThenEquipsOnPickup()
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = new TestLevel([], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);

        var origin = new Vector2(100, 400);
        var landing = new Vector2(150, 450);
        director.SpawnWeaponPickup(BatWeapon.Bat, origin, landing);

        var pickup = mgr.All.OfType<WeaponPickupEntity>().Single();
        var target = new PlayerEntityTester("player", Vector2.Zero, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(pickup.Position, Is.EqualTo(origin), "The arc starts at the launch point.");
            Assert.That(pickup.CanBeCollectedBy(target), Is.False);
        });

        pickup.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0f)));

        var expectedGround = new Vector2(landing.X, landing.Y - pickup.Height / 2f);
        Assert.Multiple(() =>
        {
            Assert.That(pickup.CanBeCollectedBy(target), Is.True);
            Assert.That(pickup.Position, Is.EqualTo(expectedGround));
        });

        var wielder = new PlayerEntityTester("player", Vector2.Zero, 1f);
        pickup.OnPickup(wielder);
        Assert.That(wielder.EquippedWeapon, Is.SameAs(BatWeapon.Bat), "A landed pickup re-equips the weapon.");
    }

    [Test]
    public void PlayerKnockdown_WhileArmed_DropsCollectiblePickup()
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new PlayerEntityTester("player", new Vector2(100, 100), 1f);
        var level = new TestLevel([], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);
        // Mirrors GameLoop.OnPlayerWeaponDropped (the only Game-side subscription).
        player.WeaponDropped += director.SpawnWeaponPickup;
        player.EquipWeapon(BatWeapon.Bat);

        player.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        var pickup = mgr.All.OfType<WeaponPickupEntity>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(player.EquippedWeapon, Is.Null, "Knockdown drops the held weapon.");
            Assert.That(pickup.CanBeCollectedBy(player), Is.False, "The arc must not be re-grabbed mid-flight.");
        });

        pickup.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0f)));

        Assert.That(pickup.CanBeCollectedBy(player), Is.True,
            "After landing, the now-unarmed player can re-collect the dropped weapon.");
    }

    [Test]
    public void SpawnWeaponPickup_NonPickupBaseFactoryResult_LandsAtGroundHeight()
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = new TestLevel([], endTriggerX: 1500f);
        var director = new PlainPickupLevelDirector(mgr, level, player,
            def => new PlainPickupEntity(def.Type, def.Position, 40, 20));

        var landing = new Vector2(150, 450);
        director.SpawnWeaponPickup(BatWeapon.Bat, new Vector2(100, 400), landing);

        var pickup = mgr.All.OfType<PlainPickupEntity>().Single();
        Assert.That(pickup.Position, Is.EqualTo(new Vector2(landing.X, landing.Y - 10f)));
    }

    [Test]
    public void WeaponName_ResolvesThroughPickupFactory()
    {
        var entity = TestLevelContent.CreatePickup(new PickupSpawnDef(BatWeapon.Bat.Name, Vector2.Zero));
        Assert.That(entity, Is.InstanceOf<WeaponPickupEntity>(),
            "WeaponDef.Name must equal a LevelContent key handled by CreatePickup.");
    }
}
