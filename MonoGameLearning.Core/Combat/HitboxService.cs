using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Combat;

public readonly record struct HitboxData
{
    public Vector2 Offset { get; init; }
    public Point Size { get; init; }

    public RectangleF CreateRectangle(Vector2 center, FacingDirection facing)
    {
        Debug.Assert(Size.X > 0 && Size.Y > 0, "Hitbox size must be positive");

        var offset = facing == FacingDirection.Left
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

public record struct HitResult
{
    public IDamageable Target { get; init; }
    public int Damage { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
    public SfxId? ImpactSfx { get; init; }
}

public class HitboxService
{
    private readonly record struct ActiveHitbox
    {
        public Entity Owner { get; init; }
        public RectangleF Bounds { get; init; }
        public int Damage { get; init; }
        public bool Knockdown { get; init; }
        public AttackStrength Strength { get; init; }
        public SfxId? ImpactSfx { get; init; }
    }

    private readonly List<ActiveHitbox> _activeHitboxes = [];
    private readonly Dictionary<Entity, HashSet<IDamageable>> _attackDedup = [];
    private readonly List<HitResult> _resultBuffer = [];
    private readonly List<RectangleF> _boundsBuffer = [];

    public void RegisterFrameHitboxes(Entity owner, MoveData move, int frameIndex, FacingDirection facing)
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
                if (target == active.Owner) continue;
                if (!active.Bounds.Intersects(target.Frame)) continue;
                if (target is not IDamageable tgt) continue;

                if (!_attackDedup.TryGetValue(active.Owner, out var ownerDedup))
                {
                    ownerDedup = [];
                    _attackDedup[active.Owner] = ownerDedup;
                }
                if (!ownerDedup.Add(tgt)) continue;

                if (active.Owner is IDamageable src && src.Faction == tgt.Faction) continue;

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
        _attackDedup.Remove(owner as Entity);
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