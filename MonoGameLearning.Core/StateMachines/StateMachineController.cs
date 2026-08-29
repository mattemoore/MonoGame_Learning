using System;
using System.Diagnostics;
using Stateless;

namespace MonoGameLearning.Core.StateMachines;

public sealed class StateMachineController<TState, TTrigger>
{
    public StateMachine<TState, TTrigger> StateMachine { get; }
    public TState State => StateMachine.State;

    public StateMachineController(TState initialState, Action<StateMachine<TState, TTrigger>> configure, Action? onInitialEntry = null)
    {
        StateMachine = new(initialState);
        configure(StateMachine);
        // Stateless never fires OnEntry for the initial state — invoke the idle-entry explicitly.
        onInitialEntry?.Invoke();
    }

    public bool IsInState(TState state) => StateMachine.IsInState(state);

    public bool CanFire(TTrigger trigger) => StateMachine.CanFire(trigger);

    public void Fire(TTrigger trigger)
    {
        if (StateMachine.CanFire(trigger))
        {
            StateMachine.Fire(trigger);
            return;
        }
        Debug.WriteLine($"[{typeof(TState).Name}] Ignored {trigger} in state {State}");
    }
}