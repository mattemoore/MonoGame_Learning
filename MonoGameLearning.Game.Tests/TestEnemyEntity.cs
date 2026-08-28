using Microsoft.Xna.Framework;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.StateMachines;

namespace MonoGameLearning.Game.Tests;

class TestEnemyEntity(string name, Vector2 position, LevelDirector? director = null)
    : EnemyEntity(name, position, 1f, null!, null!, director!)
{
    public StateMachineController<EnemyState, EnemyTrigger>? StateController { get; private set; }

    protected override StateMachineController<EnemyState, EnemyTrigger> CreateStateController()
    {
        StateController = EnemyStateMachine.Create(new()
        {
            OnAttackingEntry = () => CurrentMove = AttackMove,
            OnAttackingExit = Callbacks.OnAttackingExit,
            OnHurtEntry = Callbacks.OnHurtEntry,
            OnHurtExit = Callbacks.OnHurtExit,
            OnKnockdownEntry = Callbacks.OnKnockdownEntry,
            OnKnockdownExit = Callbacks.OnKnockdownExit,
            OnDyingEntry = Callbacks.OnDyingEntry,
            OnDyingExit = Callbacks.OnDyingExit,
            OnDeadEntry = Callbacks.OnDeadEntry,
        });
        return StateController;
    }
}