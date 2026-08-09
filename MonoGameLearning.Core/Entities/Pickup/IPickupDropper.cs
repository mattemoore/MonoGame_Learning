using System.Collections.Generic;

namespace MonoGameLearning.Core.Entities.Pickup;

public interface IPickupDropper
{
    IReadOnlyList<PickupSpawnDef> CreateDrops();
}
