using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class CameraTargetXTests
{
    private const int GameWidth = 800;
    private static readonly RectangleF LevelBounds = new(0, 0, 1600, 900);

    [Test]
    public void PlayerAtLeftEdge_ClampsToHalfGameWidth()
    {
        float result = CameraController.ComputeTargetX(0, null, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void PlayerAtRightEdge_ClampsToTotalWidthMinusHalfGameWidth()
    {
        float result = CameraController.ComputeTargetX(1600, null, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void PlayerInMiddle_CameraFollowsExactly()
    {
        float result = CameraController.ComputeTargetX(600, null, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(600));
    }

    [Test]
    public void PlayerPastLeftEdge_ClampsToMin()
    {
        float result = CameraController.ComputeTargetX(-100, null, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void PlayerPastRightEdge_ClampsToMax()
    {
        float result = CameraController.ComputeTargetX(2000, null, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void PlayerAtExactBoundary_StaysAtBoundary()
    {
        float minResult = CameraController.ComputeTargetX(400, null, LevelBounds, GameWidth);
        float maxResult = CameraController.ComputeTargetX(1200, null, LevelBounds, GameWidth);
        Assert.That(minResult, Is.EqualTo(400));
        Assert.That(maxResult, Is.EqualTo(1200));
    }

    [Test]
    public void LevelWidthEqualsGameWidth_ClampsToCenter()
    {
        var narrowBounds = new RectangleF(0, 0, 800, 900);
        float minResult = CameraController.ComputeTargetX(0, null, narrowBounds, GameWidth);
        float maxResult = CameraController.ComputeTargetX(800, null, narrowBounds, GameWidth);
        Assert.That(minResult, Is.EqualTo(400));
        Assert.That(maxResult, Is.EqualTo(400));
    }

    [Test]
    public void LockedCenter_ReturnsLockedValue()
    {
        float result = CameraController.ComputeTargetX(999, 500f, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(500));
    }

    [Test]
    public void LockedCenter_PlayerMoved_StillReturnsLocked()
    {
        float result = CameraController.ComputeTargetX(100, 400f, LevelBounds, GameWidth);
        Assert.That(result, Is.EqualTo(400));
    }
}