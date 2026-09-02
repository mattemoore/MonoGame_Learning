using MonoGameLearning.Core.StateMachines;
using MonoGameLearning.Game.StateMachines;
using Stateless;

namespace MonoGameLearning.Game.Entities.Player;

public enum PlayerState
{
    Idling,
    Moving,
    Attacking,
    Hurt,
    KnockedDown,
    Dying,
    Dead
}

public enum PlayerTrigger
{
    MoveStart,
    MoveStop,
    AttackStart,
    AttackCompleted,
    TakeDamage,
    TakeKnockdown,
    KnockdownCompleted,
    Die,
    HurtCompleted,
    DeathCompleted
}

public static class PlayerStateMachine
{
    public static StateMachineController<PlayerState, PlayerTrigger> Create(PlayerStateMachineCallbacks callbacks = null)
    {
        callbacks ??= new PlayerStateMachineCallbacks();
        return new StateMachineController<PlayerState, PlayerTrigger>(
            PlayerState.Idling,
            sm => Configure(sm, callbacks),
            () => callbacks.OnIdleEntry?.Invoke());
    }

    private static void Configure(StateMachine<PlayerState, PlayerTrigger> sm, PlayerStateMachineCallbacks c)
    {
        sm.Configure(PlayerState.Idling)
            .OnEntry(_ => c.OnIdleEntry?.Invoke())
            .Permit(PlayerTrigger.MoveStart, PlayerState.Moving)
            .Permit(PlayerTrigger.AttackStart, PlayerState.Attacking)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.MoveStop);

        sm.Configure(PlayerState.Moving)
            .OnEntry(_ => c.OnMovingEntry?.Invoke())
            .Permit(PlayerTrigger.MoveStop, PlayerState.Idling)
            .Permit(PlayerTrigger.AttackStart, PlayerState.Attacking)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.AttackCompleted);

        CombatStateMachineConfigurator.ConfigureCombatStates(
            sm,
            c,
            returnState: PlayerState.Idling,
            states: new(PlayerState.Attacking, PlayerState.Hurt, PlayerState.KnockedDown, PlayerState.Dying, PlayerState.Dead),
            triggers: new(PlayerTrigger.AttackStart, PlayerTrigger.AttackCompleted, PlayerTrigger.TakeDamage, PlayerTrigger.TakeKnockdown,
                PlayerTrigger.KnockdownCompleted, PlayerTrigger.HurtCompleted, PlayerTrigger.Die, PlayerTrigger.DeathCompleted),
            movementStart: PlayerTrigger.MoveStart,
            movementStop: PlayerTrigger.MoveStop);
    }
}