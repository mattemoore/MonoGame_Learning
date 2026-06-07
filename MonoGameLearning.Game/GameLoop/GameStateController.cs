using Stateless;

namespace MonoGameLearning.Game.GameLoop;

public enum GameState
{
    TitleScreen,
    Playing,
    Paused,
    GameOver,
    LevelComplete
}

public enum GameTrigger
{
    StartGame,
    PauseToggle,
    PlayerDied,
    CompleteLevel,
    ReturnToTitle
}

public class GameStateController
{
    public StateMachine<GameState, GameTrigger> StateMachine { get; }
    public GameState State => StateMachine.State;

    public GameStateController()
    {
        StateMachine = new StateMachine<GameState, GameTrigger>(GameState.TitleScreen);

        StateMachine.Configure(GameState.TitleScreen)
            .Permit(GameTrigger.StartGame, GameState.Playing);

        StateMachine.Configure(GameState.Playing)
            .Permit(GameTrigger.PauseToggle, GameState.Paused)
            .Permit(GameTrigger.PlayerDied, GameState.GameOver)
            .Permit(GameTrigger.CompleteLevel, GameState.LevelComplete);

        StateMachine.Configure(GameState.Paused)
            .Permit(GameTrigger.PauseToggle, GameState.Playing)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);

        StateMachine.Configure(GameState.GameOver)
            .Permit(GameTrigger.StartGame, GameState.Playing)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);

        StateMachine.Configure(GameState.LevelComplete)
            .Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen);
    }

    public void Fire(GameTrigger trigger)
    {
        if (StateMachine.CanFire(trigger))
        {
            StateMachine.Fire(trigger);
        }
    }
}
