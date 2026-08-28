using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

public class MeleeWeaponDef
{
    public required string Name { get; init; }
    public required MoveData SwingMove { get; init; }
    public string? SwingAnimation { get; init; }
    public Vector2 CarryAnchor { get; init; } = new Vector2(20, 0);
    public Vector2[] SwingAnchors { get; init; } = [];
    public Texture2D? Texture { get; set; }
    public SpriteSheet? Sheet { get; set; }

    public AnimatedSprite? CreateSprite()
    {
        // TODO: Assert SwingAnchors.Length <= Sheet frame count here (see TODO.md item 4) so an
        // oversized swing def fails loudly in Debug instead of throwing at draw-time SetFrame.
        if (Sheet is null || SwingAnimation is null) return null;
        var sprite = new AnimatedSprite(Sheet, SwingAnimation);
        sprite.Origin = new Vector2(sprite.Size.X / 2f, sprite.Size.Y / 2f);
        return sprite;
    }

    internal static (Vector2 anchor, int frame) ResolveWeaponAnchorAndFrame(
        MeleeWeaponDef weapon, bool isAttacking, int frameIndex)
    {
        if (isAttacking && weapon.SwingAnchors.Length > 0)
        {
            int frame = Math.Clamp(frameIndex, 0, weapon.SwingAnchors.Length - 1);
            return (weapon.SwingAnchors[frame], frame);
        }
        return (weapon.CarryAnchor, 0);
    }

    internal static Vector2 ApplyWeaponFacing(Vector2 anchor, FacingDirection direction) =>
        direction == FacingDirection.Left ? new Vector2(-anchor.X, anchor.Y) : anchor;

    internal static SpriteEffects WeaponFacingEffect(FacingDirection direction) =>
        direction == FacingDirection.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
}