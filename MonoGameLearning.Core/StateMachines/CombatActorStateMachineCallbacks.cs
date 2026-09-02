using System;

namespace MonoGameLearning.Core.StateMachines;

public abstract class CombatActorStateMachineCallbacks
{
    public Action? OnIdleEntry { get; init; }
    public Action? OnAttackingEntry { get; init; }
    public Action? OnAttackingExit { get; init; }
    public Action? OnHurtEntry { get; init; }
    public Action? OnHurtExit { get; init; }
    public Action? OnKnockdownEntry { get; init; }
    public Action? OnKnockdownExit { get; init; }
    public Action? OnDyingEntry { get; init; }
    public Action? OnDyingExit { get; init; }
    public Action? OnDeadEntry { get; init; }
}