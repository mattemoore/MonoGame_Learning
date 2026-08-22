using System.Collections.Generic;
using System.Diagnostics;
using MonoGame.Extended;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

public class HitboxService
{
    private readonly record struct ActiveHitbox
    {
        public IHitboxProvider Owner { get; init; }
        public Faction OwnerFaction { get; init; }
        public RectangleF Bounds { get; init; }
        public int Damage { get; init; }
        public bool Knockdown { get; init; }
        public AttackStrength Strength { get; init; }
        public SfxId? ImpactSfx { get; init; }
    }

    private readonly List<ActiveHitbox> _activeHitboxes = [];
    private readonly Dictionary<IHitboxProvider, HashSet<IDamageable>> _attackDedup = [];
    private readonly List<HitResult> _resultBuffer = [];
    private readonly List<RectangleF> _boundsBuffer = [];

    public void RegisterFrameHitboxes(Entity owner, Faction ownerFaction, MoveData move, int frameIndex, FacingDirection facing)
    {
        if (!move.FrameHitboxes.TryGetValue(frameIndex, out var hitboxDefs))
            return;

        Debug.Assert(owner is IHitboxProvider,
            $"{owner.GetType().Name} \"{owner.Name}\" registered hitboxes but is not an IHitboxProvider");
        var provider = (IHitboxProvider)owner;

        foreach (var hb in hitboxDefs)
        {
            _activeHitboxes.Add(new()
            {
                Owner = provider,
                OwnerFaction = ownerFaction,
                Bounds = hb.CreateRectangle(owner.Position, facing),
                Damage = move.Damage,
                Knockdown = move.Knockdown,
                Strength = move.Strength,
                ImpactSfx = move.ImpactSfx,
            });
        }
    }

    public List<HitResult> ResolveHits(IReadOnlyList<Entity> targets)
    {
        _resultBuffer.Clear();

        foreach (var active in _activeHitboxes)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (ReferenceEquals(target, active.Owner)) continue;
                if (!active.Bounds.Intersects(target.Frame)) continue;
                if (target is not IDamageable tgt) continue;

                if (!_attackDedup.TryGetValue(active.Owner, out var ownerDedup))
                {
                    ownerDedup = [];
                    _attackDedup[active.Owner] = ownerDedup;
                }
                if (!ownerDedup.Add(tgt)) continue;

                if (active.OwnerFaction == tgt.Faction) continue;

                _resultBuffer.Add(new()
                {
                    Target = tgt,
                    Damage = active.Damage,
                    Knockdown = active.Knockdown,
                    Strength = active.Strength,
                    ImpactSfx = active.ImpactSfx,
                });
            }
        }

        return _resultBuffer;
    }

    public void Clear(IHitboxProvider owner)
    {
        Debug.Assert(owner is not null, "Clear called with null owner");
        _activeHitboxes.RemoveAll(hb => hb.Owner == owner);
    }

    public void ClearAttackDedup(IHitboxProvider owner)
    {
        Debug.Assert(owner is not null, "ClearAttackDedup called with null owner");
        _attackDedup.Remove(owner);
    }

    public void ClearAll()
    {
        _activeHitboxes.Clear();
        _attackDedup.Clear();
    }

    public IReadOnlyList<RectangleF> GetActiveHitboxBounds(IHitboxProvider owner)
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