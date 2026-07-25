using System;
using Stateless;

namespace MonoGameLearning.Game.Entities.Enemy;

public class EnemyStateEntryCallbacks
{
    public Action OnIdleEntry { get; init; }
    public Action OnChasingEntry { get; init; }
    public Action OnAttackingEntry { get; init; }
    public Action OnAttackingExit { get; init; }
    public Action OnHurtEntry { get; init; }
    public Action OnHurtExit { get; init; }
    public Action OnKnockdownEntry { get; init; }
    public Action OnKnockdownExit { get; init; }
    public Action OnDyingEntry { get; init; }
    public Action OnDyingExit { get; init; }
    public Action OnDeadEntry { get; init; }
    public Action OnEnteringEntry { get; init; }
    public Action OnEnteringExit { get; init; }
}

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

public class EnemyStateController
{
    public StateMachine<EnemyState, EnemyTrigger> StateMachine { get; }
    public EnemyState State => StateMachine.State;

    public EnemyStateController(EnemyStateEntryCallbacks callbacks = null)
    {
        StateMachine = new(EnemyState.Idle);

        StateMachine.Configure(EnemyState.Entering)
            .OnEntry(_ => callbacks?.OnEnteringEntry?.Invoke())
            .OnExit(_ => callbacks?.OnEnteringExit?.Invoke())
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

        StateMachine.Configure(EnemyState.Idle)
            .OnEntry(_ => callbacks?.OnIdleEntry?.Invoke())
            .Permit(EnemyTrigger.StartEntering, EnemyState.Entering)
            .Permit(EnemyTrigger.StartChase, EnemyState.Chasing)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StopChase);

        StateMachine.Configure(EnemyState.Chasing)
            .OnEntry(_ => callbacks?.OnChasingEntry?.Invoke())
            .Permit(EnemyTrigger.StopChase, EnemyState.Idle)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.AttackCompleted);

        StateMachine.Configure(EnemyState.Attacking)
            .OnEntry(_ => callbacks?.OnAttackingEntry?.Invoke())
            .OnExit(_ => callbacks?.OnAttackingExit?.Invoke())
            .Permit(EnemyTrigger.AttackCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackStart);

        StateMachine.Configure(EnemyState.Hurt)
            .OnEntry(_ => callbacks?.OnHurtEntry?.Invoke())
            .OnExit(_ => callbacks?.OnHurtExit?.Invoke())
            .Permit(EnemyTrigger.HurtCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackCompleted);

        StateMachine.Configure(EnemyState.KnockedDown)
            .OnEntry(_ => callbacks?.OnKnockdownEntry?.Invoke())
            .OnExit(_ => callbacks?.OnKnockdownExit?.Invoke())
            .Permit(EnemyTrigger.KnockdownCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.HurtCompleted);

        StateMachine.Configure(EnemyState.Dying)
            .OnEntry(_ => callbacks?.OnDyingEntry?.Invoke())
            .OnExit(_ => callbacks?.OnDyingExit?.Invoke())
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

        StateMachine.Configure(EnemyState.Dead)
            .OnEntry(_ => callbacks?.OnDeadEntry?.Invoke())
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

        callbacks?.OnIdleEntry?.Invoke();
    }

    public bool IsInState(EnemyState state) => StateMachine.IsInState(state);

    public bool CanFire(EnemyTrigger trigger) => StateMachine.CanFire(trigger);

    public void Fire(EnemyTrigger trigger)
    {
        StateMachine.Fire(trigger);
    }
}