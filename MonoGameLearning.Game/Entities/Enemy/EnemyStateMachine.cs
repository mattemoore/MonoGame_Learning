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
    public static StateMachineController<EnemyState, EnemyTrigger> Create(EnemyStateMachineCallbacks callbacks = null)
    {
        callbacks ??= new EnemyStateMachineCallbacks();
        return new StateMachineController<EnemyState, EnemyTrigger>(
            EnemyState.Idle,
            sm => Configure(sm, callbacks),
            () => callbacks.OnIdleEntry?.Invoke());
    }

    private static void Configure(StateMachine<EnemyState, EnemyTrigger> sm, EnemyStateMachineCallbacks c)
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

        CombatStateMachineConfigurator.ConfigureCombatStates(
            sm,
            c,
            returnState: EnemyState.Idle,
            states: new(EnemyState.Attacking, EnemyState.Hurt, EnemyState.KnockedDown, EnemyState.Dying, EnemyState.Dead),
            triggers: new(EnemyTrigger.AttackStart, EnemyTrigger.AttackCompleted, EnemyTrigger.TakeDamage, EnemyTrigger.TakeKnockdown,
                EnemyTrigger.KnockdownCompleted, EnemyTrigger.HurtCompleted, EnemyTrigger.Die, EnemyTrigger.DeathCompleted),
            movementStart: EnemyTrigger.StartChase,
            movementStop: EnemyTrigger.StopChase);
    }
}