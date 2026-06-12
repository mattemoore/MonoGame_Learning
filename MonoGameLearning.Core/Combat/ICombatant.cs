using System;

namespace MonoGameLearning.Core.Combat;

public interface ICombatant
{
    int Health { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    event EventHandler Died;
    void TakeDamage(int amount);
}
