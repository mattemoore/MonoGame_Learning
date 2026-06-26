using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Game.Levels;

public record EnemySpawnDef(string Type, Vector2 Position);

public record WaveDef(float TriggerX, float FightAreaWidth, List<EnemySpawnDef> Enemies)
{
    public static void ValidateWaveDefs(IReadOnlyList<WaveDef> waveDefs, float viewportWidth)
    {
        if (waveDefs is null || waveDefs.Count == 0)
            throw new InvalidOperationException("WaveDefs must not be null or empty.");

        for (int i = 0; i < waveDefs.Count; i++)
        {
            var wave = waveDefs[i];
            if (wave.FightAreaWidth <= 0)
                throw new InvalidOperationException($"WaveDef at index {i}: FightAreaWidth ({wave.FightAreaWidth}) must be greater than 0.");
            if (wave.FightAreaWidth > viewportWidth)
                throw new InvalidOperationException($"WaveDef at index {i}: FightAreaWidth ({wave.FightAreaWidth}) exceeds viewport width ({viewportWidth}).");
        }

        for (int i = 0; i < waveDefs.Count - 1; i++)
        {
            var current = waveDefs[i];
            var next = waveDefs[i + 1];
            if (current.TriggerX >= next.TriggerX)
                throw new InvalidOperationException(
                    $"WaveDef at index {i} (TriggerX={current.TriggerX}) must have a smaller TriggerX than wave at index {i + 1} (TriggerX={next.TriggerX}). Wave triggers must be strictly increasing for left-to-right progression.");

            float currentRightEdge = current.TriggerX + current.FightAreaWidth / 2f;
            float nextLeftEdge = next.TriggerX - next.FightAreaWidth / 2f;
            if (currentRightEdge > nextLeftEdge)
                throw new InvalidOperationException(
                    $"WaveDef at index {i} fight area (right edge={currentRightEdge}) overlaps with wave at index {i + 1} fight area (left edge={nextLeftEdge}).");
        }
    }
}
