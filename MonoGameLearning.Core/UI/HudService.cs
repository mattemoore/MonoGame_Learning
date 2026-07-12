using Microsoft.Xna.Framework.Graphics;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.UI;

public class HudService
{
    private readonly EnemyBar _enemyBar;
    private readonly HudRoot _root;

    public UiBase RootWidget => _root;

    public bool IsEnemyBarVisible => _enemyBar._visible;
    public IDamageable EnemyBarTarget => _enemyBar._displayTarget;
    public bool IsDeathLinger => _enemyBar._isDeathLinger;

    public HudService(IHudPlayerData player, SpriteFont font)
    {
        var playerBar = new PlayerBar(player, font);
        _enemyBar = new EnemyBar(font);
        _root = new HudRoot(playerBar, _enemyBar);
    }

    public void OnEnemyHit(IDamageable enemy) => _enemyBar.OnHit(enemy);

    public void SetProximityTarget(IDamageable target) => _enemyBar._proximityTarget = target;

    public void ClearTargetState() => _enemyBar.Reset();
}