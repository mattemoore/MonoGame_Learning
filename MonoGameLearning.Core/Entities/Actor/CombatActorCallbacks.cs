using System;

namespace MonoGameLearning.Core.Entities.Actor;

public sealed class CombatActorCallbacks
{
    public required Action OnAttackingExit { get; init; }
    public required Action OnHurtEntry { get; init; }
    public required Action OnHurtExit { get; init; }
    public required Action OnKnockdownEntry { get; init; }
    public required Action OnKnockdownExit { get; init; }
    public required Action OnDyingEntry { get; init; }
    public required Action OnDyingExit { get; init; }
    public required Action OnDeadEntry { get; init; }
}