using System.Collections.Generic;
using MonoGameLearning.Core.Levels;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IPickupDropper
{
    IReadOnlyList<PickupSpawnDef> CreateDrops();
}
