using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Entities.Pickups;

public class FoodPickupEntity : PickupBase
{
    public const int HealAmount = 15;

    public FoodPickupEntity(string name, Vector2 position, Texture2D texture)
        : base(name, position, texture) { }

    public override void OnPickup(IDamageable target) => target.Heal(HealAmount);
}
