using System;
using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IDamageable
{
    Faction Faction { get; }
    int Health { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    event EventHandler Died;
    bool CanTakeDamage();
    void TakeDamage(DamageInfo info);
    void ReduceHealth(int amount);
    void OnDeath();
    void OnKnockdown(DamageInfo info);
    void OnHit(DamageInfo info);
}