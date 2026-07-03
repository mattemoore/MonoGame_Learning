using Microsoft.Xna.Framework;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Tests;

class TestEnemyEntity(string name, Vector2 position)
    : EnemyEntity(name, position, 1f, null!, null)
{
    protected override EnemyStateController CreateStateController()
    {
        return new EnemyStateController(new()
        {
            OnAttackingExit = AttackingExit(),
            OnHurtEntry = HurtEntry(),
            OnHurtExit = HurtExit(),
            OnKnockdownEntry = KnockdownEntry(),
            OnKnockdownExit = KnockdownExit(),
            OnDyingEntry = DyingEntry(),
            OnDyingExit = DyingExit(),
            OnDeadEntry = DeadEntry(),
        });
    }
}