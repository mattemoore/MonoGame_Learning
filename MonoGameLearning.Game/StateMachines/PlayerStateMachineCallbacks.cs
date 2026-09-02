using System;
using MonoGameLearning.Core.StateMachines;

namespace MonoGameLearning.Game.StateMachines;

public sealed class PlayerStateMachineCallbacks : CombatActorStateMachineCallbacks
{
    public Action OnMovingEntry { get; init; }
}