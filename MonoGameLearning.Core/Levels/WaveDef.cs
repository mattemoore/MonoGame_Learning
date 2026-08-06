using System.Collections.Generic;

namespace MonoGameLearning.Core.Levels;

public record WaveDef(float TriggerX, float EndX, List<EnemySpawnDef> Enemies)
{
}