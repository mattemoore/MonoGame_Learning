using System.Collections.Generic;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IPickupDropper
{
    IReadOnlyList<PickupSpawnDef> CreateDrops();
}
