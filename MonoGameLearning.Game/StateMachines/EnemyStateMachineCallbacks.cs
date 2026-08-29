using System;

namespace MonoGameLearning.Game.StateMachines;

public sealed class EnemyStateMachineCallbacks : CombatActorStateMachineCallbacks
{
    public Action OnChasingEntry { get; init; }
    public Action OnEnteringEntry { get; init; }
    public Action OnEnteringExit { get; init; }
}