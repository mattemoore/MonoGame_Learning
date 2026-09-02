using System;
using System.Diagnostics;
using Stateless;

namespace MonoGameLearning.Core.StateMachines;

public sealed class StateMachineController<TState, TTrigger>
{
    private readonly StateMachine<TState, TTrigger> _machine;

    public TState State => _machine.State;

    public StateMachineController(TState initialState, Action<StateMachine<TState, TTrigger>> configure, Action? onInitialEntry = null)
    {
        _machine = new(initialState);
        configure(_machine);
        // Stateless never fires OnEntry for the initial state — invoke the idle-entry explicitly.
        onInitialEntry?.Invoke();
    }

    public bool IsInState(TState state) => _machine.IsInState(state);

    public bool CanFire(TTrigger trigger) => _machine.CanFire(trigger);

    public void Fire(TTrigger trigger)
    {
        if (_machine.CanFire(trigger))
        {
            _machine.Fire(trigger);
            return;
        }
        Debug.WriteLine($"[{typeof(TState).Name}] Ignored {trigger} in state {State}");
    }

    public void SubscribeTransitions(Action<StateMachine<TState, TTrigger>.Transition> handler) =>
        _machine.OnTransitioned(handler);
}