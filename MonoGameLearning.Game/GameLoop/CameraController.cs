using System;
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

    public float? LeftBound { get; set; }
    public float? RightBound { get; set; }

    public void Update(OrthographicCamera camera)
    {
        float minX = _levelBounds.Left + (_gameWidth / 2f);
        float maxX = _levelBounds.Right - (_gameWidth / 2f);
        if (LeftBound.HasValue) minX = Math.Max(minX, LeftBound.Value);
        if (RightBound.HasValue) maxX = Math.Min(maxX, RightBound.Value);

        if (LeftBound.HasValue && RightBound.HasValue && LeftBound.Value == RightBound.Value)
        {
            float center = LeftBound.Value;
            float minAllowed = _levelBounds.Left + (_gameWidth / 2f);
            float maxAllowed = _levelBounds.Right - (_gameWidth / 2f);
            minX = Math.Clamp(center, minAllowed, maxAllowed);
            maxX = minX;
        }
        else if (minX > maxX)
        {
            float mid = (minX + maxX) / 2f;
            minX = mid;
            maxX = mid;
        }

        float desiredX = Math.Clamp(_player.Position.X, minX, maxX);

        float halfWidth = _gameWidth / 2f;
        float targetPos = desiredX - halfWidth;
        float newPos = MathHelper.Lerp(camera.Position.X, targetPos, SMOOTH_FACTOR);
        camera.LookAt(new Vector2(newPos + halfWidth, _gameHeight / 2f));
    }
}