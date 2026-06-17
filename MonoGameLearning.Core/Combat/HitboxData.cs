using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Combat;

public readonly record struct HitboxData
{
    public Vector2 Offset { get; init; }
    public Point Size { get; init; }

    public RectangleF CreateRectangle(Vector2 center, FacingDirection facing)
    {
        Debug.Assert(Size.X > 0 && Size.Y > 0, "Hitbox size must be positive");

        var offset = facing == FacingDirection.Left
            // Offset values assume FacingDirection.Right. Negate X when facing
            // left so the hitbox projects in front of the attacker.
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