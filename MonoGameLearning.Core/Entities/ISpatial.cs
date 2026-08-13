using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

public interface ISpatial
{
    Vector2 Position { get; set; }
    int Width { get; }
    int Height { get; }
}