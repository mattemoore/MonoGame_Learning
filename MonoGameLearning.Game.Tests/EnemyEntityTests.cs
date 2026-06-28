using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities.Helpers;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class EnemyAITests
{
    private const float AttackRange = 70f;
    private const float MinChaseDistance = 60f;
    private const float HalfWidth = 24f;
    private const float HalfHeight = 30f;
    private static readonly RectangleF DefaultBounds = new(0, 0, 2000, 600);

    private static WorldSnapshot WorldWithPlayer(Vector2 playerPos) =>
        new(playerPos, DefaultBounds, [], []);

    // --- Existing tests rewritten to new API ---

    [Test]
    public void Update_OutOfRange_ReturnsStartChase_MovementTowardTarget()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var result = ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(400, 300)), true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StartChase));
        Assert.That(ai.MovementDirection.X, Is.GreaterThan(0));
    }

    [Test]
    public void Update_OutOfRange_TargetOnLeft_ReturnsLeftwardMovement()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var result = ai.Update(new Vector2(400, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(200, 300)), true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StartChase));
        Assert.That(ai.MovementDirection.X, Is.LessThan(0));
    }

    [Test]
    public void Update_InRange_IdleOrChasing_CooldownExpired_ReturnsAttackAfterDelay()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(250, 300)), true, 0.5f);

        var result = ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(250, 300)), true, 0.6f);

        Assert.That(result, Is.EqualTo(AIAction.Attack));
    }

    [Test]
    public void Update_InRange_DelayProgressing_ReturnsStopChase()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var result = ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(250, 300)), true, 0.5f);

        Assert.That(result, Is.EqualTo(AIAction.StopChase));
    }

    [Test]
    public void Update_ChasingInRange_OnCooldown_ReturnsStopChase()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance) { AttackCooldown = 2.0f };
        var result = ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(250, 300)), true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StopChase));
        Assert.That(ai.MovementDirection, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Update_AtMinChaseDistance_StopsMovement()
    {
        var ai = new EnemyAI(AttackRange, minChaseDistance: 100f);
        ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(280, 300)), true, 1.0f);

        Assert.That(ai.MovementDirection, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Update_DirectionUpdateThrottled_OnlyChangesAfterInterval()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var selfPos = new Vector2(200, 300);
        var target = new Vector2(400, 300);

        ai.Update(selfPos, HalfWidth, HalfHeight, WorldWithPlayer(target), true, 0.5f);
        var initialDirection = ai.MovementDirection;

        target = new Vector2(380, 300);
        ai.Update(selfPos, HalfWidth, HalfHeight, WorldWithPlayer(target), true, 0.1f);

        Assert.That(ai.MovementDirection, Is.EqualTo(initialDirection));
    }

    [Test]
    public void Update_FacingChanged_OnlyWhenDirectionSignFlips()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var selfPos = new Vector2(200, 300);

        ai.Update(selfPos, HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(400, 300)), true, 1.0f);
        Assert.That(ai.FacingChanged, Is.True);

        ai.Update(selfPos, HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(400, 300)), true, 1.0f);
        Assert.That(ai.FacingChanged, Is.False);

        ai.Update(selfPos, HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(0, 300)), true, 1.0f);
        Assert.That(ai.FacingChanged, Is.True);
        Assert.That(ai.NewFacingX, Is.LessThan(0));
    }

    [Test]
    public void Update_CooldownDecays()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance) { AttackCooldown = 2.0f };
        ai.Update(Vector2.Zero, HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(400, 300)), false, 0.5f);

        Assert.That(ai.AttackCooldown, Is.EqualTo(1.5f));
    }

    [Test]
    public void Update_AttackCooldownBlocksImmediateReAttack()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance) { AttackCooldown = 1.0f };
        var result = ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, WorldWithPlayer(new Vector2(250, 300)), true, 0.5f);

        Assert.That(result, Is.EqualTo(AIAction.StopChase));
    }

    // --- Steering tests ---

    [Test]
    public void Steer_AvoidsPropAhead()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var prop = new ActorSnapshot(new Vector2(170, 270), 20, 30);
        var world = new WorldSnapshot(new Vector2(300, 300), DefaultBounds, [], new[] { prop });

        var result = ai.Update(new Vector2(100, 300), HalfWidth, HalfHeight, world, true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StartChase));
        Assert.That(MathF.Abs(ai.MovementDirection.Y), Is.GreaterThan(0.01f));
    }

    [Test]
    public void Steer_AvoidsPropOnSide()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        // Enemy at (0,0), player at (200, 0), prop at (70, 40) to the side within AvoidRadius (90)
        var prop = new ActorSnapshot(new Vector2(70, 40), 20, 30);
        var world = new WorldSnapshot(new Vector2(200, 0), DefaultBounds, [], new[] { prop });

        var result = ai.Update(Vector2.Zero, HalfWidth, HalfHeight, world, true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StartChase));
        // Avoidance should produce Y deflection (enemy steered away from prop's side)
        Assert.That(MathF.Abs(ai.MovementDirection.Y), Is.GreaterThan(0.01f));
    }

    [Test]
    public void Steer_SeparationBetweenTwoCloseEnemies()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var other = new ActorSnapshot(new Vector2(200, 320), HalfWidth, HalfHeight);
        var world = new WorldSnapshot(new Vector2(400, 300), DefaultBounds, new[] { other }, []);

        ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, world, true, 1.0f);

        Assert.That(MathF.Abs(ai.MovementDirection.Y), Is.GreaterThan(0.01f));
    }

    [Test]
    public void Steer_NoSeparationBeyondRadius()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var far = new ActorSnapshot(new Vector2(200, 500), HalfWidth, HalfHeight);
        var world = new WorldSnapshot(new Vector2(400, 300), DefaultBounds, new[] { far }, []);

        ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, world, true, 1.0f);

        Assert.That(ai.MovementDirection.X, Is.GreaterThan(0));
        Assert.That(MathF.Abs(ai.MovementDirection.Y), Is.LessThanOrEqualTo(0.01f));
    }

    [Test]
    public void Steer_BoundsForcePushesEnemyInward()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var tightBounds = new RectangleF(0, 0, 100, 600);
        var world = new WorldSnapshot(new Vector2(200, 300), tightBounds, [], []);

        ai.Update(new Vector2(10, 300), HalfWidth, HalfHeight, world, true, 1.0f);

        Assert.That(ai.MovementDirection.X, Is.GreaterThan(0));
    }

    [Test]
    public void Steer_RespectsAttackRange()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var world = WorldWithPlayer(new Vector2(250, 300));

        var result = ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, world, true, 0.5f);

        Assert.That(result, Is.Not.EqualTo(AIAction.StartChase));
    }

    [Test]
    public void Steer_RespectsMinChaseDistance()
    {
        var ai = new EnemyAI(AttackRange, minChaseDistance: 100f);
        var world = WorldWithPlayer(new Vector2(280, 300));

        ai.Update(new Vector2(200, 300), HalfWidth, HalfHeight, world, true, 1.0f);

        Assert.That(ai.MovementDirection, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Steer_ZeroAllocation()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var prop = new ActorSnapshot(new Vector2(170, 270), 20, 30);
        var other = new ActorSnapshot(new Vector2(200, 330), HalfWidth, HalfHeight);
        var world = new WorldSnapshot(
            new Vector2(400, 300),
            DefaultBounds,
            new[] { other },
            new[] { prop });

        for (int i = 0; i < 100; i++)
            ai.Update(new Vector2(200 + i * 0.1f, 300), HalfWidth, HalfHeight, world, true, 0.016f);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            ai.Update(new Vector2(200 + i * 0.1f, 300), HalfWidth, HalfHeight, world, true, 0.016f);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.That(after - before, Is.EqualTo(0),
            "Update should allocate zero bytes per call after warmup");
    }
}