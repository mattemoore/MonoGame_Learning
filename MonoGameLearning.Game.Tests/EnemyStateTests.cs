using MonoGameLearning.Core.StateMachines;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class EnemyStateTests
{
    private StateMachineController<EnemyState, EnemyTrigger> _controller;

    [SetUp]
    public void Setup() => _controller = EnemyStateMachine.Create();

    [Test]
    public void InitialState_ShouldBeIdle() =>
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));

    [Test]
    public void FromIdle_StartChase_TransitionsToChasing()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Chasing));
    }

    [Test]
    public void FromIdle_AttackStart_TransitionsToAttacking()
    {
        _controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Attacking));
    }

    [Test]
    public void FromIdle_TakeDamage_TransitionsToHurt()
    {
        _controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Hurt));
    }

    [Test]
    public void FromIdle_TakeKnockdown_TransitionsToKnockedDown()
    {
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void FromIdle_Die_TransitionsToDying()
    {
        _controller.Fire(EnemyTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void FromChasing_StopChase_TransitionsToIdle()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.StopChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void FromChasing_AttackStart_TransitionsToAttacking()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Attacking));
    }

    [Test]
    public void FromChasing_TakeDamage_TransitionsToHurt()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Hurt));
    }

    [Test]
    public void FromChasing_TakeKnockdown_TransitionsToKnockedDown()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void FromChasing_Die_TransitionsToDying()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void FromAttacking_AttackCompleted_TransitionsToIdle()
    {
        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void FromAttacking_TakeDamage_TransitionsToHurt()
    {
        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Hurt));
    }

    [Test]
    public void FromAttacking_Die_TransitionsToDying()
    {
        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void FromHurt_HurtCompleted_TransitionsToIdle()
    {
        _controller.Fire(EnemyTrigger.TakeDamage);
        _controller.Fire(EnemyTrigger.HurtCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void FromHurt_TakeKnockdown_TransitionsToKnockedDown()
    {
        _controller.Fire(EnemyTrigger.TakeDamage);
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void FromHurt_Die_TransitionsToDying()
    {
        _controller.Fire(EnemyTrigger.TakeDamage);
        _controller.Fire(EnemyTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void FromKnockedDown_KnockdownCompleted_TransitionsToIdle()
    {
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        _controller.Fire(EnemyTrigger.KnockdownCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void FromKnockedDown_Die_TransitionsToDying()
    {
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        _controller.Fire(EnemyTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void FromDying_DeathCompleted_TransitionsToDead()
    {
        _controller.Fire(EnemyTrigger.Die);
        _controller.Fire(EnemyTrigger.DeathCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dead));
    }

    [Test]
    public void Dead_IsTerminal_NoTransitionsOut()
    {
        _controller.Fire(EnemyTrigger.Die);
        _controller.Fire(EnemyTrigger.DeathCompleted);

        _controller.Fire(EnemyTrigger.HurtCompleted);
        _controller.Fire(EnemyTrigger.AttackCompleted);
        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.Die);
        _controller.Fire(EnemyTrigger.TakeDamage);

        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dead));
    }

    [Test]
    public void StartChase_WhileChasing_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.StartChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Chasing));
    }

    [Test]
    public void WhileAttacking_ChaseTriggers_AreIgnored()
    {
        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.StartChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Attacking));

        _controller.Fire(EnemyTrigger.StopChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Attacking));
    }

    [Test]
    public void WhileAttacking_AttackStart_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Attacking));
    }

    [Test]
    public void WhileIdle_AttackCompleted_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void WhileIdle_StopChase_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.StopChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void WhileDying_AllTriggers_AreIgnored()
    {
        _controller.Fire(EnemyTrigger.Die);

        _controller.Fire(EnemyTrigger.AttackStart);
        _controller.Fire(EnemyTrigger.StartChase);
        _controller.Fire(EnemyTrigger.TakeDamage);
        _controller.Fire(EnemyTrigger.HurtCompleted);

        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void WhileHurt_AttackAndChaseTriggers_AreIgnored()
    {
        _controller.Fire(EnemyTrigger.TakeDamage);
        _controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Hurt));

        _controller.Fire(EnemyTrigger.StartChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Hurt));
    }

    [Test]
    public void WhileHurt_TakeDamage_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.TakeDamage);
        _controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Hurt));
    }

    [Test]
    public void WhileKnockedDown_AttackAndMoveTriggers_AreIgnored()
    {
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        _controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));

        _controller.Fire(EnemyTrigger.StartChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));

        _controller.Fire(EnemyTrigger.StopChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void WhileKnockedDown_TakeDamage_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        _controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void CanFire_ReturnsTrue_ForValidTransition()
    {
        Assert.That(_controller.CanFire(EnemyTrigger.StartChase), Is.True);
    }

    [Test]
    public void CanFire_ReturnsTrue_ForIgnoredTrigger()
    {
        Assert.That(_controller.CanFire(EnemyTrigger.AttackCompleted), Is.True);
    }

    [Test]
    public void IgnoredTrigger_DoesNotChangeState()
    {
        _controller.Fire(EnemyTrigger.AttackCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void EntryCallbacks_AreInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new() { OnChasingEntry = () => entryInvoked = true });

        controller.Fire(EnemyTrigger.StartChase);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void Attacking_IsInAttackingState()
    {
        var controller = EnemyStateMachine.Create();
        controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(controller.IsInState(EnemyState.Attacking), Is.True);
    }

    [Test]
    public void HurtEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new() { OnHurtEntry = () => entryInvoked = true });
        controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void DyingEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new() { OnDyingEntry = () => entryInvoked = true });
        controller.Fire(EnemyTrigger.Die);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void DeadEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new()
        {
            OnDyingEntry = () => { },
            OnDeadEntry = () => entryInvoked = true
        });
        controller.Fire(EnemyTrigger.Die);
        controller.Fire(EnemyTrigger.DeathCompleted);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void ExitCallbacks_AreInvoked_OnStateExit()
    {
        bool exitInvoked = false;
        var controller = EnemyStateMachine.Create(new()
        {
            OnAttackingEntry = () => { },
            OnAttackingExit = () => exitInvoked = true
        });

        controller.Fire(EnemyTrigger.AttackStart);
        controller.Fire(EnemyTrigger.AttackCompleted);
        Assert.That(exitInvoked, Is.True);
    }

    [Test]
    public void KnockdownEntryCallback_IsInvoked()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new() { OnKnockdownEntry = () => entryInvoked = true });
        controller.Fire(EnemyTrigger.TakeKnockdown);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void KnockdownExitCallback_IsInvoked()
    {
        bool exitInvoked = false;
        var controller = EnemyStateMachine.Create(new()
        {
            OnKnockdownEntry = () => { },
            OnKnockdownExit = () => exitInvoked = true
        });
        controller.Fire(EnemyTrigger.TakeKnockdown);
        controller.Fire(EnemyTrigger.KnockdownCompleted);
        Assert.That(exitInvoked, Is.True);
    }

    [Test]
    public void Constructor_StartsInIdle_AndInvokesOnIdleEntry()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new() { OnIdleEntry = () => entryInvoked = true });
        Assert.Multiple(() =>
        {
            Assert.That(controller.State, Is.EqualTo(EnemyState.Idle));
            Assert.That(entryInvoked, Is.True);
        });
    }

    // --- Entering state transitions ---

    [Test]
    public void FromIdle_StartEntering_TransitionsToEntering()
    {
        _controller.Fire(EnemyTrigger.StartEntering);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Entering));
    }

    [Test]
    public void FromEntering_SpawnWalkCompleted_TransitionsToIdle()
    {
        _controller.Fire(EnemyTrigger.StartEntering);
        _controller.Fire(EnemyTrigger.SpawnWalkCompleted);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void FromEntering_Die_TransitionsToDying()
    {
        _controller.Fire(EnemyTrigger.StartEntering);
        _controller.Fire(EnemyTrigger.Die);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void WhileEntering_StartChase_IsIgnored()
    {
        _controller.Fire(EnemyTrigger.StartEntering);
        _controller.Fire(EnemyTrigger.StartChase);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Entering));
    }

    [Test]
    public void WhileEntering_AttackAndHurtTriggers_AreIgnored()
    {
        _controller.Fire(EnemyTrigger.StartEntering);
        _controller.Fire(EnemyTrigger.AttackStart);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Entering));
        _controller.Fire(EnemyTrigger.TakeDamage);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Entering));
        _controller.Fire(EnemyTrigger.TakeKnockdown);
        Assert.That(_controller.State, Is.EqualTo(EnemyState.Entering));
    }

    [Test]
    public void EnteringEntryCallback_IsInvoked_OnStateEntry()
    {
        bool entryInvoked = false;
        var controller = EnemyStateMachine.Create(new() { OnEnteringEntry = () => entryInvoked = true });
        controller.Fire(EnemyTrigger.StartEntering);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void EnteringExitCallback_IsInvoked_OnStateExit()
    {
        bool exitInvoked = false;
        var controller = EnemyStateMachine.Create(new()
        {
            OnEnteringEntry = () => { },
            OnEnteringExit = () => exitInvoked = true
        });
        controller.Fire(EnemyTrigger.StartEntering);
        controller.Fire(EnemyTrigger.SpawnWalkCompleted);
        Assert.That(exitInvoked, Is.True);
    }
}