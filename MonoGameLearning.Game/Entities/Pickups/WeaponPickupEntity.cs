using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Entities.Pickups;

public class WeaponPickupEntity(string name, Vector2 position, MeleeWeaponDef weapon) : PickupBase(name, position, weapon.Texture)
{
    private readonly MeleeWeaponDef _weapon = weapon;

    public override void OnPickup(IDamageable target)
    {
        if (target is IWeaponWielder wielder)
            wielder.EquipWeapon(_weapon);
    }
}