using System;
using System.Collections.Generic;
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
    Reset,
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

    private static readonly Dictionary<(EnemyState, EnemyTrigger), EnemyState> Transitions = new()
    {
        [(EnemyState.Dummy, EnemyTrigger.Activate)] = EnemyState.Idle,
        [(EnemyState.Idle, EnemyTrigger.StartChase)] = EnemyState.Chasing,
        [(EnemyState.Idle, EnemyTrigger.AttackStart)] = EnemyState.Attacking,
        [(EnemyState.Idle, EnemyTrigger.TakeDamage)] = EnemyState.Hurt,
        [(EnemyState.Idle, EnemyTrigger.TakeKnockdown)] = EnemyState.KnockedDown,
        [(EnemyState.Idle, EnemyTrigger.Die)] = EnemyState.Dying,
        [(EnemyState.Chasing, EnemyTrigger.StopChase)] = EnemyState.Idle,
        [(EnemyState.Chasing, EnemyTrigger.AttackStart)] = EnemyState.Attacking,
        [(EnemyState.Chasing, EnemyTrigger.TakeDamage)] = EnemyState.Hurt,
        [(EnemyState.Chasing, EnemyTrigger.TakeKnockdown)] = EnemyState.KnockedDown,
        [(EnemyState.Chasing, EnemyTrigger.Die)] = EnemyState.Dying,
        [(EnemyState.Attacking, EnemyTrigger.AttackCompleted)] = EnemyState.Idle,
        [(EnemyState.Attacking, EnemyTrigger.TakeDamage)] = EnemyState.Hurt,
        [(EnemyState.Attacking, EnemyTrigger.TakeKnockdown)] = EnemyState.KnockedDown,
        [(EnemyState.Attacking, EnemyTrigger.Die)] = EnemyState.Dying,
        [(EnemyState.Hurt, EnemyTrigger.HurtCompleted)] = EnemyState.Idle,
        [(EnemyState.Hurt, EnemyTrigger.TakeKnockdown)] = EnemyState.KnockedDown,
        [(EnemyState.Hurt, EnemyTrigger.Die)] = EnemyState.Dying,
        [(EnemyState.KnockedDown, EnemyTrigger.KnockdownCompleted)] = EnemyState.Idle,
        [(EnemyState.KnockedDown, EnemyTrigger.Die)] = EnemyState.Dying,
        [(EnemyState.Dying, EnemyTrigger.DeathCompleted)] = EnemyState.Dead,
    };

    private static readonly Dictionary<EnemyState, HashSet<EnemyTrigger>> IgnoredTriggers = new()
    {
        [EnemyState.Dummy] = [EnemyTrigger.AttackCompleted],
        [EnemyState.Idle] = [EnemyTrigger.Activate, EnemyTrigger.AttackCompleted, EnemyTrigger.StopChase],
        [EnemyState.Chasing] = [EnemyTrigger.StartChase, EnemyTrigger.Activate, EnemyTrigger.AttackCompleted],
        [EnemyState.Attacking] = [EnemyTrigger.StartChase, EnemyTrigger.StopChase, EnemyTrigger.AttackStart, EnemyTrigger.Activate],
        [EnemyState.Hurt] = [EnemyTrigger.TakeDamage, EnemyTrigger.AttackStart, EnemyTrigger.StartChase, EnemyTrigger.StopChase, EnemyTrigger.Activate, EnemyTrigger.AttackCompleted],
        [EnemyState.KnockedDown] = [EnemyTrigger.TakeDamage, EnemyTrigger.TakeKnockdown, EnemyTrigger.AttackStart, EnemyTrigger.AttackCompleted, EnemyTrigger.StartChase, EnemyTrigger.StopChase, EnemyTrigger.HurtCompleted, EnemyTrigger.Activate],
        [EnemyState.Dying] = [EnemyTrigger.TakeDamage, EnemyTrigger.Die, EnemyTrigger.HurtCompleted, EnemyTrigger.AttackStart, EnemyTrigger.StartChase, EnemyTrigger.StopChase, EnemyTrigger.Activate, EnemyTrigger.AttackCompleted, EnemyTrigger.TakeKnockdown, EnemyTrigger.KnockdownCompleted],
        [EnemyState.Dead] = [EnemyTrigger.TakeDamage, EnemyTrigger.Die, EnemyTrigger.HurtCompleted, EnemyTrigger.DeathCompleted, EnemyTrigger.AttackStart, EnemyTrigger.StartChase, EnemyTrigger.StopChase, EnemyTrigger.Activate, EnemyTrigger.AttackCompleted, EnemyTrigger.TakeKnockdown, EnemyTrigger.KnockdownCompleted],
    };

    public EnemyStateController(EnemyStateEntryCallbacks callbacks = null)
    {
        StateMachine = new(EnemyState.Dummy);
        ConfigureStateMachine(callbacks);
        StateMachine.Activate();
    }

    private void ConfigureStateMachine(EnemyStateEntryCallbacks callbacks)
    {
        var allStates = (EnemyState[])Enum.GetValues(typeof(EnemyState));
        foreach (var state in allStates)
        {
            var config = StateMachine.Configure(state);

            if (callbacks is not null)
            {
                var entry = state switch
                {
                    EnemyState.Idle => callbacks.OnIdleEntry,
                    EnemyState.Chasing => callbacks.OnChasingEntry,
                    EnemyState.Attacking => callbacks.OnAttackingEntry,
                    EnemyState.Hurt => callbacks.OnHurtEntry,
                    EnemyState.KnockedDown => callbacks.OnKnockdownEntry,
                    EnemyState.Dying => callbacks.OnDyingEntry,
                    EnemyState.Dead => callbacks.OnDeadEntry,
                    _ => null
                };
                if (entry is not null) config.OnEntry(_ => entry());

                var exit = state switch
                {
                    EnemyState.Attacking => callbacks.OnAttackingExit,
                    EnemyState.Hurt => callbacks.OnHurtExit,
                    EnemyState.KnockedDown => callbacks.OnKnockdownExit,
                    EnemyState.Dying => callbacks.OnDyingExit,
                    _ => null
                };
                if (exit is not null) config.OnExit(_ => exit());
            }

            if (state == EnemyState.Dummy)
                config.OnActivate(() => StateMachine.Fire(EnemyTrigger.Activate));

            foreach (var ((fromState, trigger), toState) in Transitions)
            {
                if (fromState == state)
                    config.Permit(trigger, toState);
            }

            if (IgnoredTriggers.TryGetValue(state, out var ignored))
            {
                foreach (var trigger in ignored)
                    config.Ignore(trigger);
            }

            if (state != EnemyState.Dummy)
                config.Permit(EnemyTrigger.Reset, EnemyState.Dummy);
        }
    }

    public void ResetToRoot()
    {
        if (StateMachine.CanFire(EnemyTrigger.Reset))
            StateMachine.Fire(EnemyTrigger.Reset);
        StateMachine.Activate();
    }

    public bool IsInState(EnemyState state) => StateMachine.IsInState(state);

    public bool CanFire(EnemyTrigger trigger) => StateMachine.CanFire(trigger);

    public void Fire(EnemyTrigger trigger)
    {
        StateMachine.Fire(trigger);
    }
}