using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Camera;

public class CameraController(Entity player, int gameWidth, int gameHeight, RectangleF levelBounds)
{
    private bool _waveClearedPending;
    private float? _holdCameraCenter;
    private float _holdPlayerX;

    public const float DEAD_ZONE_FRACTION = 0.25f;
    public const float CATCH_UP_RATE = 0.25f;

    public float? WaveEndX { get; set; }

    public void OnWaveCleared()
    {
        _waveClearedPending = true;
    }

    public static float ComputeTargetX(
        float playerX,
        float currentCameraCenterX,
        float minCenter,
        float maxCenter,
        int width,
        float deadZoneFraction = DEAD_ZONE_FRACTION)
    {
        Debug.Assert(minCenter <= maxCenter, $"Camera clamp range is empty (min={minCenter}, max={maxCenter}).");
        Debug.Assert(deadZoneFraction is >= 0f and <= 0.5f,
            $"deadZoneFraction must be in [0, 0.5]; got {deadZoneFraction}.");

        float halfWidth = width / 2f;
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
        float halfWidth = gameWidth / 2f;
        float currentCenterX = camera.Position.X + halfWidth;
        float fullMinCenter = levelBounds.Left + halfWidth;
        float fullMaxCenter = levelBounds.Right - halfWidth;

        float maxCenter = fullMaxCenter;
        if (WaveEndX.HasValue)
            maxCenter = Math.Min(fullMaxCenter, WaveEndX.Value - halfWidth);

        float deadZoneEdge = halfWidth * (1f - 2f * DEAD_ZONE_FRACTION);

        if (_waveClearedPending)
        {
            _waveClearedPending = false;
            if (player.Position.X - currentCenterX > deadZoneEdge)
            {
                _holdCameraCenter = currentCenterX;
                _holdPlayerX = player.Position.X;
            }
            else
            {
                _holdCameraCenter = null;
            }
        }

        float targetCenterX = ComputeTargetX(player.Position.X, currentCenterX, fullMinCenter, maxCenter, gameWidth);

        if (_holdCameraCenter.HasValue)
        {
            float rightwardMove = Math.Max(0, player.Position.X - _holdPlayerX);
            float softTarget = _holdCameraCenter.Value + rightwardMove * (1f + CATCH_UP_RATE);

            if (softTarget >= player.Position.X - deadZoneEdge)
                _holdCameraCenter = null;
            else
                targetCenterX = Math.Min(targetCenterX, softTarget);
        }

        camera.LookAt(new Vector2(targetCenterX, gameHeight / 2f));
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