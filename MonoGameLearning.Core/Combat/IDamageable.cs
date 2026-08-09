using System;

namespace MonoGameLearning.Core.Combat;

public interface IDamageable
{
    Faction Faction { get; }
    int Health { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    event EventHandler Died;
    void TakeDamage(DamageInfo info);
    void Heal(int amount);
}