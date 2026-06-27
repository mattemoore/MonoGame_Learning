namespace MonoGameLearning.Core.Combat;

public static class OilDrumDamage
{
    public static int GetEffectiveDamage(AttackStrength strength) => strength switch
    {
        AttackStrength.Heavy => 6,
        AttackStrength.Medium => 3,
        _ => 2,
    };
}