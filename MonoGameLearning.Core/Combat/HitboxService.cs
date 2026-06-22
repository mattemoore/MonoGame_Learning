using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGameLearning.Core.Entities;

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

public readonly record struct HitResult
{
    public Entity Target { get; init; }
    public int Damage { get; init; }
    public Entity Source { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
}

public class HitboxService
{
    private record ActiveHitbox
    {
        public Entity Owner { get; init; }
        public RectangleF Bounds { get; init; }
        public int Damage { get; init; }
        public bool Knockdown { get; init; }
        public AttackStrength Strength { get; init; }
        public HitboxData Definition { get; init; }
    }

    private readonly List<ActiveHitbox> _activeHitboxes = [];
    private readonly Dictionary<Entity, HashSet<(HitboxData, Entity)>> _resolvedThisFrame = [];
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
                Definition = hb
            });
        }
    }

    public List<HitResult> ResolveHits(IEnumerable<Entity> targets)
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

                if (active.Owner is ICombatant src && target is ICombatant tgt && src.Faction == tgt.Faction) continue;

                _resultBuffer.Add(new()
                {
                    Target = target,
                    Damage = active.Damage,
                    Source = active.Owner,
                    Knockdown = active.Knockdown,
                    Strength = active.Strength
                });
            }
        }

        return _resultBuffer;
    }

    public void Clear(Entity owner)
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

    public IReadOnlyList<RectangleF> GetActiveHitboxBounds(Entity owner)
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