using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Game.Levels;

public record EnemySpawnDef(string Type, Vector2 Position);

public record WaveDef(float TriggerX, float EndX, List<EnemySpawnDef> Enemies)
{
    // Contract: TriggerX must be a screen boundary: TriggerX = (i+1) * GAME_WIDTH.
    // EndX must be > TriggerX (wave extends to the right).
    // TriggerX values must be strictly increasing.
    // EndX values must be strictly increasing.
}