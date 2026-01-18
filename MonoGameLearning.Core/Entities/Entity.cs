using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLearning.Core.Entities;

public abstract class Entity(string name, Vector2 position, int width, int height, float rotation = 0.0f)
{
    public Vector2 Position { get; set; } = position;
    public float Rotation { get; set; } = rotation;
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
    public string Name { get; } = name;

    public abstract void Update(GameTime gameTime);
    public abstract void DrawDebug(SpriteBatch spriteBatch);
}