using System;

namespace MonoGameLearning.Game.StateMachines;

public sealed class ActorStateMachineCallbacks
{
    public Action OnIdleEntry { get; init; }
    public Action OnMovingEntry { get; init; }
    public Action OnChasingEntry { get; init; }
    public Action OnEnteringEntry { get; init; }
    public Action OnEnteringExit { get; init; }
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