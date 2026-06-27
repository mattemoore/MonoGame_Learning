using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class CameraTargetXTests
{
    private const int GameWidth = 800;
    private static readonly RectangleF LevelBounds = new(0, 0, 1600, 900);

    private static float TargetX(float playerX, float currentCameraCenterX, float? waveEndX = null)
        => CameraController.ComputeTargetX(playerX, currentCameraCenterX, LevelBounds, GameWidth, waveEndX);

    [Test]
    public void PlayerAtLeftEdge_ClampsToHalfGameWidth()
    {
        float result = TargetX(0, 0);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void PlayerAtRightEdge_ClampsToTotalWidthMinusHalfGameWidth()
    {
        float result = TargetX(1600, 1600);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void PlayerInDeadZone_CameraStaysAtCurrentCenter()
    {
        float result = TargetX(600, 600);
        Assert.That(result, Is.EqualTo(600));
    }

    [Test]
    public void PlayerPastLeftEdge_ClampsToMin()
    {
        float result = TargetX(-100, -100);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void PlayerPastRightEdge_ClampsToMax()
    {
        float result = TargetX(2000, 2000);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void PlayerAtExactBoundary_StaysAtBoundary()
    {
        float minResult = TargetX(400, 400);
        float maxResult = TargetX(1200, 1200);
        Assert.That(minResult, Is.EqualTo(400));
        Assert.That(maxResult, Is.EqualTo(1200));
    }

    [Test]
    public void LevelWidthEqualsGameWidth_ClampsToCenter()
    {
        var narrowBounds = new RectangleF(0, 0, 800, 900);
        float minResult = CameraController.ComputeTargetX(0, 0, narrowBounds, GameWidth);
        float maxResult = CameraController.ComputeTargetX(800, 800, narrowBounds, GameWidth);
        Assert.That(minResult, Is.EqualTo(400));
        Assert.That(maxResult, Is.EqualTo(400));
    }

    [Test]
    public void DeadZone_PlayerInCenter_NoCameraMovement()
    {
        float currentCenter = 800;
        float result = TargetX(800, currentCenter);
        Assert.That(result, Is.EqualTo(currentCenter));
    }

    [Test]
    public void DeadZone_PlayerAtLeftEdgeOfDeadZone_Stays()
    {
        float currentCenter = 800;
        float result = TargetX(600, currentCenter);
        Assert.That(result, Is.EqualTo(currentCenter));
    }

    [Test]
    public void DeadZone_PlayerAtRightEdgeOfDeadZone_Stays()
    {
        float currentCenter = 800;
        float result = TargetX(1000, currentCenter);
        Assert.That(result, Is.EqualTo(currentCenter));
    }

    [Test]
    public void DeadZone_PlayerPastLeftEdge_CameraDoesNotMoveLeft()
    {
        float currentCenter = 800;
        float playerX = 500;
        float result = TargetX(playerX, currentCenter);
        Assert.That(result, Is.EqualTo(currentCenter));
    }

    [Test]
    public void DeadZone_PlayerPastRightEdge_CameraSnapsToPushPlayerToDeadZoneEdge()
    {
        float currentCenter = 800;
        float playerX = 1100;
        float result = TargetX(playerX, currentCenter);
        Assert.That(result, Is.EqualTo(900f));
    }

    [Test]
    public void DeadZone_PlayerAtLeftViewportEdge_CameraAtBoundary_StaysAtBoundary()
    {
        float result = TargetX(0, 400);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void DeadZone_PlayerSlightlyRightOfLeftDeadZoneEdge_NoCameraMovement()
    {
        float currentCenter = 800;
        float result = TargetX(601, currentCenter);
        Assert.That(result, Is.EqualTo(currentCenter));
    }

    [Test]
    public void DeadZone_PlayerAtRightViewportEdge_CameraAtBoundary_StaysAtBoundary()
    {
        float result = TargetX(1600, 1200);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void DeadZone_PlayerFarOutsideLeft_CameraDoesNotMoveLeft()
    {
        float result = TargetX(100, 800);
        Assert.That(result, Is.EqualTo(800));
    }

    [Test]
    public void DeadZone_PlayerFarOutsideRight_CameraClampsToRightBoundary()
    {
        float result = TargetX(1500, 800);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void WaveEndCap_CameraRightEdgeCappedAtEnd()
    {
        float result = TargetX(1500, 800, waveEndX: 1200f);
        Assert.That(result, Is.EqualTo(800f));
    }

    [Test]
    public void WaveEndCap_PlayerPastEnd_ClampsToEnd()
    {
        float result = TargetX(1300, 1000, waveEndX: 1200f);
        Assert.That(result, Is.EqualTo(800f));
    }

    [Test]
    public void WaveEndCap_NoEffectWhenNull_UsesLevelBounds()
    {
        float result = TargetX(1500, 800);
        Assert.That(result, Is.EqualTo(1200f));
    }

    [Test]
    public void WaveEndCap_TighterThanLevelBounds_Wins()
    {
        float result = TargetX(1500, 800, waveEndX: 1000f);
        Assert.That(result, Is.EqualTo(600f));
    }

    [Test]
    public void OneWayScroll_PlayerWalksLeftPastDeadZone_CameraStays()
    {
        float currentCenter = 800;
        float result = TargetX(100, currentCenter);
        Assert.That(result, Is.EqualTo(currentCenter));
    }

    [Test]
    public void OneWayScroll_PlayerWalksRightPastDeadZone_CameraClampsToRightBoundary()
    {
        float currentCenter = 800;
        float result = TargetX(1500, currentCenter);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void OneWayScroll_PlayerAtLeftBoundary_WalksRight_CameraFollowsRight()
    {
        float result = TargetX(800, 400);
        Assert.That(result, Is.EqualTo(600f));
    }

    [Test]
    public void MovementBounds_CameraLeftLeftOfBounds_ReturnsBoundsUnchanged()
    {
        var baseBounds = new RectangleF(400, 0, 2000, 600);
        var result = CameraController.ComputeMovementBounds(0, baseBounds, null);
        Assert.That(result.X, Is.EqualTo(400));
        Assert.That(result.Width, Is.EqualTo(2000));
    }

    [Test]
    public void MovementBounds_CameraLeftRightOfBoundsLeft_ReturnsShrunkBounds()
    {
        var baseBounds = new RectangleF(200, 0, 1200, 600);
        var result = CameraController.ComputeMovementBounds(600, baseBounds, null);
        Assert.That(result.X, Is.EqualTo(600));
        Assert.That(result.Width, Is.EqualTo(800));
    }

    [Test]
    public void MovementBounds_CameraLeftEqualsBoundsLeft_ReturnsBoundsUnchanged()
    {
        var baseBounds = new RectangleF(200, 0, 1200, 600);
        var result = CameraController.ComputeMovementBounds(200, baseBounds, null);
        Assert.That(result.X, Is.EqualTo(200));
        Assert.That(result.Width, Is.EqualTo(1200));
    }

    [Test]
    public void MovementBounds_RightCapTighterThanBounds_ShrinksRightSide()
    {
        var baseBounds = new RectangleF(0, 0, 2000, 600);
        var result = CameraController.ComputeMovementBounds(0, baseBounds, 1200f);
        Assert.That(result.X, Is.EqualTo(0));
        Assert.That(result.Width, Is.EqualTo(1200));
    }

    [Test]
    public void MovementBounds_RightCapLooserThanBounds_UsesBaseBoundsRight()
    {
        var baseBounds = new RectangleF(0, 0, 1000, 600);
        var result = CameraController.ComputeMovementBounds(0, baseBounds, 2000f);
        Assert.That(result.X, Is.EqualTo(0));
        Assert.That(result.Width, Is.EqualTo(1000));
    }
}