using Microsoft.Xna.Framework;
using MonoGameLearning.Core.StateMachines;
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
        StateController = EnemyStateMachine.Create(new EnemyStateMachineCallbacks
        {
            OnAttackingEntry = () => CurrentMove = AttackMove,
            OnAttackingExit = AttackingExitImpl,
            OnHurtEntry = HurtEntryImpl,
            OnHurtExit = HurtExitImpl,
            OnKnockdownEntry = KnockdownEntryImpl,
            OnKnockdownExit = KnockdownExitImpl,
            OnDyingEntry = DyingEntryImpl,
            OnDyingExit = DyingExitImpl,
            OnDeadEntry = DeadEntryImpl,
        });
        return StateController;
    }
}