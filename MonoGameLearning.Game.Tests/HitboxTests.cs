using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Tests;

public class TestSpatialEntity : Entity, ICombatant, ICollisionActor
{
    private readonly Health _health;
    public IShapeF Bounds => Frame;
    public Faction Faction { get; protected set; }
    public int Health => _health.Value;
    public int MaxHealth => _health.MaxHealth;
    public bool IsAlive => _health.IsAlive;
    public event EventHandler Died;

    public TestSpatialEntity(string name, Vector2 position, int width, int height, Faction faction = default)
        : base(name, position, width, height)
    {
        _health = new(100);
        Faction = faction;
    }

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    bool ICombatant.CanTakeDamage() => _health.IsAlive;
    void ICombatant.ReduceHealth(int amount) => _health.Subtract(amount);
    void ICombatant.OnDeath() { }
    void ICombatant.OnKnockdown(DamageInfo info) { }
    void ICombatant.OnHit(DamageInfo info) { }

    public void OnCollision(CollisionEventArgs collisionInfo) { }
}

[TestFixture]
public class HitboxTests
{
    private static TestSpatialEntity MakeActor(float x, float y, int size = 50, Faction faction = Faction.Enemy) =>
        new("actor", new Vector2(x, y), size, size, faction);

    private static MoveData MakeTestMove(int damage = 10) => new()
    {
        Name = "TestPunch",
        AnimationKey = "attack1",
        Damage = damage,
        FrameHitboxes = new()
        {
            [0] = [new() { Offset = new Vector2(30, 0), Size = new Point(40, 40) }],
        }
    };

    [Test]
    public void CreateRectangle_RightFacing()
    {
        var hb = new HitboxData { Offset = new Vector2(30, 0), Size = new Point(40, 40) };
        var rect = hb.CreateRectangle(new Vector2(100, 100), FacingDirection.Right);

        Assert.That(rect.X, Is.EqualTo(110f));
        Assert.That(rect.Y, Is.EqualTo(80f));
        Assert.That(rect.Width, Is.EqualTo(40));
        Assert.That(rect.Height, Is.EqualTo(40));
    }

    [Test]
    public void CreateRectangle_LeftFacing()
    {
        var hb = new HitboxData { Offset = new Vector2(30, 0), Size = new Point(40, 40) };
        var rect = hb.CreateRectangle(new Vector2(100, 100), FacingDirection.Left);

        Assert.That(rect.X, Is.EqualTo(50f));
        Assert.That(rect.Y, Is.EqualTo(80f));
        Assert.That(rect.Width, Is.EqualTo(40));
        Assert.That(rect.Height, Is.EqualTo(40));
    }

    [Test]
    public void FrameHitboxLookup_ValidFrame()
    {
        var move = MakeTestMove();
        var found = move.FrameHitboxes.TryGetValue(0, out var hitboxes);
        Assert.That(found, Is.True);
        Assert.That(hitboxes, Has.Count.EqualTo(1));
    }

    [Test]
    public void FrameHitboxLookup_EmptyFrame()
    {
        var move = MakeTestMove();
        var found = move.FrameHitboxes.TryGetValue(1, out var hitboxes);
        Assert.That(found, Is.False);
        Assert.That(hitboxes, Is.Null);
    }

    [Test]
    public void FrameHitboxLookup_InvalidFrame()
    {
        var move = MakeTestMove();
        var found = move.FrameHitboxes.TryGetValue(99, out var hitboxes);
        Assert.That(found, Is.False);
        Assert.That(hitboxes, Is.Null);
    }

    [Test]
    public void RegisterAndResolve_Hit()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target = MakeActor(35, 0);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Target, Is.EqualTo(target));
        Assert.That(hits[0].Source, Is.EqualTo(owner));
        Assert.That(hits[0].Damage, Is.EqualTo(10));
    }

    [Test]
    public void RegisterAndResolve_NoHit()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0);
        var target = MakeActor(200, 0);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Is.Empty);
    }

    [Test]
    public void NoFriendlyFire()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner]);

        Assert.That(hits, Is.Empty);
    }

    [Test]
    public void DoubleHitPrevention()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target = MakeActor(35, 0);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void ClearsAfterResolve()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target = MakeActor(35, 0);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        service.ResolveHits([owner, target]);
        service.Clear(owner);

        var hits = service.ResolveHits([owner, target]);
        Assert.That(hits, Is.Empty);
    }

    [Test]
    public void MultipleTargets_OnlyOverlappingGetsHit()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target1 = MakeActor(35, 0);
        var target2 = MakeActor(200, 0);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target1, target2]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Target, Is.EqualTo(target1));
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        var target = MakeActor(0, 0, 50);
        target.TakeDamage(new DamageInfo { Amount = 25 });
        Assert.That(target.Health, Is.EqualTo(75));
    }

    [Test]
    public void HitboxService_HitAppliesDamage()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target = MakeActor(35, 0);
        var move = MakeTestMove(damage: 7);

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Damage, Is.EqualTo(7));

        if (hits[0].Target is ICombatant combatant)
            combatant.TakeDamage(new DamageInfo { Amount = hits[0].Damage });
        Assert.That(target.Health, Is.EqualTo(93));
    }

    [Test]
    public void SameFaction_NoHit()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target = MakeActor(35, 0, faction: Faction.Player);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Is.Empty);
    }

    [Test]
    public void CrossFaction_Hits()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var target = MakeActor(35, 0, faction: Faction.Enemy);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void PropTarget_AlwaysHittable()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0, faction: Faction.Player);
        var prop = new TestPropForHit("prop", new Vector2(35, 0), 50, 50);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, prop]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Target, Is.EqualTo(prop));
    }

    [Test]
    public void PerAttackDedup_MultipleFrames_OneHit()
    {
        var service = new HitboxService();
        var player = MakeActor(0, 0, faction: Faction.Player);
        var enemy = MakeActor(35, 0, faction: Faction.Enemy);
        var move = MakeTestMove(damage: 5);

        // Frame 1 of the attack
        service.Clear(player);
        service.RegisterFrameHitboxes(player, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([player, enemy]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Damage, Is.EqualTo(5));

        // Frame 2 of the same attack — same (owner, target) pair should be blocked
        service.Clear(player);
        service.RegisterFrameHitboxes(player, move, 0, FacingDirection.Right);
        hits = service.ResolveHits([player, enemy]);

        Assert.That(hits, Has.Count.EqualTo(0));
    }

    [Test]
    public void PerAttackDedup_DifferentTargets_BothHit()
    {
        var service = new HitboxService();
        var player = MakeActor(0, 0, faction: Faction.Player);
        var enemy1 = MakeActor(35, 0, faction: Faction.Enemy);
        var enemy2 = MakeActor(35, 40, faction: Faction.Enemy);
        var move = MakeTestMove();

        service.RegisterFrameHitboxes(player, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([player, enemy1, enemy2]);

        Assert.That(hits, Has.Count.EqualTo(2));
    }

    [Test]
    public void PerAttackDedup_DifferentOwners_BothHit()
    {
        var service = new HitboxService();
        var player1 = MakeActor(0, 0, faction: Faction.Player);
        var player2 = MakeActor(0, 40, faction: Faction.Player);
        var enemy = MakeActor(35, 0, faction: Faction.Enemy);
        var move = MakeTestMove();

        // Player1 hits enemy
        service.RegisterFrameHitboxes(player1, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([player1, enemy]);
        Assert.That(hits, Has.Count.EqualTo(1));

        // Player2 also hits enemy (different owner, same target)
        service.RegisterFrameHitboxes(player2, move, 0, FacingDirection.Right);
        hits = service.ResolveHits([player2, enemy]);
        Assert.That(hits, Has.Count.EqualTo(1));
    }

    [Test]
    public void PerAttackDedup_ClearedAfterClearAttackResolveState()
    {
        var service = new HitboxService();
        var player = MakeActor(0, 0, faction: Faction.Player);
        var enemy = MakeActor(35, 0, faction: Faction.Enemy);
        var move = MakeTestMove(damage: 5);

        // Frame 1 — hits
        service.Clear(player);
        service.RegisterFrameHitboxes(player, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([player, enemy]);
        Assert.That(hits, Has.Count.EqualTo(1));

        // Clear attack resolve state (simulating attack end / new attack)
        service.ClearAttackResolveState(player);

        // Frame 2 — should hit again because it's a "new" attack
        service.Clear(player);
        service.RegisterFrameHitboxes(player, move, 0, FacingDirection.Right);
        hits = service.ResolveHits([player, enemy]);
        Assert.That(hits, Has.Count.EqualTo(1));
    }

    private class TestPropForHit : Entity, IDamageable, IHasHealth, ICollisionActor
    {
        public IShapeF Bounds => Frame;
        public int Health => 100;
        public int MaxHealth => 100;

        public TestPropForHit(string name, Vector2 position, int width, int height)
            : base(name, position, width, height) { }

        public void TakeDamage(DamageInfo info) { }
        public void OnCollision(CollisionEventArgs collisionInfo) { }
    }
}