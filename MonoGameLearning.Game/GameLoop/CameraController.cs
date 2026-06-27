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
        int gameWidth,
        float deadZoneFraction = DEAD_ZONE_FRACTION)
    {
        Debug.Assert(minCenter <= maxCenter, $"Camera clamp range is empty (min={minCenter}, max={maxCenter}).");
        Debug.Assert(deadZoneFraction is >= 0f and <= 0.5f,
            $"deadZoneFraction must be in [0, 0.5]; got {deadZoneFraction}.");

        float halfWidth = gameWidth / 2f;
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
        float halfWidth = _gameWidth / 2f;
        float currentCenterX = camera.Position.X + halfWidth;
        float fullMinCenter = _levelBounds.Left + halfWidth;
        float fullMaxCenter = _levelBounds.Right - halfWidth;

        float maxCenter = fullMaxCenter;
        if (WaveEndX.HasValue)
            maxCenter = Math.Min(fullMaxCenter, WaveEndX.Value - halfWidth);

        float deadZoneEdge = halfWidth * (1f - 2f * DEAD_ZONE_FRACTION);

        if (_waveClearedPending)
        {
            _waveClearedPending = false;
            if (_player.Position.X - currentCenterX > deadZoneEdge)
            {
                _holdCameraCenter = currentCenterX;
                _holdPlayerX = _player.Position.X;
            }
            else
            {
                _holdCameraCenter = null;
            }
        }

        float targetCenterX = ComputeTargetX(_player.Position.X, currentCenterX, fullMinCenter, maxCenter, _gameWidth);

        // Gradually close the gap as the player moves right, so the camera
        // only advances in response to player-initiated rightward movement.
        if (_holdCameraCenter.HasValue)
        {
            float rightwardMove = Math.Max(0, _player.Position.X - _holdPlayerX);
            float softTarget = _holdCameraCenter.Value + rightwardMove * (1f + CATCH_UP_RATE);

            if (softTarget >= _player.Position.X - deadZoneEdge)
                _holdCameraCenter = null;
            else
                targetCenterX = Math.Min(targetCenterX, softTarget);
        }

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
