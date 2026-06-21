namespace MonoGameLearning.Core.Combat;

public enum AttackStrength { Light, Medium, Heavy }

public readonly record struct DamageInfo
{
    public int Amount { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
}