using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Core.Combat;

/// <summary>
/// A held melee weapon: the carry pose plus a swing run keyed to the actor's attack
/// animation. The weapon overlay is drawn directly from atlas regions, never from an
/// <c>AnimatedSprite</c>: the hold pose is separate art (Aseprite <c>Hold</c> tag →
/// <see cref="WeaponDef.CarryRegion"/>) and cannot be reached through the swing run,
/// and per-equip sprites forced the <c>SetAnimation</c>/<c>SetFrame</c> +
/// <c>TextureRegion</c> pitfall for no animation benefit. Keeping two string names
/// (carry + swing prefix) is the minimal cost of the region-keyed design (see
/// AGENTS.md "MonoGame.Extended Pitfalls").
/// </summary>
public class MeleeWeaponDef : WeaponDef
{
    public required MoveData SwingMove { get; init; }
    public string? SwingPrefix { get; init; }

    public Vector2[] SwingAnchors { get; init; } = [];
    public Vector2[] SwingHandleOffsets { get; init; } = [];

    private Texture2DRegion?[]? _swingRegions;

    protected override void ResetRegionCache()
    {
        base.ResetRegionCache();
        _swingRegions = null;
    }

    public override bool HasHandleOffsets => base.HasHandleOffsets || SwingHandleOffsets.Length > 0;

    /// <summary>
    /// The resolved swing frame index: clamped to the swing-anchor run, or 0 when the
    /// weapon has no swing anchors (carry anchor is used). Single owner of the clamp so
    /// anchor, region, and handle resolvers cannot drift apart.
    /// </summary>
    internal static int ResolveSwingFrame(MeleeWeaponDef weapon, int actorFrameIndex) =>
        weapon.SwingAnchors.Length > 0
            ? Math.Clamp(actorFrameIndex, 0, weapon.SwingAnchors.Length - 1)
            : 0;

    public override (Vector2 anchor, int frame) ResolveAnchorAndFrame(bool isAttacking, int actorFrameIndex)
    {
        if (isAttacking && SwingAnchors.Length > 0)
        {
            int frame = ResolveSwingFrame(this, actorFrameIndex);
            return (SwingAnchors[frame], frame);
        }
        return (CarryAnchor, 0);
    }

    /// <summary>
    /// The current frame's handle point (frame-local, from the Aseprite slice). Uses the
    /// same resolved swing frame as the anchors; falls back to the carry handle when the
    /// weapon has no per-frame swing handles.
    /// </summary>
    public override Vector2 ResolveHandleOffset(bool isAttacking, int actorFrameIndex)
    {
        if (!isAttacking || SwingHandleOffsets.Length == 0) return CarryHandleOffset;
        int frame = Math.Min(ResolveSwingFrame(this, actorFrameIndex), SwingHandleOffsets.Length - 1);
        return SwingHandleOffsets[frame];
    }

    /// <summary>
    /// Resolves the atlas region for the weapon overlay: per-frame swing regions
    /// (<c>{SwingPrefix}-{frame:00}</c>) selected by the actor's animation frame while
    /// attacking, otherwise the single carried pose region. Caches the resolved region per
    /// frame for the allocation-free Draw path. The actor's frame index is the ONLY clock:
    /// there is no per-weapon animation timer.
    /// </summary>
    public override Texture2DRegion? ResolveRegion(bool isAttacking, int actorFrameIndex)
    {
        if (!isAttacking) return ResolveCarryRegion();
        if (Sheet is null || SwingPrefix is null) return null;

        int frame = ResolveSwingFrame(this, actorFrameIndex);
        _swingRegions ??= new Texture2DRegion?[Math.Max(1, SwingAnchors.Length)];
        return _swingRegions[frame] ??= Sheet.TextureAtlas[ResolveWeaponRegionName(this, isAttacking: true, actorFrameIndex)!];
    }

    /// <summary>
    /// Resolves the atlas region name for the weapon overlay: per-frame swing regions
    /// (<c>{SwingPrefix}-{frame:00}</c>) selected by the actor's animation frame while
    /// attacking, otherwise the single carried pose region (<see cref="WeaponDef.CarryRegion"/>).
    /// </summary>
    internal static string? ResolveWeaponRegionName(MeleeWeaponDef weapon, bool isAttacking, int actorFrameIndex)
    {
        if (isAttacking)
        {
            if (weapon.SwingPrefix is null) return null;
            return $"{weapon.SwingPrefix}-{ResolveSwingFrame(weapon, actorFrameIndex):00}";
        }
        return weapon.CarryRegion;
    }
}
