using Microsoft.Xna.Framework;
using MonoGame.Extended.Animations;
using MonoGameLearning.Core.StateMachines;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.StateMachines;

namespace MonoGameLearning.Game.Tests;

class TestEnemyEntity(string name, Vector2 position, LevelDirector? director = null)
    : EnemyEntity(name, position, 1f, null!, null!, director is null ? () => default : () => director.CurrentWorld)
{
    public StateMachineController<EnemyState, EnemyTrigger>? StateController { get; private set; }

    /// <summary>Test hook: simulate the current animation reaching its end.</summary>
    public void CompleteAnimation() => OnAnimationCompleted(null!, AnimationEventTrigger.AnimationCompleted);

    /// <summary>Test hook: drive the incapacitated (knockdown/death) update path headlessly.</summary>
    public bool TickIncapacitated(GameTime gameTime) => TryHandleIncapacitatedUpdate(gameTime);

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