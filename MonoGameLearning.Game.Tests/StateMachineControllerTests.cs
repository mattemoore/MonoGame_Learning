using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Player;
using MonoGameLearning.Game.StateMachines;
using NUnit.Framework;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class StateMachineControllerTests
{
    [Test]
    public void Fire_IllegalTrigger_DoesNotThrow_StateUnchanged()
    {
        var controller = EnemyStateMachine.Create();

        Assert.DoesNotThrow(() => controller.Fire(EnemyTrigger.DeathCompleted));
        Assert.That(controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void Fire_IllegalTrigger_Player_DoesNotThrow_StateUnchanged()
    {
        var controller = PlayerStateMachine.Create();

        Assert.DoesNotThrow(() => controller.Fire(PlayerTrigger.DeathCompleted));
        Assert.That(controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void Constructor_InvokesInitialStateEntryCallback()
    {
        bool idleEntryInvoked = false;
        var controller = PlayerStateMachine.Create(new ActorStateMachineCallbacks
        {
            OnIdleEntry = () => idleEntryInvoked = true,
        });

        Assert.That(idleEntryInvoked, Is.True);
        Assert.That(controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void Fire_IgnoredTrigger_IsNoOp()
    {
        var controller = PlayerStateMachine.Create();

        controller.Fire(PlayerTrigger.AttackCompleted);

        Assert.That(controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void CanFire_ReturnsTrue_ForIgnoredTrigger()
    {
        var controller = EnemyStateMachine.Create();
        Assert.That(controller.CanFire(EnemyTrigger.AttackCompleted), Is.True);
    }
}