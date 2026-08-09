using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.UI;

public abstract class UiBase(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), IUpdatable, IScreenRenderable, IDebugDrawable
{
    public bool Visible { get; set; } = true;
    public bool IsScreenSpace { get; init; }

    public abstract void Update(GameTime gameTime);
    public abstract void Render(RenderContext context);

    public virtual void DrawDebug(DebugDrawContext context) { }
}