using System;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Combat;

public interface ICombatant : IDamageable
{
    int Health { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    event EventHandler Died;
}
