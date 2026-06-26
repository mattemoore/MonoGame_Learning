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
    private readonly List<ICombatant> _combatants = [];

    public IReadOnlyList<Entity> All => _all;
    public IReadOnlyList<IUpdatable> Updatables => _updatables;
    public IReadOnlyList<IRenderable> Renderables => _renderables;
    public IReadOnlyList<ICollisionActor> Collidables => _collidables;
    public IReadOnlyList<IMoveableEntity> Movables => _movables;
    public IReadOnlyList<IDebugDrawable> DebugDrawables => _debugDrawables;
    public IReadOnlyList<ICombatant> Combatants => _combatants;
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

    private void AddToTypedLists(Entity entity)
    {
        if (entity is IUpdatable u) _updatables.Add(u);
        if (entity is IRenderable r) _renderables.Add(r);
        if (entity is ICollisionActor c) { _collidables.Add(c); _collision.Insert(c); }
        if (entity is IDamageable d) _damageables.Add(d);
        if (entity is IHitboxProvider h) _hitboxProviders.Add(h);
        if (entity is IMoveableEntity m) _movables.Add(m);
        if (entity is IDebugDrawable dd) _debugDrawables.Add(dd);
        if (entity is ICombatant cb) _combatants.Add(cb);
    }

    private void RemoveFromTypedLists(Entity entity)
    {
        if (entity is ICollisionActor c) { _collidables.Remove(c); _collision.Remove(c); }
        if (entity is IUpdatable u) _updatables.Remove(u);
        if (entity is IRenderable r) _renderables.Remove(r);
        if (entity is IDamageable d) _damageables.Remove(d);
        if (entity is IHitboxProvider h) _hitboxProviders.Remove(h);
        if (entity is IMoveableEntity m) _movables.Remove(m);
        if (entity is IDebugDrawable dd) _debugDrawables.Remove(dd);
        if (entity is ICombatant cb) _combatants.Remove(cb);
    }
}