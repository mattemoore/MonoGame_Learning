using MonoGameLearning.Game.Entities.Player;
using NUnit.Framework;

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
    public void FromIdling_Attack1Start_TransitionsToAttacking1()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking1));
    }

    [Test]
    public void FromIdling_Attack2Start_TransitionsToAttacking2()
    {
        _controller.Fire(PlayerTrigger.Attack2Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking2));
    }

    [Test]
    public void FromIdling_Attack3Start_TransitionsToAttacking3()
    {
        _controller.Fire(PlayerTrigger.Attack3Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking3));
    }

    [Test]
    public void FromIdling_MoveLeftStart_TransitionsToMovingLeft()
    {
        _controller.Fire(PlayerTrigger.MoveLeftStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingLeft));
    }

    [Test]
    public void FromIdling_MoveRightStart_TransitionsToMovingRight()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingRight));
    }

    [Test]
    public void FromIdling_MoveUpStart_TransitionsToMovingUp()
    {
        _controller.Fire(PlayerTrigger.MoveUpStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingUp));
    }

    [Test]
    public void FromIdling_MoveDownStart_TransitionsToMovingDown()
    {
        _controller.Fire(PlayerTrigger.MoveDownStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingDown));
    }

    [Test]
    public void FromMoving_MoveStop_TransitionsToIdling()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.MoveStop);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void FromMoving_Attack1Start_TransitionsToAttacking1()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.Attack1Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking1));
    }

    [Test]
    public void FromMoving_Attack2Start_TransitionsToAttacking2()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.Attack2Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking2));
    }

    [Test]
    public void FromMoving_Attack3Start_TransitionsToAttacking3()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.Attack3Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking3));
    }

    [Test]
    public void FromAttacking_AttackCompleted_TransitionsToIdling()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
        _controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Idling));
    }

    [Test]
    public void Substate_MovingLeft_IsInStateMoving() =>
        AssertSubstateIsInParent(PlayerTrigger.MoveLeftStart);

    [Test]
    public void Substate_MovingRight_IsInStateMoving() =>
        AssertSubstateIsInParent(PlayerTrigger.MoveRightStart);

    [Test]
    public void Substate_MovingUp_IsInStateMoving() =>
        AssertSubstateIsInParent(PlayerTrigger.MoveUpStart);

    [Test]
    public void Substate_MovingDown_IsInStateMoving() =>
        AssertSubstateIsInParent(PlayerTrigger.MoveDownStart);

    private void AssertSubstateIsInParent(PlayerTrigger moveTrigger)
    {
        _controller.Fire(moveTrigger);
        Assert.That(_controller.IsInState(PlayerState.Moving), Is.True);
    }

    [Test]
    public void Substate_MovingLeftToMovingRight_TransitionsDirection()
    {
        _controller.Fire(PlayerTrigger.MoveLeftStart);
        _controller.Fire(PlayerTrigger.MoveRightStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingRight));
    }

    [Test]
    public void Substate_MovingRightToMovingLeft_TransitionsDirection()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.MoveLeftStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingLeft));
    }

    [Test]
    public void Substate_Moving_CyclesThroughAllDirections()
    {
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.MoveUpStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingUp));

        _controller.Fire(PlayerTrigger.MoveLeftStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingLeft));

        _controller.Fire(PlayerTrigger.MoveDownStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingDown));
    }

    [Test]
    public void SameDirection_FromMovingSubstate_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.MoveLeftStart);
        _controller.Fire(PlayerTrigger.MoveLeftStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.MovingLeft));
    }

    [Test]
    public void WhileAttacking_MovementTriggers_AreIgnored()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
        _controller.Fire(PlayerTrigger.MoveRightStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking1));
    }

    [Test]
    public void WhileAttacking_MoveStop_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
        _controller.Fire(PlayerTrigger.MoveStop);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking1));
    }

    [Test]
    public void WhileAttacking_AttackTrigger_IsIgnored()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
        _controller.Fire(PlayerTrigger.Attack2Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Attacking1));
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
        Assert.That(_controller.CanFire(PlayerTrigger.MoveLeftStart), Is.True);
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
        var controller = new PlayerStateController(new() { OnMovingRightEntry = () => entryInvoked = true });

        controller.Fire(PlayerTrigger.MoveRightStart);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void ExitCallbacks_AreInvoked_OnStateExit()
    {
        bool exitInvoked = false;
        var controller = new PlayerStateController(new() { OnAttacking1Exit = () => exitInvoked = true });

        controller.Fire(PlayerTrigger.Attack1Start);
        controller.Fire(PlayerTrigger.AttackCompleted);
        Assert.That(exitInvoked, Is.True);
    }

    [Test]
    public void SubstateCallbacks_CanBeDistinct_PerDirection()
    {
        bool leftInvoked = false, rightInvoked = false;
        var controller = new PlayerStateController(new()
        {
            OnMovingLeftEntry = () => leftInvoked = true,
            OnMovingRightEntry = () => rightInvoked = true
        });

        controller.Fire(PlayerTrigger.MoveLeftStart);
        Assert.That(leftInvoked, Is.True);
        Assert.That(rightInvoked, Is.False);

        controller.Fire(PlayerTrigger.MoveRightStart);
        Assert.That(rightInvoked, Is.True);
    }

    [Test]
    public void Attacking_Substates_EachHaveDistinctTriggers()
    {
        var controller = new PlayerStateController();

        controller.Fire(PlayerTrigger.Attack1Start);
        Assert.That(controller.State, Is.EqualTo(PlayerState.Attacking1));

        controller.Fire(PlayerTrigger.AttackCompleted);

        controller.Fire(PlayerTrigger.Attack2Start);
        Assert.That(controller.State, Is.EqualTo(PlayerState.Attacking2));

        controller.Fire(PlayerTrigger.AttackCompleted);

        controller.Fire(PlayerTrigger.Attack3Start);
        Assert.That(controller.State, Is.EqualTo(PlayerState.Attacking3));
    }

    [Test]
    public void Attacking_Substates_AreInAttackingState()
    {
        var controller = new PlayerStateController();
        controller.Fire(PlayerTrigger.Attack1Start);
        Assert.That(controller.IsInState(PlayerState.Attacking), Is.True);

        controller.Fire(PlayerTrigger.AttackCompleted);
        controller.Fire(PlayerTrigger.Attack2Start);
        Assert.That(controller.IsInState(PlayerState.Attacking), Is.True);

        controller.Fire(PlayerTrigger.AttackCompleted);
        controller.Fire(PlayerTrigger.Attack3Start);
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
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void FromAttacking_TakeDamage_InterruptsAttack()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
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
        _controller.Fire(PlayerTrigger.MoveRightStart);
        _controller.Fire(PlayerTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dying));
    }

    [Test]
    public void FromAttacking_Die_InterruptsAttack()
    {
        _controller.Fire(PlayerTrigger.Attack1Start);
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
        _controller.Fire(PlayerTrigger.Attack1Start);
        _controller.Fire(PlayerTrigger.MoveLeftStart);
        _controller.Fire(PlayerTrigger.Die);
        _controller.Fire(PlayerTrigger.TakeDamage);

        Assert.That(_controller.State, Is.EqualTo(PlayerState.Dead));
    }

    [Test]
    public void WhileHurt_AttackAndMovementTriggers_AreIgnored()
    {
        _controller.Fire(PlayerTrigger.TakeDamage);
        _controller.Fire(PlayerTrigger.Attack1Start);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));

        _controller.Fire(PlayerTrigger.MoveRightStart);
        Assert.That(_controller.State, Is.EqualTo(PlayerState.Hurt));
    }

    [Test]
    public void WhileDying_AllTriggers_AreIgnored()
    {
        _controller.Fire(PlayerTrigger.Die);

        _controller.Fire(PlayerTrigger.Attack1Start);
        _controller.Fire(PlayerTrigger.MoveRightStart);
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
    public void HurtExitCallback_IsInvoked_OnStateExit()
    {
        bool exitInvoked = false;
        var controller = new PlayerStateController(new()
        {
            OnHurtEntry = () => { },
            OnHurtExit = () => exitInvoked = true
        });
        controller.Fire(PlayerTrigger.TakeDamage);
        controller.Fire(PlayerTrigger.HurtCompleted);
        Assert.That(exitInvoked, Is.True);
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
    public void DyingExitCallback_IsInvoked_OnStateExit()
    {
        bool exitInvoked = false;
        var controller = new PlayerStateController(new()
        {
            OnDyingEntry = () => { },
            OnDyingExit = () => exitInvoked = true
        });
        controller.Fire(PlayerTrigger.Die);
        controller.Fire(PlayerTrigger.DeathCompleted);
        Assert.That(exitInvoked, Is.True);
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
}