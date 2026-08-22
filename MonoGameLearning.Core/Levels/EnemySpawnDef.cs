using System.Collections.Generic;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Core.Levels;

public record EnemySpawnDef(string Type, SpawnSide Side, SpawnVertical Vertical, IReadOnlyList<PickupSpawnDef>? Drops = null, string? Weapon = null);