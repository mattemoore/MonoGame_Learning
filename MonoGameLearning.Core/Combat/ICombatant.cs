using System;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Combat;

public interface ICombatant : IHasHealth, IDamageable
{
    Faction Faction { get; }
    bool IsAlive { get; }
    event EventHandler Died;
    bool CanTakeDamage();
    void OnDeath();
    void OnKnockdown(DamageInfo info);
    void OnHit(DamageInfo info);
    void ReduceHealth(int amount);
}