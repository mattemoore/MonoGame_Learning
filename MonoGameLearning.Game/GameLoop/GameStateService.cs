using MonoGameLearning.Core;
using Stateless;

namespace MonoGameLearning.Game.GameLoop;

public class GameStateService
{
    public StateMachine<GameState, GameTrigger> StateMachine { get; }
    public GameState State => StateMachine.State;

    public GameStateService()
    {
        StateMachine = new StateMachine<GameState, GameTrigger>(GameState.TitleScreen);

        StateMachine.Configure(GameState.TitleScreen)
            .Permit(GameTrigger.StartGame, GameState.Playing)
            .Permit(GameTrigger.OpenSettings, GameState.Settings);

        StateMachine.Configure(GameState.Playing)
            .Permit(GameTrigger.PauseToggle, GameState.Paused)
            .Permit(GameTrigger.PlayerDied, GameState.GameOver)
            .Permit(GameTrigger.CompleteLevel, GameState.LevelComplete);

        StateMachine.Configure(GameState.Paused)
            .Permit(GameTrigger.PauseToggle, GameState.Playing)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen)
            .Permit(GameTrigger.OpenSettings, GameState.Settings);

        StateMachine.Configure(GameState.GameOver)
            .Permit(GameTrigger.StartGame, GameState.Playing)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);

        StateMachine.Configure(GameState.LevelComplete)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);

        StateMachine.Configure(GameState.Settings)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen)
            .Permit(GameTrigger.PauseToggle, GameState.Paused);
    }

    public void Fire(GameTrigger trigger)
    {
        if (StateMachine.CanFire(trigger))
        {
            StateMachine.Fire(trigger);
        }
    }
}