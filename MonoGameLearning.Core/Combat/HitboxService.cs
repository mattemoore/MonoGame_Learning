using System.Collections.Generic;
using System.Diagnostics;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Combat;

public record struct HitResult
{
    public SpatialEntity Target { get; init; }
    public int Damage { get; init; }
    public SpatialEntity Source { get; init; }
    public bool Knockdown { get; init; }
}

public class HitboxService
{
    private record ActiveHitbox
    {
        public SpatialEntity Owner { get; init; }
        public RectangleF Bounds { get; init; }
        public int Damage { get; init; }
        public bool Knockdown { get; init; }
        public HitboxData Definition { get; init; }
    }

    private readonly List<ActiveHitbox> _activeHitboxes = [];
    private readonly Dictionary<SpatialEntity, HashSet<(HitboxData, SpatialEntity)>> _resolvedThisFrame = [];
    private readonly List<HitResult> _resultBuffer = [];
    private readonly List<RectangleF> _boundsBuffer = [];

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
                Knockdown = move.Knockdown,
                Definition = hb
            });
        }
    }

    public List<HitResult> ResolveHits(IEnumerable<SpatialEntity> targets)
    {
        _resultBuffer.Clear();

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

                _resultBuffer.Add(new()
                {
                    Target = target,
                    Damage = active.Damage,
                    Source = active.Owner,
                    Knockdown = active.Knockdown
                });
            }
        }

        return _resultBuffer;
    }

    public void Clear(SpatialEntity owner)
    {
        Debug.Assert(owner is not null, "Clear called with null owner");
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
        _boundsBuffer.Clear();
        foreach (var hb in _activeHitboxes)
        {
            if (hb.Owner == owner)
                _boundsBuffer.Add(hb.Bounds);
        }
        return _boundsBuffer;
    }
}