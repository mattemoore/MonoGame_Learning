using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace MonoGameLearning.Game.Tests;

static class RespawnTestHelper
{
    public static Vector2 ComputeRespawnPosition(float cameraX, RectangleF movementBounds, float walkableTopY)
    {
        return global::MonoGameLearning.Game.GameLoop.GameLoop.ComputeRespawnPosition(cameraX, movementBounds, walkableTopY);
    }
}