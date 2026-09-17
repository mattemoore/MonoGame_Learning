using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

/// <summary>
/// Shared base for held weapons. Owns the data every weapon renders with: the
/// carried hold pose and its actor-side anchor, the atlas-side grip point, and the
/// render scale. The overlay is region-keyed (never frame-stepped), so subclasses
/// resolve the atlas region/anchor/handle per frame through the virtual resolvers;
/// melee adds a swing run on top, throwables reuse the carry defaults verbatim.
/// </summary>
public abstract class WeaponDef
{
    public required string Name { get; init; }

    /// <summary>
    /// Weapon render scale relative to the actor's <c>SpriteRenderer.Scale</c>. The
    /// anchors must fold it in (anchor = hand - (handleOffset - FrameCenter) * Scale)
    /// so the grip stays on the hand; the overlay draws the region at actorScale * Scale.
    /// </summary>
    public float Scale { get; init; } = 1f;

    /// <summary>
    /// Frame center the handle offsets are expressed against. The grip-on-hand invariant
    /// (anchor formula + <see cref="ComputeHandleScreenPoint"/>) cancels only when the
    /// drawn region's half-size equals this; weapons whose regions differ must set it to
    /// their region size / 2. <c>RenderWeaponOverlay</c> asserts the match in Debug.
    /// </summary>
    public Vector2 FrameCenter { get; init; } = new(32, 32);

    public string? CarryRegion { get; init; }
    public Vector2 CarryAnchor { get; init; } = new Vector2(20, 0);

    /// <summary>
    /// Atlas-side grip point in frame-local pixels (Aseprite "handle" slice pivot,
    /// <c>bounds.origin + pivot</c>). Doubles as the debug-draw handle marker and as
    /// the weapon-side half of the anchor formula.
    /// </summary>
    public Vector2 CarryHandleOffset { get; init; }

    public Texture2D? Texture { get; set; }

    private SpriteSheet? _sheet;
    private Texture2DRegion? _carryRegion;

    public SpriteSheet? Sheet
    {
        get => _sheet;
        set
        {
            _sheet = value;
            ResetRegionCache();
        }
    }

    protected virtual void ResetRegionCache() => _carryRegion = null;

    protected Texture2DRegion? ResolveCarryRegion() =>
        Sheet is null || CarryRegion is null ? null : _carryRegion ??= Sheet.TextureAtlas[CarryRegion];

    /// <summary>
    /// True when the weapon carries Aseprite "handle" slice data, so debug draws may
    /// show the grip point (the anchor overlay already draws the center; the handle
    /// marker is the weapon-side half of the anchor formula).
    /// </summary>
    public virtual bool HasHandleOffsets => CarryHandleOffset != Vector2.Zero;

    /// <summary>
    /// Resolves the weapon's overlay pose. The overlay has NO clock of its own:
    /// <paramref name="isAttacking"/> and <paramref name="actorFrameIndex"/> are the actor's
    /// own state (<c>CombatActorBase.IsWeaponSwingActive</c> / <c>FrameTracker.FrameIndex</c>),
    /// so a melee weapon's per-frame swing regions track the arm in lockstep while a
    /// throwable just ignores them and returns the carry pose.
    /// (Contrast: <see cref="ThrowableWeaponDef.ResolveProjectileRegion"/> is driven by the
    /// projectile's own timer, not by the thrower's animation.)
    /// </summary>
    public virtual (Vector2 anchor, int frame) ResolveAnchorAndFrame(bool isAttacking, int actorFrameIndex) =>
        (CarryAnchor, 0);

    /// <summary>
    /// Resolves the atlas region for the weapon overlay, caching the resolved
    /// <see cref="Texture2DRegion"/> on first use so the Draw path stays allocation-free.
    /// Returns <c>null</c> when the sheet or the region mapping is missing.
    /// <paramref name="actorFrameIndex"/> is the actor's animation frame (see
    /// <see cref="ResolveAnchorAndFrame"/>).
    /// </summary>
    public virtual Texture2DRegion? ResolveRegion(bool isAttacking, int actorFrameIndex) => ResolveCarryRegion();

    /// <summary>
    /// The current frame's handle point (frame-local, from the Aseprite slice), selected by
    /// the actor's animation frame for melee swing runs and ignored by throwables.
    /// </summary>
    public virtual Vector2 ResolveHandleOffset(bool isAttacking, int actorFrameIndex) => CarryHandleOffset;

    public static Vector2 ApplyWeaponFacing(Vector2 anchor, FacingDirection direction) =>
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

    internal static SpriteEffects WeaponFacingEffect(FacingDirection direction) =>
        direction == FacingDirection.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
}
