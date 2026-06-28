using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Entities.Helpers;

public readonly record struct WorldSnapshot(
    Vector2 PlayerPosition,
    RectangleF WalkableBounds,
    IReadOnlyList<ActorSnapshot> Enemies,
    IReadOnlyList<ActorSnapshot> Props);