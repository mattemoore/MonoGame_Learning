using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

public readonly record struct HitboxData
{
    public Vector2 Offset { get; init; }
    public Point Size { get; init; }

    public RectangleF CreateRectangle(Vector2 center, FacingDirection facing)
    {
        Debug.Assert(Size.X > 0 && Size.Y > 0, "Hitbox size must be positive");

        var offset = facing == FacingDirection.Left
            ? new Vector2(-Offset.X, Offset.Y)
            : Offset;

        return new(
            center.X + offset.X - (Size.X / 2f),
            center.Y + offset.Y - (Size.Y / 2f),
            Size.X,
            Size.Y
        );
    }
}
