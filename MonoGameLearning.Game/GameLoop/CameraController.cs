using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Game.Entities.Player;

namespace MonoGameLearning.Game.GameLoop;

public class CameraController(PlayerEntity player, int gameWidth, int gameHeight, RectangleF levelBounds)
{
    private readonly PlayerEntity _player = player;
    private readonly int _gameWidth = gameWidth;
    private readonly int _gameHeight = gameHeight;
    private readonly RectangleF _levelBounds = levelBounds;

    public const float DEAD_ZONE_FRACTION = 0.25f;

    public float? WaveEndX { get; set; }

    public static float ComputeTargetX(
        float playerX,
        float currentCameraCenterX,
        RectangleF levelBounds,
        int gameWidth,
        float? waveEndX = null,
        float deadZoneFraction = DEAD_ZONE_FRACTION)
    {
        float halfWidth = gameWidth / 2f;

        float minCenter = levelBounds.Left + halfWidth;
        float maxCenter = levelBounds.Right - halfWidth;
        if (waveEndX.HasValue)
            maxCenter = Math.Min(maxCenter, waveEndX.Value - halfWidth);
        Debug.Assert(minCenter <= maxCenter, $"Camera clamp range is empty (min={minCenter}, max={maxCenter}).");

        Debug.Assert(deadZoneFraction is >= 0f and <= 0.5f,
            $"deadZoneFraction must be in [0, 0.5]; got {deadZoneFraction}.");
        float deadZoneEdge = halfWidth * (1f - 2f * deadZoneFraction);
        float targetCenter = currentCameraCenterX;

        if (playerX < currentCameraCenterX - deadZoneEdge)
            targetCenter = playerX + deadZoneEdge;
        else if (playerX > currentCameraCenterX + deadZoneEdge)
            targetCenter = playerX - deadZoneEdge;

        targetCenter = Math.Max(targetCenter, currentCameraCenterX);
        return Math.Clamp(targetCenter, minCenter, maxCenter);
    }

    public void Update(OrthographicCamera camera)
    {
        float currentCenterX = camera.Position.X + _gameWidth / 2f;
        float targetCenterX = ComputeTargetX(_player.Position.X, currentCenterX, _levelBounds, _gameWidth, WaveEndX);
        camera.LookAt(new Vector2(targetCenterX, _gameHeight / 2f));
    }

    public static RectangleF ComputeMovementBounds(float cameraLeftEdge, RectangleF baseBounds, float? rightCap)
    {
        float effectiveLeft = Math.Max(cameraLeftEdge, baseBounds.X);
        float effectiveRight = rightCap.HasValue ? Math.Min(rightCap.Value, baseBounds.Right) : baseBounds.Right;
        return new RectangleF(
            effectiveLeft,
            baseBounds.Y,
            effectiveRight - effectiveLeft,
            baseBounds.Height);
    }
}