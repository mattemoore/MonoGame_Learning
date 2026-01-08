using System.Buffers.Text;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

public abstract class Entity(Vector2 position, int width, int height, float rotation = 0.0f)
{
    public Vector2 Position { get; set; } = position;
    public float Rotation { get; set; } = rotation;
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;

    public abstract void Update(GameTime gameTime);
}