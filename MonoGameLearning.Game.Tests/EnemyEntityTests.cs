using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities.Helpers;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class EnemyAITests
{
    private const float AttackRange = 70f;
    private const float MinChaseDistance = 60f;

    [Test]
    public void Update_OutOfRange_ReturnsStartChase_MovementTowardTarget()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var result = ai.Update(Vector2.Zero, new Vector2(200, 0), true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StartChase));
        Assert.That(ai.MovementDirection.X, Is.GreaterThan(0));
    }

    [Test]
    public void Update_OutOfRange_TargetOnLeft_ReturnsLeftwardMovement()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var result = ai.Update(new Vector2(200, 0), Vector2.Zero, true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StartChase));
        Assert.That(ai.MovementDirection.X, Is.LessThan(0));
    }

    [Test]
    public void Update_InRange_IdleOrChasing_CooldownExpired_ReturnsAttackAfterDelay()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        ai.Update(Vector2.Zero, new Vector2(50, 0), true, 0.5f);

        var result = ai.Update(Vector2.Zero, new Vector2(50, 0), true, 0.6f);

        Assert.That(result, Is.EqualTo(AIAction.Attack));
    }

    [Test]
    public void Update_InRange_DelayProgressing_ReturnsStopChase()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var result = ai.Update(Vector2.Zero, new Vector2(50, 0), true, 0.5f);

        Assert.That(result, Is.EqualTo(AIAction.StopChase));
    }

    [Test]
    public void Update_ChasingInRange_OnCooldown_ReturnsStopChase()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance) { AttackCooldown = 2.0f };
        var result = ai.Update(Vector2.Zero, new Vector2(50, 0), true, 1.0f);

        Assert.That(result, Is.EqualTo(AIAction.StopChase));
        Assert.That(ai.MovementDirection, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Update_AtMinChaseDistance_StopsMovement()
    {
        var ai = new EnemyAI(AttackRange, minChaseDistance: 100f);
        ai.Update(Vector2.Zero, new Vector2(80, 0), true, 1.0f);

        Assert.That(ai.MovementDirection, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Update_DirectionUpdateThrottled_OnlyChangesAfterInterval()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var target = new Vector2(200, 0);

        ai.Update(Vector2.Zero, target, true, 0.5f);
        var initialDirection = ai.MovementDirection;

        target = new Vector2(180, 0);
        ai.Update(Vector2.Zero, target, true, 0.1f);

        Assert.That(ai.MovementDirection, Is.EqualTo(initialDirection));
    }

    [Test]
    public void Update_FacingChanged_OnlyWhenDirectionSignFlips()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance);
        var targetRight = new Vector2(200, 0);

        ai.Update(Vector2.Zero, targetRight, true, 1.0f);
        Assert.That(ai.FacingChanged, Is.True);

        ai.Update(Vector2.Zero, targetRight, true, 1.0f);
        Assert.That(ai.FacingChanged, Is.False);

        var targetLeft = new Vector2(-200, 0);
        ai.Update(Vector2.Zero, targetLeft, true, 1.0f);
        Assert.That(ai.FacingChanged, Is.True);
        Assert.That(ai.NewFacingX, Is.LessThan(0));
    }

    [Test]
    public void Update_CooldownDecays()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance) { AttackCooldown = 2.0f };

        ai.Update(Vector2.Zero, new Vector2(200, 0), false, 0.5f);

        Assert.That(ai.AttackCooldown, Is.EqualTo(1.5f));
    }

    [Test]
    public void Update_AttackCooldownBlocksImmediateReAttack()
    {
        var ai = new EnemyAI(AttackRange, MinChaseDistance) { AttackCooldown = 1.0f };
        var result = ai.Update(Vector2.Zero, new Vector2(50, 0), true, 0.5f);

        Assert.That(result, Is.EqualTo(AIAction.StopChase));
    }
}