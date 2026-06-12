using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Combat;

public record struct HitResult
{
    public SpatialEntity Target { get; init; }
    public int Damage { get; init; }
    public Vector2 Knockback { get; init; }
    public SpatialEntity Source { get; init; }
}

public class HitboxService
{
    private record ActiveHitbox
    {
        public SpatialEntity Owner { get; init; }
        public RectangleF Bounds { get; init; }
        public int Damage { get; init; }
        public Vector2 Knockback { get; init; }
        public HitboxData Definition { get; init; }
    }

    private readonly List<ActiveHitbox> _activeHitboxes = [];
    // Tracks which (hitbox definition, target) pairs have already dealt damage
    // during the current animation frame, scoped per owner. Persists across
    // ResolveHits() calls so that a single animation frame's hitboxes don't hit
    // the same target repeatedly on consecutive game ticks. Cleared by
    // Clear(owner) when the animation frame advances or the attack ends.
    private readonly Dictionary<SpatialEntity, HashSet<(HitboxData, SpatialEntity)>> _resolvedThisFrame = [];

    public void RegisterFrameHitboxes(SpatialEntity owner, MoveData move, int frameIndex, FacingDirection facing)
    {
        if (!move.FrameHitboxes.TryGetValue(frameIndex, out var hitboxDefs))
            return;

        foreach (var hb in hitboxDefs)
        {
            _activeHitboxes.Add(new()
            {
                Owner = owner,
                Bounds = hb.CreateRectangle(owner.Position, facing),
                Damage = move.Damage,
                Knockback = facing == FacingDirection.Left
                    // Knockback values in move definitions assume FacingDirection.Right.
                    // Negate X when facing left so knockback pushes away from the attacker.
                    ? move.Knockback with { X = -move.Knockback.X }
                    : move.Knockback,
                Definition = hb
            });
        }
    }

    public List<HitResult> ResolveHits(IEnumerable<SpatialEntity> targets)
    {
        var results = new List<HitResult>();

        foreach (var active in _activeHitboxes)
        {
            if (!_resolvedThisFrame.TryGetValue(active.Owner, out var ownerResolved))
            {
                ownerResolved = [];
                _resolvedThisFrame[active.Owner] = ownerResolved;
            }

            foreach (var target in targets)
            {
                if (target == active.Owner) continue;
                if (!active.Bounds.Intersects(target.Frame)) continue;
                if (!ownerResolved.Add((active.Definition, target))) continue;

                results.Add(new()
                {
                    Target = target,
                    Damage = active.Damage,
                    Knockback = active.Knockback,
                    Source = active.Owner
                });
            }
        }

        return results;
    }

    // Owner-scoped: only removes hitboxes and resolved-frame tracking belonging
    // to the given entity. Other entities' tracking is unaffected.
    public void Clear(SpatialEntity owner)
    {
        _activeHitboxes.RemoveAll(hb => hb.Owner == owner);
        _resolvedThisFrame.Remove(owner);
    }

    public void ClearAll()
    {
        _activeHitboxes.Clear();
        _resolvedThisFrame.Clear();
    }

    public IReadOnlyList<RectangleF> GetActiveHitboxBounds(SpatialEntity owner)
    {
        var bounds = new List<RectangleF>();
        foreach (var hb in _activeHitboxes)
        {
            if (hb.Owner == owner)
                bounds.Add(hb.Bounds);
        }
        return bounds;
    }
}