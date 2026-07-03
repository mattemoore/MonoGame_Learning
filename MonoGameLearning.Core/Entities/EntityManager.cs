using System.Collections.Generic;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public class EntityManager(CollisionWorld2D world)
{
    public void Clear()
    {
        _all.Clear();
        _updatables.Clear();
        _renderables.Clear();
        _screenRenderables.Clear();
        _actorCollidables.Clear();
        _propCollidables.Clear();
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
    private readonly List<IDamageable> _damageables = [];
    private readonly List<IHitboxProvider> _hitboxProviders = [];
    private readonly List<IMoveableEntity> _movables = [];
    private readonly List<IDebugDrawable> _debugDrawables = [];
    private readonly List<IDamageable> _combatants = [];

    public IReadOnlyList<Entity> All => _all;
    public IReadOnlyList<IUpdatable> Updatables => _updatables;
    public IReadOnlyList<IRenderable> Renderables => _renderables;
    public IReadOnlyList<IScreenRenderable> ScreenRenderables => _screenRenderables;
    public IReadOnlyList<ICollisionActor> ActorCollidables => _actorCollidables;
    public IReadOnlyList<ICollisionActor> PropCollidables => _propCollidables;
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

    private static void TryAdd<T>(Entity entity, List<T> list) where T : class
    {
        if (entity is T t)
            list.Add(t);
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
        TryAdd<IDamageable>(entity, _damageables);
        TryAdd<IHitboxProvider>(entity, _hitboxProviders);
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
}
