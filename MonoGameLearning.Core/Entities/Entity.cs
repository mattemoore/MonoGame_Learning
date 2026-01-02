using System.Buffers.Text;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Entities;

/// <summary>
/// Represents anything in the game that is interactable (e.g. players, enemies, platforms, pickups etc.)
/// </summary>
public abstract class Entity
{
    public Vector2 Position { get; set; }
    public float Rotation { get; set; } = 0.0f;
    public float Width { get; set; }
    public float Height { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;

    public Entity(Vector2 position, float width, float height)
    {
        Position = position;
        Width = width;
        Height = height;
    }

    public abstract void Update(GameTime gameTime);
}