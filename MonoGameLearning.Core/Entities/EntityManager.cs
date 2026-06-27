using System.Collections.Generic;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public class EntityManager(CollisionComponent collision)
{
    private CollisionComponent _collision = collision;

    public void SetCollisionComponent(CollisionComponent collision) => _collision = collision;

    public void Clear()
    {
        _all.Clear();
        _updatables.Clear();
        _renderables.Clear();
        _collidables.Clear();
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
    private readonly List<ICollisionActor> _collidables = [];
    private readonly List<IDamageable> _damageables = [];
    private readonly List<IHitboxProvider> _hitboxProviders = [];
    private readonly List<IMoveableEntity> _movables = [];
    private readonly List<IDebugDrawable> _debugDrawables = [];
    private readonly List<IDamageable> _combatants = [];

    public IReadOnlyList<Entity> All => _all;
    public IReadOnlyList<IUpdatable> Updatables => _updatables;
    public IReadOnlyList<IRenderable> Renderables => _renderables;
    public IReadOnlyList<ICollisionActor> Collidables => _collidables;
    public IReadOnlyList<IMoveableEntity> Movables => _movables;
    public IReadOnlyList<IDebugDrawable> DebugDrawables => _debugDrawables;
    public IReadOnlyList<IDamageable> Combatants => _combatants;
    public IReadOnlyList<IHitboxProvider> HitboxProviders => _hitboxProviders;

    public void Register(Entity entity)
    {
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

    private static void TryAdd<T>(Entity entity, List<T> list, System.Action<T> extra = null) where T : class
    {
        if (entity is T t)
        {
            list.Add(t);
            extra?.Invoke(t);
        }
    }

    private static void TryRemove<T>(Entity entity, List<T> list, System.Action<T> extra = null) where T : class
    {
        if (entity is T t)
        {
            list.Remove(t);
            extra?.Invoke(t);
        }
    }

    private void AddToTypedLists(Entity entity)
    {
        TryAdd<IUpdatable>(entity, _updatables);
        TryAdd<IRenderable>(entity, _renderables);
        TryAdd<ICollisionActor>(entity, _collidables, c => _collision.Insert(c));
        TryAdd<IDamageable>(entity, _damageables);
        TryAdd<IHitboxProvider>(entity, _hitboxProviders);
        TryAdd<IMoveableEntity>(entity, _movables);
        TryAdd<IDebugDrawable>(entity, _debugDrawables);
        TryAdd<IDamageable>(entity, _combatants);
    }

    private void RemoveFromTypedLists(Entity entity)
    {
        TryRemove<ICollisionActor>(entity, _collidables, c => _collision.Remove(c));
        TryRemove<IUpdatable>(entity, _updatables);
        TryRemove<IRenderable>(entity, _renderables);
        TryRemove<IDamageable>(entity, _damageables);
        TryRemove<IHitboxProvider>(entity, _hitboxProviders);
        TryRemove<IMoveableEntity>(entity, _movables);
        TryRemove<IDebugDrawable>(entity, _debugDrawables);
        TryRemove<IDamageable>(entity, _combatants);
    }
}