using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

/// <summary>
/// Shared base for held weapons. Owns the data every weapon renders with: the carried
/// hold pose, the atlas-side grip point, and the render scale. The actor owns where its
/// hand is per animation frame (<c>CombatActorBase.ResolveHandAnchor</c>); a weapon
/// combines the two through <see cref="ComputeAnchor"/>. The overlay is region-keyed
/// (never frame-stepped), so subclasses resolve the atlas region/grip per actor frame;
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
    /// Resolves the atlas region for the weapon overlay, caching the resolved
    /// <see cref="Texture2DRegion"/> on first use so the Draw path stays allocation-free.
    /// Returns <c>null</c> when the sheet or the region mapping is missing.
    /// <paramref name="actorFrameIndex"/> is the actor's animation frame
    /// (<c>SpriteRenderer.AnimationFrame</c>).
    /// </summary>
    public virtual Texture2DRegion? ResolveRegion(bool isAttacking, int actorFrameIndex) => ResolveCarryRegion();

    /// <summary>
    /// The current frame's handle point (frame-local, from the Aseprite slice), selected by
    /// the actor's animation frame for melee swing runs and ignored by throwables.
    /// </summary>
    public virtual Vector2 ResolveHandleOffset(bool isAttacking, int actorFrameIndex) => CarryHandleOffset;

    /// <summary>
    /// The overlay anchor for a weapon held at <paramref name="hand"/>: positions the drawn
    /// region so the weapon's grip point (<paramref name="handleOffset"/>, from the Aseprite
    /// handle slice) lands on the actor's hand. The weapon scale cancels out of the
    /// grip-on-hand invariant, so the grip stays on the hand for any <paramref name="scale"/>.
    /// </summary>
    internal static Vector2 ComputeAnchor(Vector2 hand, Vector2 handleOffset, Vector2 frameCenter, float scale) =>
        hand - (handleOffset - frameCenter) * scale;

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
