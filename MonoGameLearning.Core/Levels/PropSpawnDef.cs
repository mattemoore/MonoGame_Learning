using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Levels;

public record PropSpawnDef(string Type, Vector2 Position, CollisionAnchor Anchor = CollisionAnchor.Top);