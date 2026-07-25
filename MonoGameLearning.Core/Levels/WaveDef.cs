using System.Collections.Generic;

namespace MonoGameLearning.Core.Levels;

public record EnemySpawnDef(string Type, SpawnSide Side, SpawnVertical Vertical);

public record WaveDef(float TriggerX, float EndX, List<EnemySpawnDef> Enemies)
{
}