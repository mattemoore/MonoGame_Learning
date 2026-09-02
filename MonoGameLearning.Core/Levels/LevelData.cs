using System.Collections.Generic;
using System.Diagnostics;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Core.Levels;

public sealed record LevelData(
    int BackgroundCount,
    int GameWidth,
    int GameHeight,
    float EndTriggerX,
    float WalkableTopY,
    IReadOnlyList<PropSpawnDef> Props,
    IReadOnlyList<PickupSpawnDef> Pickups,
    IReadOnlyList<WaveDef> WaveDefs)
{
    public RectangleF MovementBounds =>
        new(0, WalkableTopY, BackgroundCount * GameWidth, GameHeight - WalkableTopY);

    public static void Validate(LevelData level)
    {
        Debug.Assert(level.BackgroundCount >= 1, "Level must have at least one background.");

        var waveDefs = level.WaveDefs;
        float levelRightEdge = level.MovementBounds.Right;

        Debug.Assert(waveDefs.Count > 0, "Level must have at least one wave.");
        for (int i = 0; i < waveDefs.Count; i++)
        {
            var wave = waveDefs[i];

            Debug.Assert(wave.TriggerX > 0, $"Wave {i} TriggerX ({wave.TriggerX}) must be > 0.");
            Debug.Assert(wave.EndX > wave.TriggerX, $"Wave {i} EndX ({wave.EndX}) must be > TriggerX ({wave.TriggerX}).");
            Debug.Assert(wave.TriggerX < levelRightEdge, $"Wave {i} TriggerX ({wave.TriggerX}) must be < level right edge ({levelRightEdge}).");
            Debug.Assert(wave.EndX <= levelRightEdge, $"Wave {i} EndX ({wave.EndX}) must be <= level right edge ({levelRightEdge}).");

            if (i > 0)
            {
                Debug.Assert(wave.TriggerX > waveDefs[i - 1].TriggerX,
                    $"Wave {i} TriggerX ({wave.TriggerX}) must be > previous wave TriggerX ({waveDefs[i - 1].TriggerX}).");
                Debug.Assert(wave.EndX > waveDefs[i - 1].EndX,
                    $"Wave {i} EndX ({wave.EndX}) must be > previous wave EndX ({waveDefs[i - 1].EndX}).");
            }
        }
    }
}