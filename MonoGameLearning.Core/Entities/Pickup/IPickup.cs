using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Core.Entities.Pickup;

public interface IPickup
{
    void OnPickup(IDamageable target);
}
