namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IDamageable
{
    void TakeDamage(int amount, bool knockdown = false);
}