using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Entities;

public class RenderContext(SpriteBatch spriteBatch, OrthographicCamera camera)
{
    public SpriteBatch SpriteBatch { get; } = spriteBatch;
    public OrthographicCamera Camera { get; } = camera;
}