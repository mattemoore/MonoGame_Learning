using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Entities.Pickups;

public class WeaponPickupEntity(string name, Vector2 position, WeaponDef weapon) : PickupBase(name, position, weapon.Texture)
{
    private readonly WeaponDef _weapon = weapon;

    /// <summary>
    /// An already-armed wielder cannot collect this: an actor holds at most one weapon and must
    /// drop it (melee, via knockdown/death) or throw it (throwable) before grabbing another.
    /// </summary>
    public override bool CanBeCollectedBy(IDamageable target) =>
        base.CanBeCollectedBy(target) && target is not IWeaponWielder { EquippedWeapon: not null };

    public override void OnPickup(IDamageable target)
    {
        if (target is IWeaponWielder wielder)
            wielder.EquipWeapon(_weapon);
    }
}