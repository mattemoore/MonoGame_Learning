using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLearning.Core.Rendering;

public class DebugDrawContext(SpriteBatch spriteBatch, SpriteFont font)
{
    public SpriteBatch SpriteBatch { get; } = spriteBatch;
    public SpriteFont Font { get; } = font;
}