namespace MonoGameLearning.Core.Combat;

public static class CombatService
{
    public static bool ApplyDamage(ICombatant target, DamageInfo info)
    {
        if (!target.CanTakeDamage()) return false;

        target.ReduceHealth(info.Amount);

        if (!target.IsAlive) { target.OnDeath(); return true; }
        if (info.Knockdown) { target.OnKnockdown(info); return true; }
        target.OnHit(info);
        return true;
    }
}