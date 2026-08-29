namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class GameLoopLivesTests
{
    [Test]
    public void TryConsumeLife_FromThree_DecrementsToTwo_ReturnsTrue()
    {
        int lives = 3;
        Assert.That(global::MonoGameLearning.Game.GameLoop.GameLoop.TryConsumeLife(ref lives), Is.True);
        Assert.That(lives, Is.EqualTo(2));
    }

    [Test]
    public void TryConsumeLife_FromOne_DecrementsToZero_ReturnsTrue()
    {
        int lives = 1;
        Assert.That(global::MonoGameLearning.Game.GameLoop.GameLoop.TryConsumeLife(ref lives), Is.True);
        Assert.That(lives, Is.EqualTo(0));
    }

    [Test]
    public void TryConsumeLife_FromZero_ReturnsFalse_DoesNotDecrement()
    {
        int lives = 0;
        Assert.That(global::MonoGameLearning.Game.GameLoop.GameLoop.TryConsumeLife(ref lives), Is.False);
        Assert.That(lives, Is.EqualTo(0));
    }

    [Test]
    public void TryConsumeLife_FromNegative_ReturnsFalse_DoesNotDecrement()
    {
        int lives = -1;
        Assert.That(global::MonoGameLearning.Game.GameLoop.GameLoop.TryConsumeLife(ref lives), Is.False);
        Assert.That(lives, Is.EqualTo(-1));
    }
}