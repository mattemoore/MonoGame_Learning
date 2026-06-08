using System;
using Stateless;

namespace MonoGameLearning.Game.Entities.Player;

public class PlayerStateControllerConfig
{
    public Action OnIdleEntry { get; init; }
    public Action OnMovingLeftEntry { get; init; }
    public Action OnMovingRightEntry { get; init; }
    public Action OnMovingUpEntry { get; init; }
    public Action OnMovingDownEntry { get; init; }
    public Action OnAttacking1Entry { get; init; }
    public Action OnAttacking1Exit { get; init; }
    public Action OnAttacking2Entry { get; init; }
    public Action OnAttacking2Exit { get; init; }
    public Action OnAttacking3Entry { get; init; }
    public Action OnAttacking3Exit { get; init; }
    public Action OnHurtEntry { get; init; }
    public Action OnHurtExit { get; init; }
    public Action OnDyingEntry { get; init; }
    public Action OnDyingExit { get; init; }
    public Action OnDeadEntry { get; init; }
}

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
    MovingDown,
    Hurt,
    Dying,
    Dead
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
    MoveStop,
    TakeDamage,
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
            .Ignore(PlayerTrigger.Attack1Start)
            .Ignore(PlayerTrigger.Attack2Start)
            .Ignore(PlayerTrigger.Attack3Start);

        StateMachine.Configure(PlayerState.Attacking)
            .Permit(PlayerTrigger.AttackCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
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
            .OnEntry(_ => config?.OnAttacking1Entry?.Invoke())
            .OnExit(_ => config?.OnAttacking1Exit?.Invoke())
            .SubstateOf(PlayerState.Attacking);

        StateMachine.Configure(PlayerState.Attacking2)
            .OnEntry(_ => config?.OnAttacking2Entry?.Invoke())
            .OnExit(_ => config?.OnAttacking2Exit?.Invoke())
            .SubstateOf(PlayerState.Attacking);

        StateMachine.Configure(PlayerState.Attacking3)
            .OnEntry(_ => config?.OnAttacking3Entry?.Invoke())
            .OnExit(_ => config?.OnAttacking3Exit?.Invoke())
            .SubstateOf(PlayerState.Attacking);

        StateMachine.Configure(PlayerState.Idling)
            .OnEntry(_ => config?.OnIdleEntry?.Invoke())
            .Permit(PlayerTrigger.Attack1Start, PlayerState.Attacking1)
            .Permit(PlayerTrigger.Attack2Start, PlayerState.Attacking2)
            .Permit(PlayerTrigger.Attack3Start, PlayerState.Attacking3)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted)
            .Ignore(PlayerTrigger.MoveStop);

        StateMachine.Configure(PlayerState.Moving)
            .Permit(PlayerTrigger.Attack1Start, PlayerState.Attacking1)
            .Permit(PlayerTrigger.Attack2Start, PlayerState.Attacking2)
            .Permit(PlayerTrigger.Attack3Start, PlayerState.Attacking3)
            .Permit(PlayerTrigger.MoveStop, PlayerState.Idling)
            .Permit(PlayerTrigger.TakeDamage, PlayerState.Hurt)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

        StateMachine.Configure(PlayerState.MovingLeft)
            .OnEntry(_ => config?.OnMovingLeftEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.MoveLeftStart);

        StateMachine.Configure(PlayerState.MovingRight)
            .OnEntry(_ => config?.OnMovingRightEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.MoveRightStart);

        StateMachine.Configure(PlayerState.MovingUp)
            .OnEntry(_ => config?.OnMovingUpEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveDownStart, PlayerState.MovingDown)
            .Ignore(PlayerTrigger.MoveUpStart);

        StateMachine.Configure(PlayerState.MovingDown)
            .OnEntry(_ => config?.OnMovingDownEntry?.Invoke())
            .SubstateOf(PlayerState.Moving)
            .Permit(PlayerTrigger.MoveRightStart, PlayerState.MovingRight)
            .Permit(PlayerTrigger.MoveUpStart, PlayerState.MovingUp)
            .Permit(PlayerTrigger.MoveLeftStart, PlayerState.MovingLeft)
            .Ignore(PlayerTrigger.MoveDownStart);

        StateMachine.Configure(PlayerState.Hurt)
            .OnEntry(_ => config?.OnHurtEntry?.Invoke())
            .OnExit(_ => config?.OnHurtExit?.Invoke())
            .Permit(PlayerTrigger.HurtCompleted, PlayerState.Idling)
            .Permit(PlayerTrigger.Die, PlayerState.Dying)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Attack1Start)
            .Ignore(PlayerTrigger.Attack2Start)
            .Ignore(PlayerTrigger.Attack3Start)
            .Ignore(PlayerTrigger.MoveLeftStart)
            .Ignore(PlayerTrigger.MoveRightStart)
            .Ignore(PlayerTrigger.MoveUpStart)
            .Ignore(PlayerTrigger.MoveDownStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

        StateMachine.Configure(PlayerState.Dying)
            .OnEntry(_ => config?.OnDyingEntry?.Invoke())
            .OnExit(_ => config?.OnDyingExit?.Invoke())
            .Permit(PlayerTrigger.DeathCompleted, PlayerState.Dead)
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Die)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.Attack1Start)
            .Ignore(PlayerTrigger.Attack2Start)
            .Ignore(PlayerTrigger.Attack3Start)
            .Ignore(PlayerTrigger.MoveLeftStart)
            .Ignore(PlayerTrigger.MoveRightStart)
            .Ignore(PlayerTrigger.MoveUpStart)
            .Ignore(PlayerTrigger.MoveDownStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

        StateMachine.Configure(PlayerState.Dead)
            .OnEntry(_ => config?.OnDeadEntry?.Invoke())
            .Ignore(PlayerTrigger.TakeDamage)
            .Ignore(PlayerTrigger.Die)
            .Ignore(PlayerTrigger.HurtCompleted)
            .Ignore(PlayerTrigger.DeathCompleted)
            .Ignore(PlayerTrigger.Attack1Start)
            .Ignore(PlayerTrigger.Attack2Start)
            .Ignore(PlayerTrigger.Attack3Start)
            .Ignore(PlayerTrigger.MoveLeftStart)
            .Ignore(PlayerTrigger.MoveRightStart)
            .Ignore(PlayerTrigger.MoveUpStart)
            .Ignore(PlayerTrigger.MoveDownStart)
            .Ignore(PlayerTrigger.MoveStop)
            .Ignore(PlayerTrigger.Activate)
            .Ignore(PlayerTrigger.AttackCompleted);

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