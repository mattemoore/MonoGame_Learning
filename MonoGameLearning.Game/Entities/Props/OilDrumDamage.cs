using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Game.Entities.Props;

public static class OilDrumDamage
{
    // Maps an AttackStrength tier to a damage value instead of using
    // DamageInfo.Amount like every other IDamageable. The drum's max HP is 6 and
    // its durability is meant to read as "N hits per attack weight", not as health
    // consumed by arbitrary numbers — otherwise rebalancing attack amounts
    // elsewhere would silently change the drum's intended hit count (heavy
    // one-shots, light needs three). Keep these values in sync with the drum's
    // maxHealth.
    public static int GetEffectiveDamage(AttackStrength strength) => strength switch
    {
        AttackStrength.Heavy => 6,
        AttackStrength.Medium => 3,
        _ => 2,
    };
}