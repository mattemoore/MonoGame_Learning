using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Movement;

namespace MonoGameLearning.Core.Combat;

public class HitboxService
{
    private readonly List<ActiveHitbox> _activeHitboxes = [];
    private readonly Dictionary<IHitboxProvider, HashSet<IDamageable>> _attackDedup = [];
    private readonly List<DamageInfo> _resultBuffer = [];
    private readonly List<RectangleF> _boundsBuffer = [];

    public void RegisterFrameHitboxes(Entity owner, Faction ownerFaction, MoveData move, int frameIndex, FacingDirection facing)
    {
        if (!move.FrameHitboxes.TryGetValue(frameIndex, out var hitboxDefs))
            return;

        Debug.Assert(owner is IHitboxProvider,
            $"{owner.GetType().Name} \"{owner.Name}\" registered hitboxes but is not an IHitboxProvider");
        var provider = (IHitboxProvider)owner;

        foreach (var hb in hitboxDefs)
            RegisterHitbox(provider, owner.Position, ownerFaction, hb, move.Damage, move.Knockdown, move.Strength, move.ImpactSfx, facing);
    }

    /// <summary>
    /// Registers a single hitbox at an explicit center. Frame hitboxes and pooled
    /// projectiles both funnel through here so hitbox shape/damage handling has one owner.
    /// </summary>
    public void RegisterHitbox(
        IHitboxProvider owner,
        Vector2 center,
        Faction ownerFaction,
        HitboxData hitbox,
        int damage,
        bool knockdown,
        AttackStrength strength,
        SfxId? impactSfx,
        FacingDirection facing)
    {
        _activeHitboxes.Add(new()
        {
            Owner = owner,
            OwnerFaction = ownerFaction,
            Bounds = hitbox.CreateRectangle(center, facing),
            Damage = damage,
            Knockdown = knockdown,
            Strength = strength,
            ImpactSfx = impactSfx,
        });
    }

    public List<DamageInfo> ResolveHits(IReadOnlyList<Entity> targets)
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

                if (tgt is CombatActorBase { Faction: var targetFaction } && active.OwnerFaction == targetFaction) continue;

                _resultBuffer.Add(new()
                {
                    Target = tgt,
                    Amount = active.Damage,
                    Knockdown = active.Knockdown,
                    Strength = active.Strength,
                    ImpactSfx = active.ImpactSfx,
                    Source = active.Owner,
                });
            }
        }

        return _resultBuffer;
    }

    public void Clear(IHitboxProvider owner)
    {
        Debug.Assert(owner is not null, "Clear called with null owner");
        // Index loop, not RemoveAll(lambda): this runs per projectile per frame, and a
        // capturing predicate would allocate a closure + delegate on the gameplay hot path.
        for (int i = _activeHitboxes.Count - 1; i >= 0; i--)
        {
            if (_activeHitboxes[i].Owner == owner)
                _activeHitboxes.RemoveAt(i);
        }
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