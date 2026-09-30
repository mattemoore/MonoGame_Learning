using System;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Core.Entities.Pickup;

public static class PickupService
{
    public static void ResolveOverlaps(EntityService entityManager, Entity player, Action<SfxId> playSfx)
    {
        if (player is not IDamageable { IsAlive: true } damageable) return;

        var playerFrame = player.Frame;
        var pickups = entityManager.GetCollidables(CollisionLayers.Pickups);
        for (int i = 0; i < pickups.Count; i++)
        {
            var pickup = pickups[i];
            if (pickup is not Entity { } pickupEntity) continue;
            if (!playerFrame.Intersects(pickupEntity.Frame)) continue;
            if (pickup is not IPickup pickupInterface) continue;
            if (!pickupInterface.CanBeCollectedBy(damageable)) continue;

            pickupInterface.OnPickup(damageable);
            playSfx(SfxId.PickupHeal);
            entityManager.Destroy(pickupEntity);
        }
    }
}