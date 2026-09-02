using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests;

static class RespawnTestHelper
{
    public static Vector2 ComputeRespawnPosition(float cameraX, RectangleF movementBounds, float walkableTopY)
    {
        return GameLoopRules.ComputeRespawnPosition(cameraX, movementBounds, walkableTopY);
    }
}