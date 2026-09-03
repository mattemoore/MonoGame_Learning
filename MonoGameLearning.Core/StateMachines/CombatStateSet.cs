namespace MonoGameLearning.Core.StateMachines;

public readonly record struct CombatStateSet<TState>(
    TState Attacking,
    TState Hurt,
    TState KnockedDown,
    TState Dying,
    TState Dead);