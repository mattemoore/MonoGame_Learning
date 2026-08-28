using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Movement;

public interface IMoveable : ISpatial
{
    Vector2 MovementDirection { get; set; }
    float Speed { get; }
    RectangleF MovementBounds { get; set; }
}