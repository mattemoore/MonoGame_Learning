namespace MonoGameLearning.Core.Combat;

public interface IDamageResponse
{
    bool IsAlive { get; }
    bool CanTakeDamage();
    void ReduceHealth(int amount);
    void OnDeath();
    void OnKnockdown(DamageInfo info);
    void OnHit(DamageInfo info);
}