using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}