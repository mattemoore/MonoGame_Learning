using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Entities.Interfaces;

public interface IMoveableEntity
{
    Vector2 MovementDirection { get; set; }
    float Speed { get; }
    RectangleF MovementBounds { get; set; }
}