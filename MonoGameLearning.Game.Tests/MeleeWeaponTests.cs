using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Tests;

internal sealed class WeaponWielderTrackerEntity : IDamageable, IWeaponWielder
{
    public string Name => "WeaponWielderTracker";
    public Faction Faction => Faction.Player;
    public int Health => 100;
    public int MaxHealth => 100;
    public bool IsAlive => true;
    public event EventHandler Died = delegate { };
    public MeleeWeaponDef? Equipped { get; private set; }

    public void TakeDamage(DamageInfo info) { }
    public void Heal(int amount) { }
    public void EquipWeapon(MeleeWeaponDef weapon) => Equipped = weapon;
    public void UnequipWeapon() => Equipped = null;
}

[TestFixture]
public class MeleeWeaponTests
{
    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 600));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("pickups", new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers("actors", "props");
        world.EnableCollisionBetweenLayers("actors", "pickups");
        return world;
    }

    private static PlayerEntityTester CreatePlayer() => new("Test", Vector2.Zero, 1f);

    private static TestEnemyEntity CreateEnemy() => new("TestEnemy", Vector2.Zero);

    private static MoveData UnarmedPunchMove() => new()
    {
        AnimationKey = "attack1",
        Damage = 5,
        Strength = AttackStrength.Light,
        FrameHitboxes = new()
        {
            [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
        }
    };

    // --- Equipping/swapping ---

    [Test]
    public void EquipWeapon_SwapsPlayerAttack1Move()
    {
        var player = CreatePlayer();
        var unarmed = player.Attack1Move;

        player.EquipWeapon(BatWeapon.Bat);

        Assert.That(player.Attack1Move, Is.SameAs(BatWeapon.Bat.SwingMove));
        Assert.That(player.Attack1Move, Is.Not.SameAs(unarmed));
    }

    [Test]
    public void UnequipWeapon_RestoresPlayerAttack1Move()
    {
        var player = CreatePlayer();
        var unarmed = player.Attack1Move;
        player.EquipWeapon(BatWeapon.Bat);

        player.UnequipWeapon();

        Assert.That(player.Attack1Move, Is.SameAs(unarmed));
    }

    [Test]
    public void EquipWeapon_DoesNotSwapAttack2OrAttack3()
    {
        var player = CreatePlayer();
        var attack2 = player.Attack2Move;
        var attack3 = player.Attack3Move;

        player.EquipWeapon(BatWeapon.Bat);

        Assert.That(player.Attack2Move, Is.SameAs(attack2));
        Assert.That(player.Attack3Move, Is.SameAs(attack3));
    }

    [Test]
    public void UnequipWeapon_SwapsEnemyAttackMoveToDefault()
    {
        var enemy = CreateEnemy();
        var unarmed = enemy.AttackMove;
        enemy.EquipWeapon(BatWeapon.Bat);
        Assert.That(enemy.AttackMove, Is.SameAs(BatWeapon.Bat.SwingMove));

        enemy.UnequipWeapon();

        Assert.That(enemy.AttackMove, Is.SameAs(unarmed));
    }

    // --- Weapon pickup ---

    [Test]
    public void WeaponPickup_OnPickup_EquipsActor()
    {
        var pickup = new WeaponPickupEntity("Bat", Vector2.Zero, BatWeapon.Bat);
        var player = CreatePlayer();

        pickup.OnPickup(player);

        Assert.That(player.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
    }

    [Test]
    public void WeaponPickup_OnPickup_EquipsAnyWeaponWielder()
    {
        var pickup = new WeaponPickupEntity("Bat", Vector2.Zero, BatWeapon.Bat);
        var wielder = new WeaponWielderTrackerEntity();

        pickup.OnPickup(wielder);

        Assert.That(wielder.Equipped, Is.SameAs(BatWeapon.Bat));
    }

    [Test]
    public void WeaponPickup_OnPickup_NonActorTarget_NoOp()
    {
        var pickup = new WeaponPickupEntity("Bat", Vector2.Zero, BatWeapon.Bat);
        var tracker = new HealTrackerEntity(100);

        Assert.DoesNotThrow(() => pickup.OnPickup(tracker));
    }

    // --- Weapon loss on knockdown / reset ---

    [Test]
    public void Knockdown_ClearsWeapon_Player()
    {
        var player = CreatePlayer();
        player.EquipWeapon(BatWeapon.Bat);

        player.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        Assert.That(player.EquippedWeapon, Is.Null);
    }

    [Test]
    public void Knockdown_ClearsWeapon_Enemy()
    {
        var enemy = CreateEnemy();
        enemy.EquipWeapon(BatWeapon.Bat);

        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        Assert.That(enemy.EquippedWeapon, Is.Null);
    }

    [Test]
    public void Reset_ClearsWeapon_Player()
    {
        var player = CreatePlayer();
        player.EquipWeapon(BatWeapon.Bat);

        player.Reset(Vector2.Zero);

        Assert.That(player.EquippedWeapon, Is.Null);
    }

    [Test]
    public void Reset_ClearsWeapon_Enemy()
    {
        var enemy = CreateEnemy();
        enemy.EquipWeapon(BatWeapon.Bat);

        enemy.Reset(Vector2.Zero, null!);

        Assert.That(enemy.EquippedWeapon, Is.Null);
    }

    // --- Armed enemy attacking entry ---

    [Test]
    public void ArmedEnemy_AttackingEntry_UsesWeaponMove()
    {
        var enemy = CreateEnemy();
        enemy.EquipWeapon(BatWeapon.Bat);

        enemy.StateController!.Fire(EnemyTrigger.AttackStart);

        Assert.That(enemy.CurrentMove, Is.SameAs(BatWeapon.Bat.SwingMove));
    }

    [Test]
    public void ArmedAttack_Completes_ReturnsToIdleStillArmed()
    {
        var enemy = CreateEnemy();
        enemy.EquipWeapon(BatWeapon.Bat);

        enemy.StateController!.Fire(EnemyTrigger.AttackStart);
        Assert.That(enemy.StateController!.State, Is.EqualTo(EnemyState.Attacking));

        enemy.StateController!.Fire(EnemyTrigger.AttackCompleted);

        Assert.That(enemy.StateController!.State, Is.EqualTo(EnemyState.Idle));
        Assert.That(enemy.EquippedWeapon, Is.SameAs(BatWeapon.Bat),
            "An armed swing must not consume the weapon");
    }

    // --- Reach integration ---

    [Test]
    public void ArmedSwing_ReachesFurtherThan_UnarmedAttack1()
    {
        var service = new HitboxService();
        var armed = new TestSpatialEntity("armed", Vector2.Zero, 50, 50, Faction.Player);
        var unarmed = new TestSpatialEntity("unarmed", new Vector2(0, 0), 50, 50, Faction.Player);
        var target = new TestSpatialEntity("target", new Vector2(75, 0), 10, 10, Faction.Enemy);

        service.RegisterFrameHitboxes(armed, armed.Faction, BatWeapon.Bat.SwingMove, 2, FacingDirection.Right);
        var armedHits = service.ResolveHits([armed, target]);
        Assert.That(armedHits, Has.Count.EqualTo(1),
            "An armed swing should reach a target at ~75px");

        service.RegisterFrameHitboxes(unarmed, unarmed.Faction, UnarmedPunchMove(), 1, FacingDirection.Right);
        var unarmedHits = service.ResolveHits([unarmed, target]);
        Assert.That(unarmedHits, Is.Empty,
            "An unarmed attack1 at the same reach should miss");
    }

    [Test]
    public void OneSwing_HitsATargetExactlyOnce()
    {
        var service = new HitboxService();
        var owner = new TestSpatialEntity("owner", Vector2.Zero, 50, 50, Faction.Player);
        var target = new TestSpatialEntity("target", new Vector2(75, 0), 10, 10, Faction.Enemy);

        service.RegisterFrameHitboxes(owner, owner.Faction, BatWeapon.Bat.SwingMove, 2, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);
        Assert.That(hits, Has.Count.EqualTo(1));

        service.Clear(owner);
        service.RegisterFrameHitboxes(owner, owner.Faction, BatWeapon.Bat.SwingMove, 3, FacingDirection.Right);
        hits = service.ResolveHits([owner, target]);
        Assert.That(hits, Is.Empty,
            "The same swing's second frame must not re-hit an already-hit target");
    }

    // --- LevelDirector wiring ---

    [Test]
    public void SpawnWave_WithWeaponDef_SpawnsArmedEnemy()
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new TestPlayerEntity("player", Vector2.Zero);
        var level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle, Weapon: "Bat"),
            ])
        ], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);

        player.Position = new Vector2(300, 0);
        director.Update(new GameTime());

        Assert.That(director.SpawnedEnemies, Has.Count.EqualTo(1));
        var enemy = (EnemyEntity)director.SpawnedEnemies[0];
        Assert.That(enemy.EquippedWeapon, Is.SameAs(BatWeapon.Bat));
    }

    [Test]
    public void SpawnWave_UnknownWeapon_Throws()
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new TestPlayerEntity("p", Vector2.Zero);
        var level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle, Weapon: "Pipe"),
            ])
        ], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);

        player.Position = new Vector2(300, 0);

        Assert.Throws<ArgumentException>(() => director.Update(new GameTime()));
    }

    [Test]
    public void CreatePickup_Bat_ReturnsWeaponPickupEntity()
    {
        var mgr = new EntityService(CreateTestWorld());
        var player = new TestPlayerEntity("p", Vector2.Zero);
        var level = new TestLevel(
        [
            new WaveDef(TriggerX: 300f, EndX: 1100f, Enemies:
            [
                new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Middle),
            ])
        ], endTriggerX: 1500f);
        var director = new TestLevelDirector(mgr, level, player);

        director.SpawnPickups([new PickupSpawnDef("Bat", new Vector2(350, 556))]);

        Assert.That(mgr.All.Count(e => e is WeaponPickupEntity), Is.EqualTo(1));
    }
}