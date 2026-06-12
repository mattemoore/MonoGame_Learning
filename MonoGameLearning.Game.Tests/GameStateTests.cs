using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests
{
    [TestFixture]
    public class GameStateTests
    {
        private GameStateController _controller;

        [SetUp]
        public void Setup() => _controller = new GameStateController();

        [Test]
        public void InitialState_ShouldBeTitleScreen() =>
            Assert.That(_controller.State, Is.EqualTo(GameState.TitleScreen));

        [Test]
        public void StartGame_FromTitleScreen_TransitionsToPlaying()
        {
            _controller.Fire(GameTrigger.StartGame);
            Assert.That(_controller.State, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void PauseToggle_FromPlaying_TransitionsToPausedAndBack()
        {
            _controller.Fire(GameTrigger.StartGame);
            _controller.Fire(GameTrigger.PauseToggle);
            Assert.That(_controller.State, Is.EqualTo(GameState.Paused));

            _controller.Fire(GameTrigger.PauseToggle);
            Assert.That(_controller.State, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void PlayerDied_FromPlaying_TransitionsToGameOver()
        {
            _controller.Fire(GameTrigger.StartGame);
            _controller.Fire(GameTrigger.PlayerDied);
            Assert.That(_controller.State, Is.EqualTo(GameState.GameOver));

            _controller.Fire(GameTrigger.ReturnToTitle);
            Assert.That(_controller.State, Is.EqualTo(GameState.TitleScreen));
        }

        [Test]
        public void Retry_FromGameOver_TransitionsToPlaying()
        {
            _controller.Fire(GameTrigger.StartGame);
            _controller.Fire(GameTrigger.PlayerDied);
            _controller.Fire(GameTrigger.StartGame);
            Assert.That(_controller.State, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void CompleteLevel_FromPlaying_TransitionsToLevelComplete()
        {
            _controller.Fire(GameTrigger.StartGame);
            _controller.Fire(GameTrigger.CompleteLevel);
            Assert.That(_controller.State, Is.EqualTo(GameState.LevelComplete));

            _controller.Fire(GameTrigger.ReturnToTitle);
            Assert.That(_controller.State, Is.EqualTo(GameState.TitleScreen));
        }

        [Test]
        public void InvalidTransition_ShouldBeIgnored()
        {
            _controller.Fire(GameTrigger.PauseToggle);
            Assert.That(_controller.State, Is.EqualTo(GameState.TitleScreen));
        }
    }
}
