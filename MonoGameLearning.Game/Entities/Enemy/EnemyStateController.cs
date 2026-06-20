using System;
using Stateless;

namespace MonoGameLearning.Game.Entities.Enemy;

public class EnemyStateControllerConfig
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
}

public enum EnemyState
{
    Dummy,
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
    Activate,
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

    public EnemyStateController(EnemyStateControllerConfig config = null)
    {
        StateMachine = new(EnemyState.Dummy);

        StateMachine.Configure(EnemyState.Dummy)
            .OnActivate(() => StateMachine.Fire(EnemyTrigger.Activate))
            .Permit(EnemyTrigger.Activate, EnemyState.Idle)
            .Ignore(EnemyTrigger.AttackCompleted);

        StateMachine.Configure(EnemyState.Idle)
            .OnEntry(_ => config?.OnIdleEntry?.Invoke())
            .Permit(EnemyTrigger.StartChase, EnemyState.Chasing)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.Activate)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StopChase);

        StateMachine.Configure(EnemyState.Chasing)
            .OnEntry(_ => config?.OnChasingEntry?.Invoke())
            .Permit(EnemyTrigger.StopChase, EnemyState.Idle)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.Activate)
            .Ignore(EnemyTrigger.AttackCompleted);

        StateMachine.Configure(EnemyState.Attacking)
            .OnEntry(_ => config?.OnAttackingEntry?.Invoke())
            .OnExit(_ => config?.OnAttackingExit?.Invoke())
            .Permit(EnemyTrigger.AttackCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.Activate);

        StateMachine.Configure(EnemyState.Hurt)
            .OnEntry(_ => config?.OnHurtEntry?.Invoke())
            .OnExit(_ => config?.OnHurtExit?.Invoke())
            .Permit(EnemyTrigger.HurtCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.Activate)
            .Ignore(EnemyTrigger.AttackCompleted);

        StateMachine.Configure(EnemyState.KnockedDown)
            .OnEntry(_ => config?.OnKnockdownEntry?.Invoke())
            .OnExit(_ => config?.OnKnockdownExit?.Invoke())
            .Permit(EnemyTrigger.KnockdownCompleted, EnemyState.Idle)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.HurtCompleted)
            .Ignore(EnemyTrigger.Activate);

        StateMachine.Configure(EnemyState.Dying)
            .OnEntry(_ => config?.OnDyingEntry?.Invoke())
            .OnExit(_ => config?.OnDyingExit?.Invoke())
            .Permit(EnemyTrigger.DeathCompleted, EnemyState.Dead)
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.Die)
            .Ignore(EnemyTrigger.HurtCompleted)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.Activate)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.KnockdownCompleted);

        StateMachine.Configure(EnemyState.Dead)
            .OnEntry(_ => config?.OnDeadEntry?.Invoke())
            .Ignore(EnemyTrigger.TakeDamage)
            .Ignore(EnemyTrigger.Die)
            .Ignore(EnemyTrigger.HurtCompleted)
            .Ignore(EnemyTrigger.DeathCompleted)
            .Ignore(EnemyTrigger.AttackStart)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.StopChase)
            .Ignore(EnemyTrigger.Activate)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.TakeKnockdown)
            .Ignore(EnemyTrigger.KnockdownCompleted);

        StateMachine.Activate();
    }

    public bool IsInState(EnemyState state) => StateMachine.IsInState(state);

    public bool CanFire(EnemyTrigger trigger) => StateMachine.CanFire(trigger);

    public void Fire(EnemyTrigger trigger)
    {
        StateMachine.Fire(trigger);
    }
}
