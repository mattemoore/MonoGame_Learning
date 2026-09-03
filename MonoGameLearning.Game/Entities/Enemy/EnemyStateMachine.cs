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
            machine => Configure(machine, callbacks),
            () => callbacks.OnIdleEntry?.Invoke());
    }

    private static void Configure(StateMachine<EnemyState, EnemyTrigger> machine, EnemyStateMachineCallbacks callbacks)
    {
        machine.Configure(EnemyState.Entering)
            .OnEntry(_ => callbacks.OnEnteringEntry?.Invoke())
            .OnExit(_ => callbacks.OnEnteringExit?.Invoke())
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

        machine.Configure(EnemyState.Idle)
            .OnEntry(_ => callbacks.OnIdleEntry?.Invoke())
            .Permit(EnemyTrigger.StartEntering, EnemyState.Entering)
            .Permit(EnemyTrigger.StartChase, EnemyState.Chasing)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.AttackCompleted)
            .Ignore(EnemyTrigger.StopChase);

        machine.Configure(EnemyState.Chasing)
            .OnEntry(_ => callbacks.OnChasingEntry?.Invoke())
            .Permit(EnemyTrigger.StopChase, EnemyState.Idle)
            .Permit(EnemyTrigger.AttackStart, EnemyState.Attacking)
            .Permit(EnemyTrigger.TakeDamage, EnemyState.Hurt)
            .Permit(EnemyTrigger.TakeKnockdown, EnemyState.KnockedDown)
            .Permit(EnemyTrigger.Die, EnemyState.Dying)
            .Ignore(EnemyTrigger.StartChase)
            .Ignore(EnemyTrigger.AttackCompleted);

        CombatStateMachineConfigurator.ConfigureCombatStates(
            machine,
            callbacks,
            returnState: EnemyState.Idle,
            states: new(EnemyState.Attacking, EnemyState.Hurt, EnemyState.KnockedDown, EnemyState.Dying, EnemyState.Dead),
            triggers: new(EnemyTrigger.AttackStart, EnemyTrigger.AttackCompleted, EnemyTrigger.TakeDamage, EnemyTrigger.TakeKnockdown,
                EnemyTrigger.KnockdownCompleted, EnemyTrigger.HurtCompleted, EnemyTrigger.Die, EnemyTrigger.DeathCompleted),
            movementStart: EnemyTrigger.StartChase,
            movementStop: EnemyTrigger.StopChase);
    }
}