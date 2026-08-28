using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.UI;

public abstract class UiBase : IUpdatable, IScreenRenderable, IDebugDrawable
{
    public bool Visible { get; set; } = true;
    public Vector2 Position { get; set; }

    public abstract void Update(GameTime gameTime);
    public abstract void Render(RenderContext context);

    public virtual void DrawDebug(DebugDrawContext context) { }
}