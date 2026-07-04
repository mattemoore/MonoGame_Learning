using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Rendering;

public class RenderContext(SpriteBatch spriteBatch, OrthographicCamera camera)
{
    public SpriteBatch SpriteBatch { get; } = spriteBatch;
    public OrthographicCamera Camera { get; } = camera;
}