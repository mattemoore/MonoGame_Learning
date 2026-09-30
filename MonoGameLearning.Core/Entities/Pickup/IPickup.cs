using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Core.Entities.Pickup;

public interface IPickup
{
    /// <summary>
    /// Whether <paramref name="target"/> may collect this pickup right now. Defaults to
    /// <c>true</c>. A pickup in flight (e.g. a dropped weapon arcing to the ground) returns
    /// <c>false</c> so the dropper cannot instantly re-grab it, and a weapon pickup returns
    /// <c>false</c> for an already-armed wielder so an actor holds at most one weapon.
    /// </summary>
    bool CanBeCollectedBy(IDamageable target) => true;

    void OnPickup(IDamageable target);
}
