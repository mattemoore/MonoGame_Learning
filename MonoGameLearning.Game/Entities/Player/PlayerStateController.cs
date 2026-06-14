using System;
using Stateless;

namespace MonoGameLearning.Game.Entities.Player;

public class PlayerStateControllerConfig
{
    public Action OnIdleEntry { get; init; }
    public Action OnMovingEntry { get; init; }
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

public enum PlayerState
{
    Dummy,
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
    Activate,
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

public class PlayerStateController
{
    public StateMachine<PlayerState, PlayerTrigger> StateMachine { get; }
    public PlayerState State => StateMachine.State;

    public PlayerStateController(PlayerStateControllerConfig config = null)
    {
        StateMachine = new(PlayerState.Dummy);

        StateMachine.Configure(PlayerState.Dummy)
            .OnActivate(() => StateMachine.Fire(PlayerTrigger.Activate))
            .Permit(PlayerTrigger.Activate, PlayerState.Idling)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.AttackStart);

        StateMachine.Configure(PlayerState.Idling)
            .OnEntry(_ => config?.OnIdleEntry?.Invoke())
            .Permit(PlayerTrigger.MoveStart, PlayerState.Moving)
            .Permit(PlayerTrigger.AttackStart, PlayerState.Attacking)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.MoveStop);

        StateMachine.Configure(PlayerState.Moving)
            .OnEntry(_ => config?.OnMovingEntry?.Invoke())
            .Permit(PlayerTrigger.MoveStop, PlayerState.Idling)
            .Permit(PlayerTrigger.AttackStart, PlayerState.Attacking)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

        StateMachine.Configure(PlayerState.Attacking)
            .OnEntry(_ => config?.OnAttackingEntry?.Invoke())
            .OnExit(_ => config?.OnAttackingExit?.Invoke())
            .Permit(PlayerTrigger.AttackCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.Activate);

        StateMachine.Configure(PlayerState.Hurt)
            .OnEntry(_ => config?.OnHurtEntry?.Invoke())
            .OnExit(_ => config?.OnHurtExit?.Invoke())
            .Permit(PlayerTrigger.HurtCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.TakeKnockdown, PlayerState.KnockedDown)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

        StateMachine.Configure(PlayerState.KnockedDown)
            .OnEntry(_ => config?.OnKnockdownEntry?.Invoke())
            .OnExit(_ => config?.OnKnockdownExit?.Invoke())
            .Permit(PlayerTrigger.KnockdownCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.TakeKnockdown)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.Activate);

        StateMachine.Configure(PlayerState.Dying)
            .OnEntry(_ => config?.OnDyingEntry?.Invoke())
            .OnExit(_ => config?.OnDyingExit?.Invoke())
            .Permit(PlayerTrigger.DeathCompleted, PlayerState.Dead)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Die)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.TakeKnockdown)
            .Ignore(PlayerTrigger.KnockdownCompleted);

        StateMachine.Configure(PlayerState.Dead)
            .OnEntry(_ => config?.OnDeadEntry?.Invoke())
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Die)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.DeathCompleted)
            .Ignore(PlayerTrigger.AttackStart)
            .Ignore(PlayerTrigger.MoveStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.TakeKnockdown)
            .Ignore(PlayerTrigger.KnockdownCompleted);

        StateMachine.Activate();
    }

    public bool IsInState(PlayerState state) => StateMachine.IsInState(state);

    public bool CanFire(PlayerTrigger trigger) => StateMachine.CanFire(trigger);

    public void Fire(PlayerTrigger trigger)
    {
        if (StateMachine.CanFire(trigger))
            StateMachine.Fire(trigger);
    }
}