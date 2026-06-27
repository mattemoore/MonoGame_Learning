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

    private const float SMOOTH_FACTOR = 0.01f;

    public Vector2? LockedCenter { get; set; }

    public static float ComputeTargetX(float playerX, float? lockedCenterX, RectangleF levelBounds, int gameWidth)
    {
        if (lockedCenterX.HasValue) return lockedCenterX.Value;
        float minX = levelBounds.Left + (gameWidth / 2f);
        float maxX = levelBounds.Right - (gameWidth / 2f);
        Debug.Assert(minX <= maxX, $"Level width ({levelBounds.Width}) is smaller than viewport width ({gameWidth}).");
        return Math.Clamp(playerX, minX, maxX);
    }

    public void Update(OrthographicCamera camera)
    {
        float targetX = ComputeTargetX(_player.Position.X, LockedCenter?.X, _levelBounds, _gameWidth);
        float targetY = _gameHeight / 2f;

        float halfWidth = _gameWidth / 2f;
        float desiredPos = targetX - halfWidth;
        float newPos = MathHelper.Lerp(camera.Position.X, desiredPos, SMOOTH_FACTOR);
        camera.LookAt(new Vector2(newPos + halfWidth, targetY));
    }
}