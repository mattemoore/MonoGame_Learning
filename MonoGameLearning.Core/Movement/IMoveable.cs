using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Movement;

public interface IMoveable
{
    Vector2 MovementDirection { get; set; }
    float Speed { get; }
    RectangleF MovementBounds { get; set; }
}