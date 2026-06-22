using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Game.Entities.Player;

namespace MonoGameLearning.Game.Camera;

public class CameraController
{
    private readonly PlayerEntity _player;
    private readonly int _gameWidth;
    private readonly int _gameHeight;
    private readonly int _totalLevelWidth;

    public CameraController(PlayerEntity player, int gameWidth, int gameHeight, int totalLevelWidth)
    {
        _player = player;
        _gameWidth = gameWidth;
        _gameHeight = gameHeight;
        _totalLevelWidth = totalLevelWidth;
    }

    public void Update(OrthographicCamera camera)
    {
        float minX = _gameWidth / 2f;
        float maxX = _totalLevelWidth - (_gameWidth / 2f);
        float clampedX = Math.Clamp(_player.Position.X, minX, maxX);
        camera.LookAt(new Vector2(clampedX, _gameHeight / 2f));
    }
}