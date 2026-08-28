using System;

namespace MonoGameLearning.Core.Combat;

public interface IDamageable
{
    string Name { get; }
    int Health { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    event EventHandler Died;
    void TakeDamage(DamageInfo info);
    void Heal(int amount);
}