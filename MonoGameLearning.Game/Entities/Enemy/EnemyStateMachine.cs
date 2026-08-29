using System.Diagnostics;
using MonoGameLearning.Core.StateMachines;
using MonoGameLearning.Game.StateMachines;
using Stateless;

namespace MonoGameLearning.Game.Entities.Enemy;

public enum EnemyState
{
    Entering,
    Idle,
    Chasing,
    Attacking,
    Hurt,
    KnockedDown,
    Dying,
    Dead
}

public enum EnemyTrigger
{
    StartEntering,
    SpawnWalkCompleted,
    StartChase,
    StopChase,
    AttackStart,
    AttackCompleted,
    TakeDamage,
    TakeKnockdown,
    KnockdownCompleted,
    Die,
    HurtCompleted,
    DeathCompleted
}

public static class EnemyStateMachine
{
    public static StateMachineController<EnemyState, EnemyTrigger> Create(ActorStateMachineCallbacks callbacks = null)
    {
        callbacks ??= new ActorStateMachineCallbacks();
        Debug.Assert(callbacks.OnMovingEntry is null,
            "EnemyStateMachine: player-only callback (OnMovingEntry) is not wired by the enemy machine");
        return new StateMachineController<EnemyState, EnemyTrigger>(
            EnemyState.Idle,
            sm => Configure(sm, callbacks),
            () => callbacks.OnIdleEntry?.Invoke());
    }

    private static void Configure(StateMachine<EnemyState, EnemyTrigger> sm, ActorStateMachineCallbacks c)
    {
        sm.Configure(EnemyState.Entering)
            .OnEntry(_ => c.OnEnteringEntry?.Invoke())
            .OnExit(_ => c.OnEnteringExit?.Invoke())
            .Permit(EnemyTrigger.SpawnWalkCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartEntering)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.KnockdownCompleted)
            .Ignore(EnemyTrigger.HurtCompleted)
            .Ignore(EnemyTrigger.DeathCompleted);

        sm.Configure(EnemyState.Idle)
            .OnEntry(_ => c.OnIdleEntry?.Invoke())
            .Permit(EnemyTrigger.StartEntering, EnemyState.Entering)
            .Permit(EnemyTrigger.StartChase, EnemyState.Chasing)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StopChase);

        sm.Configure(EnemyState.Chasing)
            .OnEntry(_ => c.OnChasingEntry?.Invoke())
            .Permit(EnemyTrigger.StopChase, EnemyState.Idle)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.AttackCompleted);

        sm.Configure(EnemyState.Attacking)
            .OnEntry(_ => c.OnAttackingEntry?.Invoke())
            .OnExit(_ => c.OnAttackingExit?.Invoke())
            .Permit(EnemyTrigger.AttackCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackStart);

        sm.Configure(EnemyState.Hurt)
            .OnEntry(_ => c.OnHurtEntry?.Invoke())
            .OnExit(_ => c.OnHurtExit?.Invoke())
            .Permit(EnemyTrigger.HurtCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackCompleted);

        sm.Configure(EnemyState.KnockedDown)
            .OnEntry(_ => c.OnKnockdownEntry?.Invoke())
            .OnExit(_ => c.OnKnockdownExit?.Invoke())
            .Permit(EnemyTrigger.KnockdownCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.HurtCompleted);

        sm.Configure(EnemyState.Dying)
            .OnEntry(_ => c.OnDyingEntry?.Invoke())
            .OnExit(_ => c.OnDyingExit?.Invoke())
            .Permit(EnemyTrigger.DeathCompleted, EnemyState.Dead)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.Die)
            .Ignore(EnemyTrigger.HurtCompleted)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.KnockdownCompleted);

        sm.Configure(EnemyState.Dead)
            .OnEntry(_ => c.OnDeadEntry?.Invoke())
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.Die)
            .Ignore(EnemyTrigger.HurtCompleted)
            .Ignore(EnemyTrigger.DeathCompleted)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.KnockdownCompleted);
    }
}