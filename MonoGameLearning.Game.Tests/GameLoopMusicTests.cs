using MonoGameLearning.Core;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class GameLoopMusicTests
{
    [Test]
    public void ApplyMusicForState_ToPaused_SetsPaused()
    {
        var mgr = new AudioService();
        GameLoopRules.ApplyMusicForState(mgr, GameState.Playing, GameState.Paused);
        Assert.That(mgr.IsPausedForTest, Is.True);
    }

    [Test]
    public void ApplyMusicForState_FromPausedToPlaying_Unpauses()
    {
        var mgr = new AudioService();
        GameLoopRules.ApplyMusicForState(mgr, GameState.Playing, GameState.Paused);
        Assert.That(mgr.IsPausedForTest, Is.True);

        GameLoopRules.ApplyMusicForState(mgr, GameState.Paused, GameState.Playing);
        Assert.That(mgr.IsPausedForTest, Is.False);
    }

    [Test]
    public void ApplyMusicForState_FromPausedToTitle_Unpauses()
    {
        var mgr = new AudioService();
        GameLoopRules.ApplyMusicForState(mgr, GameState.Playing, GameState.Paused);
        Assert.That(mgr.IsPausedForTest, Is.True);

        GameLoopRules.ApplyMusicForState(mgr, GameState.Paused, GameState.TitleScreen);
        Assert.That(mgr.IsPausedForTest, Is.False);
    }

    [Test]
    public void ApplyMusicForState_ToPlaying_DoesNotThrow()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => GameLoopRules.ApplyMusicForState(mgr, GameState.TitleScreen, GameState.Playing));
    }

    [Test]
    public void ApplyMusicForState_ToGameOver_DoesNotThrow()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => GameLoopRules.ApplyMusicForState(mgr, GameState.Playing, GameState.GameOver));
    }

    [Test]
    public void ApplyMusicForState_ToLevelComplete_DoesNotThrow()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => GameLoopRules.ApplyMusicForState(mgr, GameState.Playing, GameState.LevelComplete));
    }

    [Test]
    public void ApplyMusicForState_ToTitleOrSettings_DoesNotThrow()
    {
        var mgr = new AudioService();
        Assert.DoesNotThrow(() => GameLoopRules.ApplyMusicForState(mgr, GameState.GameOver, GameState.TitleScreen));
        Assert.DoesNotThrow(() => GameLoopRules.ApplyMusicForState(mgr, GameState.TitleScreen, GameState.Settings));
    }
}