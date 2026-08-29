using System;

namespace MonoGameLearning.Game.StateMachines;

public sealed class PlayerStateMachineCallbacks : CombatActorStateMachineCallbacks
{
    public Action OnMovingEntry { get; init; }
}