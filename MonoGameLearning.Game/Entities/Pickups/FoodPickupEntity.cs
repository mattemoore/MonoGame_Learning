using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Entities.Pickups;

public class FoodPickupEntity(string name, Vector2 position, Texture2D texture)
    : PickupBase(name, position, texture)
{
    public const int HealAmount = 15;

    public override void OnPickup(IDamageable target) => target.Heal(HealAmount);
}
