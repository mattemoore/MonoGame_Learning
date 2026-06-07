using System;
using Stateless;

#nullable enable

namespace MonoGameLearning.Game.Entities.Player;

public enum PlayerState
{
    Dummy,
    Idling,
    Attacking,
    Attacking1,
    Attacking2,
    Attacking3,
    Moving,
    MovingLeft,
    MovingRight,
    MovingUp,
    MovingDown
}

public enum PlayerTrigger
{
    Activate,
    Attack1Start,
    Attack2Start,
    Attack3Start,
    AttackCompleted,
    MoveLeftStart,
    MoveRightStart,
    MoveUpStart,
    MoveDownStart,
    MoveStop
}

public class PlayerStateController
{
    public StateMachine<PlayerState, PlayerTrigger> StateMachine { get; }
    public PlayerState State => StateMachine.State;

    public PlayerStateController(
        Action? onIdleEntry = null,
        Action? onMovingLeftEntry = null,
        Action? onMovingRightEntry = null,
        Action? onMovingUpEntry = null,
        Action? onMovingDownEntry = null,
        Action? onAttacking1Entry = null,
        Action? onAttacking1Exit = null,
        Action? onAttacking2Entry = null,
        Action? onAttacking2Exit = null,
        Action? onAttacking3Entry = null,
        Action? onAttacking3Exit = null)
    {
        StateMachine = new(PlayerState.Dummy);

        StateMachine.Configure(PlayerState.Dummy)
            .OnActivate(() => StateMachine.Fire(PlayerTrigger.Activate))
            .Permit(PlayerTrigger.Activate, PlayerState.Idling)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.Attack1Start)
            .Ignore(PlayerTrigger.Attack2Start)
            .Ignore(PlayerTrigger.Attack3Start);

        StateMachine.Configure(PlayerState.Attacking)
            .Permit(PlayerTrigger.AttackCompleted, PlayerState.Idling)
            .Ignore(PlayerTrigger.Attack1Start)
            .Ignore(PlayerTrigger.Attack2Start)
            .Ignore(PlayerTrigger.Attack3Start)
            .Ignore(PlayerTrigger.MoveLeftStart)
            .Ignore(PlayerTrigger.MoveRightStart)
            .Ignore(PlayerTrigger.MoveUpStart)
            .Ignore(PlayerTrigger.MoveDownStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate);

        StateMachine.Configure(PlayerState.Attacking1)
            .OnEntry(_ => onAttacking1Entry?.Invoke())
            .OnExit(_ => onAttacking1Exit?.Invoke())
            .SubstateOf(PlayerState.Attacking);

        StateMachine.Configure(PlayerState.Attacking2)
            .OnEntry(_ => onAttacking2Entry?.Invoke())
            .OnExit(_ => onAttacking2Exit?.Invoke())
            .SubstateOf(PlayerState.Attacking);

        StateMachine.Configure(PlayerState.Attacking3)
            .OnEntry(_ => onAttacking3Entry?.Invoke())
            .OnExit(_ => onAttacking3Exit?.Invoke())
            .SubstateOf(PlayerState.Attacking);

        StateMachine.Configure(PlayerState.Idling)
            .OnEntry(_ => onIdleEntry?.Invoke())
            .Permit(PlayerTrigger.Attack1Start, PlayerState.Attacking1)
            .Permit(PlayerTrigger.Attack2Start, PlayerState.Attacking2)
            .Permit(PlayerTrigger.Attack3Start, PlayerState.Attacking3)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.MoveStop);

        StateMachine.Configure(PlayerState.Moving)
            .Permit(PlayerTrigger.Attack1Start, PlayerState.Attacking1)
            .Permit(PlayerTrigger.Attack2Start, PlayerState.Attacking2)
            .Permit(PlayerTrigger.Attack3Start, PlayerState.Attacking3)
            .Permit(PlayerTrigger.MoveStop, PlayerState.Idling)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

        StateMachine.Configure(PlayerState.MovingLeft)
            .OnEntry(_ => onMovingLeftEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.MoveLeftStart);

        StateMachine.Configure(PlayerState.MovingRight)
            .OnEntry(_ => onMovingRightEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.MoveRightStart);

        StateMachine.Configure(PlayerState.MovingUp)
            .OnEntry(_ => onMovingUpEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.MoveUpStart);

        StateMachine.Configure(PlayerState.MovingDown)
            .OnEntry(_ => onMovingDownEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Ignore(PlayerTrigger.MoveDownStart);

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