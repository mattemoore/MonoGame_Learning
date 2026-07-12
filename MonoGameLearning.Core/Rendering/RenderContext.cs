using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace MonoGameLearning.Core.Rendering;

public readonly record struct RenderContext(SpriteBatch SpriteBatch, OrthographicCamera Camera);