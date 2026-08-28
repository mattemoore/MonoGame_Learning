using System;
using System.Diagnostics;
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
    public static StateMachineController<PlayerState, PlayerTrigger> Create(ActorStateMachineCallbacks callbacks = null)
    {
        callbacks ??= new ActorStateMachineCallbacks();
        Debug.Assert(callbacks.OnChasingEntry is null && callbacks.OnEnteringEntry is null && callbacks.OnEnteringExit is null,
            "PlayerStateMachine: enemy-only callbacks (OnChasingEntry/OnEnteringEntry/OnEnteringExit) are not wired by the player machine");
        return new StateMachineController<PlayerState, PlayerTrigger>(
            PlayerState.Idling,
            sm => Configure(sm, callbacks),
            () => callbacks.OnIdleEntry?.Invoke());
    }

    private static void Configure(StateMachine<PlayerState, PlayerTrigger> sm, ActorStateMachineCallbacks c)
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

        sm.Configure(PlayerState.Attacking)
            .OnEntry(_ => c.OnAttackingEntry?.Invoke())
            .OnExit(_ => c.OnAttackingExit?.Invoke())
            .Permit(PlayerTrigger.AttackCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.AttackStart);

        sm.Configure(PlayerState.Hurt)
            .OnEntry(_ => c.OnHurtEntry?.Invoke())
            .OnExit(_ => c.OnHurtExit?.Invoke())
            .Permit(PlayerTrigger.HurtCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.AttackCompleted);

        sm.Configure(PlayerState.KnockedDown)
            .OnEntry(_ => c.OnKnockdownEntry?.Invoke())
            .OnExit(_ => c.OnKnockdownExit?.Invoke())
            .Permit(PlayerTrigger.KnockdownCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.TakeKnockdown)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.HurtCompleted);

        sm.Configure(PlayerState.Dying)
            .OnEntry(_ => c.OnDyingEntry?.Invoke())
            .OnExit(_ => c.OnDyingExit?.Invoke())
            .Permit(PlayerTrigger.DeathCompleted, PlayerState.Dead)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Die)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.TakeKnockdown)
            .Ignore(PlayerTrigger.KnockdownCompleted);

        sm.Configure(PlayerState.Dead)
            .OnEntry(_ => c.OnDeadEntry?.Invoke())
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Die)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.DeathCompleted)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.TakeKnockdown)
            .Ignore(PlayerTrigger.KnockdownCompleted);
    }
}