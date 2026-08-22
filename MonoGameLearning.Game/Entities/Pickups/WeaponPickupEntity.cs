using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Entities.Pickups;

public class WeaponPickupEntity(string name, Vector2 position, MeleeWeaponDef weapon) : PickupBase(name, position, weapon.Texture)
{
    private readonly MeleeWeaponDef _weapon = weapon;

    public MeleeWeaponDef Weapon => _weapon;

    public override void OnPickup(IDamageable target)
    {
        // TODO: Remove type check like in FoodPickupEntity (see TODO.md item 2)
        if (target is CombatActorBase actor)
            actor.EquipWeapon(_weapon);
    }
}