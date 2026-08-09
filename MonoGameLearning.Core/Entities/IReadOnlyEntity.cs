using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

public interface IReadOnlyEntity
{
    Vector2 Position { get; set; }
    int Width { get; }
    int Height { get; }
}