using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public class EntityManager(CollisionWorld2D world)
{
    public HitboxService HitboxService { get; set; }
    public void Clear()
    {
        _all.Clear();
        _updatables.Clear();
        _renderables.Clear();
        _screenRenderables.Clear();
        _actorCollidables.Clear();
        _propCollidables.Clear();
        _pickupCollidables.Clear();
        _damageables.Clear();
        _hitboxProviders.Clear();
        _movables.Clear();
        _debugDrawables.Clear();
        _combatants.Clear();
        _pendingDestroy.Clear();
    }

    private readonly List<Entity> _all = [];
    private readonly List<Entity> _pendingDestroy = [];

    private readonly List<IUpdatable> _updatables = [];
    private readonly List<IRenderable> _renderables = [];
    private readonly List<IScreenRenderable> _screenRenderables = [];
    private readonly List<ICollisionActor> _actorCollidables = [];
    private readonly List<ICollisionActor> _propCollidables = [];
    private readonly List<ICollisionActor> _pickupCollidables = [];
    private readonly List<IDamageable> _damageables = [];
    private readonly List<IHitboxProvider> _hitboxProviders = [];
    private readonly List<IMoveableEntity> _movables = [];
    private readonly List<IDebugDrawable> _debugDrawables = [];
    private readonly List<IDamageable> _combatants = [];

    public IReadOnlyList<Entity> All => _all;
    public IReadOnlyList<IUpdatable> Updatables => _updatables;
    public IReadOnlyList<IRenderable> Renderables => _renderables;
    public IReadOnlyList<IScreenRenderable> ScreenRenderables => _screenRenderables;

    private static readonly RenderableYComparer _renderableYComparer = new();

    private readonly struct RenderableYComparer : IComparer<IRenderable>
    {
        public int Compare(IRenderable x, IRenderable y)
        {
            if (x is not Entity ex || y is not Entity ey) return 0;
            float diff = ex.Position.Y - ey.Position.Y;
            return diff < 0 ? -1 : diff > 0 ? 1 : 0;
        }
    }

    public void SortRenderablesByY() => _renderables.Sort(_renderableYComparer);
    public IReadOnlyList<ICollisionActor> ActorCollidables => _actorCollidables;
    public IReadOnlyList<ICollisionActor> PropCollidables => _propCollidables;
    public IReadOnlyList<ICollisionActor> PickupCollidables => _pickupCollidables;
    public IReadOnlyList<IMoveableEntity> Movables => _movables;
    public IReadOnlyList<IDebugDrawable> DebugDrawables => _debugDrawables;
    public IReadOnlyList<IDamageable> Combatants => _combatants;
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

    public IDamageable FindNearestAliveEnemy(Vector2 origin)
    {
        IDamageable nearest = null;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < _all.Count; i++)
        {
            if (_all[i] is IDamageable { IsAlive: true, Faction: Faction.Enemy } d)
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

        if (entity is IScreenRenderable screen)
            _screenRenderables.Add(screen);
        else if (entity is IRenderable renderable)
            _renderables.Add(renderable);
        if (entity is CombatActorBase actor)
        {
            _actorCollidables.Add(actor);
            world.Insert(actor, "actors");
        }
        else if (entity is PropBase prop)
        {
            _propCollidables.Add(prop);
            world.Insert(prop, "props");
        }
        else if (entity is IPickup pickup && entity is ICollisionActor pickupCollidable)
        {
            _pickupCollidables.Add(pickupCollidable);
            world.Insert(pickupCollidable, "pickups");
        }
        TryAdd<IDamageable>(entity, _damageables);
        if (TryAdd<IHitboxProvider>(entity, _hitboxProviders))
            TryInjectHitboxService(entity);
        TryAdd<IMoveableEntity>(entity, _movables);
        TryAdd<IDebugDrawable>(entity, _debugDrawables);
        TryAdd<IDamageable>(entity, _combatants);
    }

    private void RemoveFromTypedLists(Entity entity)
    {
        if (entity is CombatActorBase actor)
        {
            _actorCollidables.Remove(actor);
            world.Remove(actor);
        }
        else if (entity is PropBase prop)
        {
            _propCollidables.Remove(prop);
            world.Remove(prop);
        }
        else if (entity is IPickup && entity is ICollisionActor pickupCollidable)
        {
            _pickupCollidables.Remove(pickupCollidable);
            world.Remove(pickupCollidable);
        }
        TryRemove<IUpdatable>(entity, _updatables);
        if (entity is IScreenRenderable screen)
            _screenRenderables.Remove(screen);
        else
            TryRemove<IRenderable>(entity, _renderables);
        TryRemove<IDamageable>(entity, _damageables);
        TryRemove<IHitboxProvider>(entity, _hitboxProviders);
        TryRemove<IMoveableEntity>(entity, _movables);
        TryRemove<IDebugDrawable>(entity, _debugDrawables);
        TryRemove<IDamageable>(entity, _combatants);
    }

    private void TryInjectHitboxService(Entity entity)
    {
        if (entity is not IHitboxProvider provider)
            return;

        if (HitboxService is null)
        {
            Debug.WriteLine($"[EntityManager] HitboxService is null — {entity.GetType().Name} \"{entity.Name}\" registered without hitbox support");
            return;
        }

        provider.HitboxService = HitboxService;
    }
}
