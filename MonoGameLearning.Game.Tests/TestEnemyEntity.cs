using Microsoft.Xna.Framework;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.StateMachines;

namespace MonoGameLearning.Game.Tests;

class TestEnemyEntity(string name, Vector2 position, LevelDirector? director = null)
    : EnemyEntity(name, position, 1f, null!, null!, director is null ? () => default : () => director.CurrentWorld)
{
    public StateMachineController<EnemyState, EnemyTrigger>? StateController { get; private set; }

    protected override StateMachineController<EnemyState, EnemyTrigger> CreateStateController()
    {
        StateController = EnemyStateMachine.Create(new()
        {
            OnAttackingEntry = () => CurrentMove = AttackMove,
            OnAttackingExit = OnAttackingExitHook,
            OnHurtEntry = OnHurtEntryHook,
            OnHurtExit = OnHurtExitHook,
            OnKnockdownEntry = OnKnockdownEntryHook,
            OnKnockdownExit = OnKnockdownExitHook,
            OnDyingEntry = OnDyingEntryHook,
            OnDyingExit = OnDyingExitHook,
            OnDeadEntry = OnDeadEntryHook,
        });
        return StateController;
    }
}