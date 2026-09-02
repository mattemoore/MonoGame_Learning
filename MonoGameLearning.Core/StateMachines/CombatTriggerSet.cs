namespace MonoGameLearning.Core.StateMachines;

public readonly record struct CombatTriggerSet<TTrigger>(
    TTrigger AttackStart,
    TTrigger AttackCompleted,
    TTrigger TakeDamage,
    TTrigger TakeKnockdown,
    TTrigger KnockdownCompleted,
    TTrigger HurtCompleted,
    TTrigger Die,
    TTrigger DeathCompleted);