using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Rendering;

namespace MonoGameLearning.Game.Levels;

public abstract class Level
{
    public List<WaveDef> WaveDefs { get; }
    public abstract int BackgroundCount { get; }
    public abstract float EndTriggerX { get; }
    public abstract List<PropSpawnDef> Props { get; }
    public abstract float WalkableTopY { get; }

    public RectangleF MovementBounds { get; }

    protected Level(List<WaveDef> waveDefs, int gameWidth, int gameHeight)
    {
        WaveDefs = waveDefs;
        MovementBounds = new RectangleF(0, WalkableTopY, BackgroundCount * gameWidth, gameHeight - WalkableTopY);
        Debug.Assert(BackgroundCount >= 1, "Level must have at least one background.");
        ValidateWaveDefs(waveDefs, MovementBounds.Right);
    }

    private static void ValidateWaveDefs(List<WaveDef> waveDefs, float levelRightEdge)
    {
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

    public abstract BackgroundRenderer CreateBackgroundRenderer(ContentManager content);

    public virtual void DrawDebug(DebugDrawContext context) { }
}