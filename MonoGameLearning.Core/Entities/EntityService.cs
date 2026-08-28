using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Entities;

public class EntityService(CollisionWorld2D world, HitboxService? hitboxService = null)
{
    private readonly HitboxService? _hitboxService = hitboxService;

    public void Clear()
    {
        _all.Clear();
        _updatables.Clear();
        _renderables.Clear();
        _collidablesByLayer.Clear();
        _damageables.Clear();
        _hitboxProviders.Clear();
        _movables.Clear();
        _debugDrawables.Clear();
        _props.Clear();
        _pendingDestroy.Clear();
    }

    private readonly List<Entity> _all = [];
    private readonly List<Entity> _pendingDestroy = [];

    private readonly List<IUpdatable> _updatables = [];
    private readonly List<IRenderable> _renderables = [];
    private readonly Dictionary<string, List<ICollisionActor>> _collidablesByLayer = [];
    private readonly List<IDamageable> _damageables = [];
    private readonly List<IHitboxProvider> _hitboxProviders = [];
    private readonly List<IMoveable> _movables = [];
    private readonly List<IDebugDrawable> _debugDrawables = [];
    private readonly List<PropBase> _props = [];

    public IReadOnlyList<Entity> All => _all;
    public IReadOnlyList<IUpdatable> Updatables => _updatables;
    public IReadOnlyList<IRenderable> Renderables => _renderables;

    private static readonly RenderableYComparer _renderableYComparer = new();

    public void SortRenderablesByY() => _renderables.Sort(_renderableYComparer);
    public IReadOnlyList<ICollisionActor> GetCollidables(string layer) =>
        _collidablesByLayer.TryGetValue(layer, out var list) ? list : [];
    public IReadOnlyList<ICollisionActor> PickupCollidables =>
        _collidablesByLayer.TryGetValue(CollisionLayers.Pickups, out var list) ? list : [];
    public IReadOnlyList<IMoveable> Movables => _movables;
    public IReadOnlyList<IDebugDrawable> DebugDrawables => _debugDrawables;
    public IReadOnlyList<PropBase> Props => _props;
    public IReadOnlyList<IHitboxProvider> HitboxProviders => _hitboxProviders;

    public void Register(Entity entity)
    {
        if (_all.Contains(entity)) return;
        _all.Add(entity);
        AddToTypedLists(entity);
    }

    public void Destroy(Entity entity) => _pendingDestroy.Add(entity);

    public void ProcessPending()
    {
        if (_pendingDestroy.Count == 0) return;

        foreach (var entity in _pendingDestroy)
        {
            _all.Remove(entity);
            RemoveFromTypedLists(entity);
        }
        _pendingDestroy.Clear();
    }

    public IDamageable? FindNearestAliveEnemy(Vector2 origin)
    {
        IDamageable? nearest = null;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < _all.Count; i++)
        {
            if (_all[i] is CombatActorBase { IsAlive: true, Faction: Faction.Enemy } d)
            {
                float dist = Math.Abs(((Entity)d).Position.X - origin.X);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = d;
                }
            }
        }
        return nearest;
    }

    private static bool TryAdd<T>(Entity entity, List<T> list) where T : class
    {
        if (entity is T t)
        {
            list.Add(t);
            return true;
        }
        return false;
    }

    private static void TryRemove<T>(Entity entity, List<T> list) where T : class
    {
        if (entity is T t)
            list.Remove(t);
    }

    private void AddToTypedLists(Entity entity)
    {
        TryAdd<IUpdatable>(entity, _updatables);

        TryAdd<IRenderable>(entity, _renderables);
        if (entity is ICollisionLayer { } layer && entity is ICollisionActor c)
            AddToCollidables(c, layer.LayerName);
        TryAdd<IDamageable>(entity, _damageables);
        if (TryAdd<IHitboxProvider>(entity, _hitboxProviders))
            TryInjectHitboxService(entity);
        TryAdd<IMoveable>(entity, _movables);
        TryAdd<IDebugDrawable>(entity, _debugDrawables);
        if (entity is PropBase prop)
            _props.Add(prop);
    }

    private void AddToCollidables(ICollisionActor c, string layer)
    {
        if (!_collidablesByLayer.TryGetValue(layer, out var list))
            _collidablesByLayer[layer] = list = [];
        list.Add(c);
        world.Insert(c, layer);
    }

    private void RemoveFromTypedLists(Entity entity)
    {
        if (entity is ICollisionLayer { } layer && entity is ICollisionActor c)
        {
            if (_collidablesByLayer.TryGetValue(layer.LayerName, out var list))
                list.Remove(c);
            world.Remove(c);
        }
        TryRemove<IUpdatable>(entity, _updatables);
        TryRemove<IRenderable>(entity, _renderables);
        TryRemove<IDamageable>(entity, _damageables);
        TryRemove<IHitboxProvider>(entity, _hitboxProviders);
        TryRemove<IMoveable>(entity, _movables);
        TryRemove<IDebugDrawable>(entity, _debugDrawables);
        if (entity is PropBase prop)
            _props.Remove(prop);
    }

    private void TryInjectHitboxService(Entity entity)
    {
        if (entity is not IHitboxProvider provider)
            return;

        if (_hitboxService is null)
        {
            Debug.WriteLine($"[EntityService] HitboxService is null — {entity.GetType().Name} \"{entity.Name}\" registered without hitbox support");
            return;
        }

        provider.HitboxService = _hitboxService;
    }
}
