using MonoGame.Extended;

namespace MonoGameLearning.Core.Rendering;

public interface IRenderable
{
    RectangleF Frame { get; }
    void Render(RenderContext context);
}
