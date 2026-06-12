using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

public class TestActor(string name, Vector2 position, int width, int height)
    : SpatialEntity(name, position, width, height)
{
    public int Health { get; private set; } = 100;
    public void TakeDamage(int amount) => Health -= amount;
}

[TestFixture]
public class HitboxTests
{
    private static TestActor MakeActor(float x, float y, int size = 50) =>
        new("actor", new Vector2(x, y), size, size);

    private static MoveData MakeTestMove(int damage = 10, Vector2? knockback = null) => new()
    {
        Name = "TestPunch",
        AnimationKey = "attack1",
        Damage = damage,
        Knockback = knockback ?? new Vector2(100, 0),
        FrameHitboxes = new()
        {
            [0] = [new() { Offset = new Vector2(30, 0), Size = new Point(40, 40) }],
        }
    };

    // ==============================================
    // HitboxData.CreateRectangle
    // ==============================================

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

    // ==============================================
    // MoveData.FrameHitboxes lookup
    // ==============================================

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

    // ==============================================
    // HitboxService — hit detection
    // ==============================================

    [Test]
    public void RegisterAndResolve_Hit()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0);
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
        var owner = MakeActor(0, 0);
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
        var owner = MakeActor(0, 0);
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
        var owner = MakeActor(0, 0);
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
        target.TakeDamage(25);
        Assert.That(target.Health, Is.EqualTo(75));
    }

    [Test]
    public void HitboxService_HitAppliesDamage()
    {
        var service = new HitboxService();
        var owner = MakeActor(0, 0);
        var target = MakeActor(35, 0);
        var move = MakeTestMove(damage: 7);

        service.RegisterFrameHitboxes(owner, move, 0, FacingDirection.Right);
        var hits = service.ResolveHits([owner, target]);

        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Damage, Is.EqualTo(7));

        target.TakeDamage(hits[0].Damage);
        Assert.That(target.Health, Is.EqualTo(93));
    }
}