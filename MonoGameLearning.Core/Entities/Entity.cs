using System.Buffers.Text;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

/// <summary>
/// Represents anything in the game that is interactable (e.g. players, enemies, platforms, pickups etc.)
/// </summary>
public abstract class Entity(Vector2 position, int width, int height)
{
    public Vector2 Position { get; set; } = position;
    public float Rotation { get; set; } = 0.0f;
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public Vector2 Scale { get; set; } = Vector2.One;

    public abstract void Update(GameTime gameTime);
}