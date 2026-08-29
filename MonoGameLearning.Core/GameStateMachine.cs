using MonoGameLearning.Core.StateMachines;
using Stateless;

namespace MonoGameLearning.Core;

public static class GameStateMachine
{
    public static StateMachineController<GameState, GameTrigger> Create() =>
        new(GameState.TitleScreen, Configure);

    private static void Configure(StateMachine<GameState, GameTrigger> sm)
    {
        sm.Configure(GameState.TitleScreen)
            .Permit(GameTrigger.StartGame, GameState.Playing)
            .Permit(GameTrigger.OpenSettings, GameState.Settings);

        sm.Configure(GameState.Playing)
            .Permit(GameTrigger.PauseToggle, GameState.Paused)
            .Permit(GameTrigger.PlayerDied, GameState.GameOver)
            .Permit(GameTrigger.CompleteLevel, GameState.LevelComplete);

        sm.Configure(GameState.Paused)
            .Permit(GameTrigger.PauseToggle, GameState.Playing)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen)
            .Permit(GameTrigger.OpenSettings, GameState.Settings);

        sm.Configure(GameState.GameOver)
            .Permit(GameTrigger.StartGame, GameState.Playing)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);

        sm.Configure(GameState.LevelComplete)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);

        sm.Configure(GameState.Settings)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen)
            .Permit(GameTrigger.PauseToggle, GameState.Paused);
    }
}