using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class CameraTargetXTests
{
    private const int GameWidth = 800;
    private const float HalfWidth = 400f;
    private const float MinCenter = 400f; // LevelBounds.Left + HalfWidth
    private const float FullMaxCenter = 1200f; // LevelBounds.Right - HalfWidth
    private static readonly RectangleF LevelBounds = new(0, 0, 1600, 900);

    private static float TargetX(float playerX, float currentCameraCenterX, float? maxCenter = null)
        => CameraController.ComputeTargetX(playerX, currentCameraCenterX, MinCenter, maxCenter ?? FullMaxCenter, GameWidth);

    private static float CappedMaxCenter(float? waveEndX)
    {
        if (!waveEndX.HasValue) return FullMaxCenter;
        return Math.Min(FullMaxCenter, waveEndX.Value - HalfWidth);
    }

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
        float minResult = CameraController.ComputeTargetX(0, 0, 400, 400, GameWidth);
        float maxResult = CameraController.ComputeTargetX(800, 800, 400, 400, GameWidth);
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
    public void WaveEndCap_CameraCenterCappedAtWaveEndX()
    {
        // maxCenter is capped at waveEndX - halfWidth: min(1200, 1200-400) = 800
        float result = TargetX(1500, 800, maxCenter: CappedMaxCenter(1200f));
        Assert.That(result, Is.EqualTo(800f));
    }

    [Test]
    public void WaveEndCap_PlayerPastEnd_ClampsCamera()
    {
        // maxCenter = min(1200, 1200-400) = 800
        float maxCenter = CappedMaxCenter(1200f);
        float result = TargetX(1300, 1000, maxCenter);
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
        // maxCenter = min(1200, 1000-400) = 600
        float result = TargetX(1500, 800, maxCenter: CappedMaxCenter(1000f));
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
