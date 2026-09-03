using Stateless;

namespace MonoGameLearning.Core.StateMachines;

/// <summary>
/// The five combat states (Attacking/Hurt/KnockedDown/Dying/Dead) share an identical transition
/// table across every combat actor. This builder configures them once, parameterized over the
/// actor's own state/trigger enums and its two exclusive movement triggers; Game-side machines
/// keep their exclusive states (Moving/Idling vs Entering/Chasing/Idle) and delegate here.
/// </summary>
public static class CombatStateMachineConfigurator
{
    public static void ConfigureCombatStates<TState, TTrigger>(
        StateMachine<TState, TTrigger> machine,
        CombatActorStateMachineCallbacks callbacks,
        TState returnState,
        CombatStateSet<TState> states,
        CombatTriggerSet<TTrigger> triggers,
        TTrigger movementStart,
        TTrigger movementStop)
    {
        machine.Configure(states.Attacking)
            .OnEntry(_ => callbacks.OnAttackingEntry?.Invoke())
            .OnExit(_ => callbacks.OnAttackingExit?.Invoke())
            .Permit(triggers.AttackCompleted, returnState)
            .Permit(triggers.TakeDamage, states.Hurt)
            .Permit(triggers.TakeKnockdown, states.KnockedDown)
            .Permit(triggers.Die, states.Dying)
            .Ignore(movementStart)
            .Ignore(movementStop)
            .Ignore(triggers.AttackStart);

        machine.Configure(states.Hurt)
            .OnEntry(_ => callbacks.OnHurtEntry?.Invoke())
            .OnExit(_ => callbacks.OnHurtExit?.Invoke())
            .Permit(triggers.HurtCompleted, returnState)
            .Permit(triggers.TakeKnockdown, states.KnockedDown)
            .Permit(triggers.Die, states.Dying)
            .Ignore(triggers.TakeDamage)
            .Ignore(triggers.AttackStart)
            .Ignore(movementStart)
            .Ignore(movementStop)
            .Ignore(triggers.AttackCompleted);

        machine.Configure(states.KnockedDown)
            .OnEntry(_ => callbacks.OnKnockdownEntry?.Invoke())
            .OnExit(_ => callbacks.OnKnockdownExit?.Invoke())
            .Permit(triggers.KnockdownCompleted, returnState)
            .Permit(triggers.Die, states.Dying)
            .Ignore(triggers.TakeDamage)
            .Ignore(triggers.TakeKnockdown)
            .Ignore(triggers.AttackStart)
            .Ignore(triggers.AttackCompleted)
            .Ignore(movementStart)
            .Ignore(movementStop)
            .Ignore(triggers.HurtCompleted);

        machine.Configure(states.Dying)
            .OnEntry(_ => callbacks.OnDyingEntry?.Invoke())
            .OnExit(_ => callbacks.OnDyingExit?.Invoke())
            .Permit(triggers.DeathCompleted, states.Dead)
            .Ignore(triggers.TakeDamage)
            .Ignore(triggers.Die)
            .Ignore(triggers.HurtCompleted)
            .Ignore(triggers.AttackStart)
            .Ignore(movementStart)
            .Ignore(movementStop)
            .Ignore(triggers.AttackCompleted)
            .Ignore(triggers.TakeKnockdown)
            .Ignore(triggers.KnockdownCompleted);

        machine.Configure(states.Dead)
            .OnEntry(_ => callbacks.OnDeadEntry?.Invoke())
            .Ignore(triggers.TakeDamage)
            .Ignore(triggers.Die)
            .Ignore(triggers.HurtCompleted)
            .Ignore(triggers.DeathCompleted)
            .Ignore(triggers.AttackStart)
            .Ignore(movementStart)
            .Ignore(movementStop)
            .Ignore(triggers.AttackCompleted)
            .Ignore(triggers.TakeKnockdown)
            .Ignore(triggers.KnockdownCompleted);
    }
}