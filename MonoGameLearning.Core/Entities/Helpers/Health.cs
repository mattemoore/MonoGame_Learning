using System;

namespace MonoGameLearning.Core.Entities.Helpers;

public class Health(int maxHealth)
{
    public int MaxHealth { get; } = maxHealth;
    public int Value { get; private set; } = maxHealth;
    public bool IsAlive => Value > 0;

    public void Subtract(int amount) => Value = Math.Max(0, Value - amount);
    public void SetToMax() => Value = MaxHealth;
}