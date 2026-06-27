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
    }

    public abstract BackgroundRenderer CreateBackgroundRenderer(ContentManager content);

    public virtual void DrawDebug(DebugDrawContext context) { }
}