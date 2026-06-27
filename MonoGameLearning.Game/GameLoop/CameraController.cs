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

    private const float SMOOTH_FACTOR = 0.04f;

    public Vector2? LockedCenter { get; set; }

    public void Update(OrthographicCamera camera)
    {
        float targetX;
        float targetY = _gameHeight / 2f;

        if (LockedCenter.HasValue)
        {
            targetX = LockedCenter.Value.X;
        }
        else
        {
            float minX = _levelBounds.Left + (_gameWidth / 2f);
            float maxX = _levelBounds.Right - (_gameWidth / 2f);
            Debug.Assert(minX <= maxX, $"Level width ({_levelBounds.Width}) is smaller than viewport width ({_gameWidth}).");
            targetX = Math.Clamp(_player.Position.X, minX, maxX);
        }

        float halfWidth = _gameWidth / 2f;
        float desiredPos = targetX - halfWidth;
        float newPos = MathHelper.Lerp(camera.Position.X, desiredPos, SMOOTH_FACTOR);
        camera.LookAt(new Vector2(newPos + halfWidth, targetY));
    }
}