using Gum;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;

namespace MonoGameLearning.Core.UI;

public class GumUiService
{
    private static GumService Svc => GumService.Default;

    public DebugOverlay DebugOverlay { get; private set; }

    public GumUiService() => DebugOverlay = null!;

    public void Initialize(Microsoft.Xna.Framework.Game game)
    {
        Svc.Initialize(game);
        ShapeRenderer.Self.Initialize();
        Svc.EnableExpandToWindow(1f);
        DebugOverlay = new DebugOverlay();
    }

    public void Update(GameTime gameTime) => Svc.Update(gameTime);

    public void Draw() => Svc.Draw();
}