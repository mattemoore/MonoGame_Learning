using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Game.Levels;

public record EnemySpawnDef(string Type, Vector2 Position);

public record WaveDef(float TriggerX, List<EnemySpawnDef> Enemies)
{
    // Contract: TriggerX must be a screen boundary: TriggerX = (i+1) * GAME_WIDTH.
    // TriggerX values must be strictly increasing.
    // FightAreaWidth is implicit (always GAME_WIDTH) and centered on TriggerX.
}