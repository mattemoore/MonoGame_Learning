using MonoGameLearning.Game.Entities.Player;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class PlayerStateTests
{
    private PlayerStateController _controller;

    [SetUp]
    public void Setup() => _controller = new PlayerStateController();

    [Test]
    public void InitialState_ShouldBeIdling() =>
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));

    [Test]
    public void FromIdling_MoveStart_TransitionsToMoving()
    {
        _controller.Fire(PlayerTrigger.MoveStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Moving));
    }

    [Test]
    public void FromIdling_AttackStart_TransitionsToAttacking()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking));
    }

    [Test]
    public void FromMoving_MoveStop_TransitionsToIdling()
    {
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.MoveStop);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void FromMoving_AttackStart_TransitionsToAttacking()
    {
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking));
    }

    [Test]
    public void FromAttacking_AttackCompleted_TransitionsToIdling()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void IsInState_Moving_ReturnsTrue() =>
        AssertSubstateIsInParent(PlayerTrigger.MoveStart);

    private void AssertSubstateIsInParent(PlayerTrigger moveTrigger)
    {
        _controller.Fire(moveTrigger);
        Assert.That(_controller.IsInState(PlayerState.Moving), Is.True);
    }

    [Test]
    public void MoveStart_WhileMoving_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.MoveStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Moving));
    }

    [Test]
    public void WhileAttacking_MovementTriggers_AreIgnored()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.MoveStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking));
    }

    [Test]
    public void WhileAttacking_MoveStop_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.MoveStop);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking));
    }

    [Test]
    public void WhileAttacking_AttackStart_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking));
    }

    [Test]
    public void WhileIdling_MoveStop_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.MoveStop);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void WhileIdling_AttackCompleted_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void InvalidTransition_FromIdling_LeavesStateUnchanged()
    {
        _controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void CanFire_ReturnsTrue_ForValidTransition()
    {
        Assert.That(_controller.CanFire(PlayerTrigger.MoveStart), Is.True);
    }

    [Test]
    public void CanFire_ReturnsTrue_ForIgnoredTrigger()
    {
        Assert.That(_controller.CanFire(PlayerTrigger.AttackCompleted), Is.True);
    }

    [Test]
    public void IgnoredTrigger_DoesNotChangeState()
    {
        _controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void EntryCallbacks_AreInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = new PlayerStateController(new() { OnMovingEntry = () => entryInvoked = true });

        controller.Fire(PlayerTrigger.MoveStart);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void Attacking_IsInAttackingState()
    {
        var controller = new PlayerStateController();
        controller.Fire(PlayerTrigger.AttackStart);
        Assert.That(controller.IsInState(PlayerState.Attacking), Is.True);
    }

    [Test]
    public void FromIdling_TakeDamage_TransitionsToHurt()
    {
        _controller.Fire(PlayerTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void FromMoving_TakeDamage_TransitionsToHurt()
    {
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void FromAttacking_TakeDamage_InterruptsAttack()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void FromHurt_HurtCompleted_TransitionsToIdling()
    {
        _controller.Fire(PlayerTrigger.TakeDamage);
        _controller.Fire(PlayerTrigger.HurtCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void FromHurt_Die_TransitionsToDying()
    {
        _controller.Fire(PlayerTrigger.TakeDamage);
        _controller.Fire(PlayerTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dying));
    }

    [Test]
    public void FromIdling_Die_TransitionsToDying()
    {
        _controller.Fire(PlayerTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dying));
    }

    [Test]
    public void FromMoving_Die_TransitionsToDying()
    {
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dying));
    }

    [Test]
    public void FromAttacking_Die_InterruptsAttack()
    {
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dying));
    }

    [Test]
    public void FromDying_DeathCompleted_TransitionsToDead()
    {
        _controller.Fire(PlayerTrigger.Die);
        _controller.Fire(PlayerTrigger.DeathCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dead));
    }

    [Test]
    public void Dead_IsTerminal_NoTransitionsOut()
    {
        _controller.Fire(PlayerTrigger.Die);
        _controller.Fire(PlayerTrigger.DeathCompleted);

        _controller.Fire(PlayerTrigger.HurtCompleted);
        _controller.Fire(PlayerTrigger.AttackCompleted);
        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.Die);
        _controller.Fire(PlayerTrigger.TakeDamage);

        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dead));
    }

    [Test]
    public void WhileHurt_AttackAndMovementTriggers_AreIgnored()
    {
        _controller.Fire(PlayerTrigger.TakeDamage);
        _controller.Fire(PlayerTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));

        _controller.Fire(PlayerTrigger.MoveStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void WhileDying_AllTriggers_AreIgnored()
    {
        _controller.Fire(PlayerTrigger.Die);

        _controller.Fire(PlayerTrigger.AttackStart);
        _controller.Fire(PlayerTrigger.MoveStart);
        _controller.Fire(PlayerTrigger.TakeDamage);
        _controller.Fire(PlayerTrigger.HurtCompleted);

        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dying));
    }

    [Test]
    public void HurtEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = new PlayerStateController(new() { OnHurtEntry = () => entryInvoked = true });
        controller.Fire(PlayerTrigger.TakeDamage);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void DyingEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = new PlayerStateController(new() { OnDyingEntry = () => entryInvoked = true });
        controller.Fire(PlayerTrigger.Die);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void DeadEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = new PlayerStateController(new()
        {
            OnDyingEntry = () => { },
            OnDeadEntry = () => entryInvoked = true
        });
        controller.Fire(PlayerTrigger.Die);
        controller.Fire(PlayerTrigger.DeathCompleted);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void ExitCallbacks_AreInvoked_OnStateExit()
    {
        bool exitInvoked = false;
        var controller = new PlayerStateController(new()
        {
            OnAttackingEntry = () => { },
            OnAttackingExit = () => exitInvoked = true
        });

        controller.Fire(PlayerTrigger.AttackStart);
        controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(exitInvoked, Is.True);
    }
}