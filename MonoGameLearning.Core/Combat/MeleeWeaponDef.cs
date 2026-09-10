using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

public class MeleeWeaponDef
{
    /// <summary>
    /// The weapon overlay is drawn directly from atlas regions, never from an
    /// <c>AnimatedSprite</c>: the hold pose is separate art (Aseprite <c>Hold</c>
    /// tag → <see cref="CarryRegion"/>, e.g. <c>bat-hold-00</c>) and cannot be reached
    /// through the swing run, and per-equip sprites forced the <c>SetAnimation</c>/
    /// <c>SetFrame</c> + <c>TextureRegion</c> pitfall for no animation benefit.
    /// Keeping two string names (carry + swing prefix) is the minimal cost of the
    /// region-keyed design (see AGENTS.md "MonoGame.Extended Pitfalls").
    /// </summary>
    public required string Name { get; init; }
    public required MoveData SwingMove { get; init; }
    public string? SwingPrefix { get; init; }
    public string? CarryRegion { get; init; }
    public Vector2 CarryAnchor { get; init; } = new Vector2(20, 0);
    public Vector2[] SwingAnchors { get; init; } = [];

    // Weapon render scale relative to the actor's SpriteRenderer.Scale. The anchors
    // must fold it in (anchor = hand - (handleOffset - FrameCenter) * Scale) so the
    // grip stays on the hand; the overlay draws the region at actorScale * Scale.
    public float Scale { get; init; } = 1f;

    // Frame center the handle offsets are expressed against. The grip-on-hand invariant
    // (anchor formula + ComputeHandleScreenPoint) cancels only when the drawn region's
    // half-size equals this; weapons whose regions differ must set it to their
    // region size / 2. RenderWeaponOverlay asserts the match in Debug.
    public Vector2 FrameCenter { get; init; } = new(32, 32);

    // Atlas-side grip point in frame-local pixels (Aseprite "handle" slice pivot,
    // bounds.origin + pivot). Doubles as the debug-draw handle marker and as the
    // bat-side half of the anchor formula.
    public Vector2 CarryHandleOffset { get; init; }
    public Vector2[] SwingHandleOffsets { get; init; } = [];

    public Texture2D? Texture { get; set; }

    private SpriteSheet? _sheet;
    private Texture2DRegion?[]? _swingRegions;
    private Texture2DRegion? _carryRegion;

    public SpriteSheet? Sheet
    {
        get => _sheet;
        set
        {
            _sheet = value;
            _swingRegions = null;
            _carryRegion = null;
        }
    }

    /// <summary>
    /// The resolved swing frame index: clamped to the swing-anchor run, or 0 when the
    /// weapon has no swing anchors (carry anchor is used). Single owner of the clamp so
    /// anchor, region, and handle resolvers cannot drift apart.
    /// </summary>
    internal static int ResolveSwingFrame(MeleeWeaponDef weapon, int frameIndex) =>
        weapon.SwingAnchors.Length > 0
            ? Math.Clamp(frameIndex, 0, weapon.SwingAnchors.Length - 1)
            : 0;

    internal static (Vector2 anchor, int frame) ResolveWeaponAnchorAndFrame(
        MeleeWeaponDef weapon, bool isAttacking, int frameIndex)
    {
        if (isAttacking && weapon.SwingAnchors.Length > 0)
            return (weapon.SwingAnchors[ResolveSwingFrame(weapon, frameIndex)], ResolveSwingFrame(weapon, frameIndex));
        return (weapon.CarryAnchor, 0);
    }

    internal static Vector2 ApplyWeaponFacing(Vector2 anchor, FacingDirection direction) =>
        direction == FacingDirection.Left ? new Vector2(-anchor.X, anchor.Y) : anchor;

    /// <summary>
    /// The face-following position of the weapon's handle grip in world space, matching
    /// the actual <see cref="CombatActorBase"/> overlay draw (anchor positioned by actor
    /// <paramref name="scale"/>, the region scaled by <c>scale * weaponScale</c> about its
    /// center). The grip lies <c>handleOffset - regionHalf</c> from the region center.
    /// <c>DrawDebug</c> flags when it falls outside the actor's sprite.
    /// </summary>
    internal static Vector2 ComputeHandleScreenPoint(
        Vector2 position, FacingDirection direction, Vector2 anchor, Vector2 handleOffset, Vector2 regionHalf, float scale, float weaponScale)
    {
        var anchorScreen = position + ApplyWeaponFacing(anchor, direction) * scale;
        return anchorScreen + ApplyWeaponFacing(handleOffset - regionHalf, direction) * scale * weaponScale;
    }

    /// <summary>
    /// True when the weapon carries Aseprite "handle" slice data, so debug draws may
    /// show the grip point (the anchor overlay already draws the center; the handle
    /// marker is the bat-side half of the anchor formula).
    /// </summary>
    public bool HasHandleOffsets => CarryHandleOffset != Vector2.Zero || SwingHandleOffsets.Length > 0;

    /// <summary>
    /// The current frame's handle point (frame-local, from the Aseprite slice). Uses the
    /// same resolved swing frame as the anchors; falls back to the carry handle when the
    /// weapon has no per-frame swing handles.
    /// </summary>
    internal Vector2 ResolveHandleOffset(bool isAttacking, int frameIndex)
    {
        if (!isAttacking || SwingHandleOffsets.Length == 0) return CarryHandleOffset;
        int frame = Math.Min(ResolveSwingFrame(this, frameIndex), SwingHandleOffsets.Length - 1);
        return SwingHandleOffsets[frame];
    }

    /// <summary>
    /// Resolves the atlas region name for the weapon overlay: per-frame swing regions
    /// (<c>{SwingPrefix}-{frame:00}</c>) while attacking, otherwise the single carried
    /// pose region (<see cref="CarryRegion"/>).
    /// </summary>
    internal static string? ResolveWeaponRegionName(MeleeWeaponDef weapon, bool isAttacking, int frameIndex)
    {
        if (isAttacking)
        {
            if (weapon.SwingPrefix is null) return null;
            return $"{weapon.SwingPrefix}-{ResolveSwingFrame(weapon, frameIndex):00}";
        }
        return weapon.CarryRegion;
    }

    /// <summary>
    /// Resolves the atlas region for the weapon overlay, caching the resolved
    /// <see cref="Texture2DRegion"/> per frame on first use so the Draw path stays
    /// allocation-free (no per-frame region-name string or dictionary lookup).
    /// Returns <c>null</c> when the sheet or the region mapping is missing.
    /// </summary>
    internal Texture2DRegion? ResolveWeaponRegion(bool isAttacking, int frameIndex)
    {
        if (Sheet is null) return null;

        if (isAttacking)
        {
            if (SwingPrefix is null) return null;
            int frame = ResolveSwingFrame(this, frameIndex);
            _swingRegions ??= new Texture2DRegion?[Math.Max(1, SwingAnchors.Length)];
            return _swingRegions[frame] ??= Sheet.TextureAtlas[$"{SwingPrefix}-{frame:00}"];
        }

        if (CarryRegion is null) return null;
        return _carryRegion ??= Sheet.TextureAtlas[CarryRegion];
    }

    internal static SpriteEffects WeaponFacingEffect(FacingDirection direction) =>
        direction == FacingDirection.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
}