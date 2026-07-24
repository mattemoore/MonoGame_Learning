using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IPickup
{
    void OnPickup(IDamageable target);
}
