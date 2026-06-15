using System;
using Stateless;

namespace MonoGameLearning.Game.Entities.Props;

public class OilDrumStateControllerConfig
{
    public Action OnNormalEntry { get; init; }
    public Action OnHitStunEntry { get; init; }
}

public enum OilDrumState
{
    Normal,
    HitStun
}

public enum OilDrumTrigger
{
    Hit,
    HitStunCompleted
}

public class OilDrumStateController
{
    public StateMachine<OilDrumState, OilDrumTrigger> StateMachine { get; }
    public OilDrumState State => StateMachine.State;

    public OilDrumStateController(OilDrumStateControllerConfig config = null)
    {
        StateMachine = new(OilDrumState.Normal);

        StateMachine.Configure(OilDrumState.Normal)
            .OnEntry(_ => config?.OnNormalEntry?.Invoke())
            .Permit(OilDrumTrigger.Hit, OilDrumState.HitStun)
            .Ignore(OilDrumTrigger.HitStunCompleted);

        StateMachine.Configure(OilDrumState.HitStun)
            .OnEntry(_ => config?.OnHitStunEntry?.Invoke())
            .Permit(OilDrumTrigger.HitStunCompleted, OilDrumState.Normal)
            .Ignore(OilDrumTrigger.Hit);

        StateMachine.Activate();
    }

    public bool IsInState(OilDrumState state) => StateMachine.IsInState(state);

    public bool CanFire(OilDrumTrigger trigger) => StateMachine.CanFire(trigger);

    public void Fire(OilDrumTrigger trigger)
    {
        if (StateMachine.CanFire(trigger))
            StateMachine.Fire(trigger);
    }
}
