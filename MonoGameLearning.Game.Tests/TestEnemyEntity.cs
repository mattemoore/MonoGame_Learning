using Microsoft.Xna.Framework;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Tests;

class TestEnemyEntity(string name, Vector2 position, LevelDirector? director = null)
    : EnemyEntity(name, position, 1f, null!, null!, director!)
{
    protected override EnemyStateController CreateStateController()
    {
        return new EnemyStateController(new()
        {
            OnAttackingExit = OnAttackingExit,
            OnHurtEntry = OnHurtEntry,
            OnHurtExit = OnHurtExit,
            OnKnockdownEntry = OnKnockdownEntryAction,
            OnKnockdownExit = OnKnockdownExitAction,
            OnDyingEntry = OnDyingEntryAction,
            OnDyingExit = OnDyingExitAction,
            OnDeadEntry = OnDeadEntryAction,
        });
    }
}