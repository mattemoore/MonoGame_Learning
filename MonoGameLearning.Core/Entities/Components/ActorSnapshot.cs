using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities.Components;

public readonly record struct ActorSnapshot(Vector2 Position, float HalfWidth, float HalfHeight);