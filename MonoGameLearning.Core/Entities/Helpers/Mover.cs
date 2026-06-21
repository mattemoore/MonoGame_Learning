using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Entities.Helpers;

public static class Mover
{
    public static void ClampToBounds(Entity entity, RectangleF movementBounds)
    {
        if (movementBounds.IsEmpty) return;

        float halfWidth = entity.Width / 2f;
        float halfHeight = entity.Height / 2f;

        entity.Position = new Vector2(
            MathHelper.Clamp(entity.Position.X, movementBounds.Left + halfWidth, movementBounds.Right - halfWidth),
            MathHelper.Clamp(entity.Position.Y, movementBounds.Top + halfHeight, movementBounds.Bottom - halfHeight)
        );
    }

    public static FacingDirection UpdateFacingDirection(AnimatedSprite sprite, Vector2 direction, FacingDirection currentFacing)
    {
        if (direction.X < 0 && currentFacing != FacingDirection.Left)
        {
            sprite.Effect = SpriteEffects.FlipHorizontally;
            return FacingDirection.Left;
        }
        else if (direction.X > 0 && currentFacing != FacingDirection.Right)
        {
            sprite.Effect = SpriteEffects.None;
            return FacingDirection.Right;
        }
        return currentFacing;
    }

    public static Vector2 PreventDiagonal(Vector2 direction) =>
        Math.Abs(direction.X) > Math.Abs(direction.Y)
            ? new Vector2(direction.X, 0)
            : new Vector2(0, direction.Y);
}