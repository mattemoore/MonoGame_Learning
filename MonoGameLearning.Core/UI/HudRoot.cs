using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.UI;

public sealed class HudRoot(PlayerBar playerBar, EnemyBar enemyBar) : UiBase
{
    public override void Update(GameTime gameTime)
    {
        playerBar.Update(gameTime);
        enemyBar.Update(gameTime);
    }

    public override void Render(RenderContext context)
    {
        playerBar.Render(context);
        enemyBar.Render(context);
    }

    public override void DrawDebug(DebugDrawContext context)
    {
        playerBar.DrawDebug(context);
        enemyBar.DrawDebug(context);
    }
}