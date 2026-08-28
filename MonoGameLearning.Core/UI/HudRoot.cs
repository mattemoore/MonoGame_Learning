using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.UI;

public sealed class HudRoot : UiBase
{
    private readonly PlayerBar _playerBar;
    private readonly EnemyBar _enemyBar;

    public HudRoot(PlayerBar playerBar, EnemyBar enemyBar)
    {
        _playerBar = playerBar;
        _enemyBar = enemyBar;
    }

    public override void Update(GameTime gameTime)
    {
        _playerBar.Update(gameTime);
        _enemyBar.Update(gameTime);
    }

    public override void Render(RenderContext context)
    {
        _playerBar.Render(context);
        _enemyBar.Render(context);
    }

    public override void DrawDebug(DebugDrawContext context)
    {
        _playerBar.DrawDebug(context);
        _enemyBar.DrawDebug(context);
    }
}