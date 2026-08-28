using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Levels;

public abstract class EntityPool<TEnemy>(
    EntityService entityManager,
    Func<WorldSnapshot> getWorld,
    Func<string, int, Func<WorldSnapshot>, TEnemy> factory)
    where TEnemy : Entity
{
    private static readonly Vector2 Sentinel = new(-99999, -99999);

    protected readonly EntityService EntityService = entityManager;
    private readonly Func<WorldSnapshot> _getWorld = getWorld;
    private readonly Func<string, int, Func<WorldSnapshot>, TEnemy> _factory = factory;
    protected readonly Dictionary<string, Stack<TEnemy>> Free = [];
    protected readonly Dictionary<TEnemy, string> EntityType = [];

    public void Build(Level level)
    {
        var maxPerType = new Dictionary<string, int>();
        foreach (var wave in level.WaveDefs)
        {
            foreach (var def in wave.Enemies)
            {
                maxPerType.TryGetValue(def.Type, out var count);
                maxPerType[def.Type] = count + 1;
            }
        }

        foreach (var (type, count) in maxPerType)
        {
            var stack = new Stack<TEnemy>(count);
            for (int i = 0; i < count; i++)
            {
                var enemy = _factory(type, i, _getWorld);
                enemy.Position = Sentinel;
                stack.Push(enemy);
                EntityType[enemy] = type;
            }
            Free[type] = stack;
        }
    }

    public virtual TEnemy Rent(string type, Vector2 position, Entity target)
    {
        if (!Free.TryGetValue(type, out var stack) || stack.Count == 0)
            throw new InvalidOperationException($"Pool exhausted for enemy type '{type}'.");

        var enemy = stack.Pop();
        OnRentEnemy(enemy, position, target);
        EntityService.Register(enemy);
        return enemy;
    }

    protected abstract void OnRentEnemy(TEnemy enemy, Vector2 position, Entity target);

    public void Return(TEnemy enemy)
    {
        EntityService.Destroy(enemy);
        OnReturnEnemy(enemy);
        enemy.Position = Sentinel;

        if (EntityType.TryGetValue(enemy, out var type) &&
            Free.TryGetValue(type, out var stack))
            stack.Push(enemy);
    }

    protected abstract void OnReturnEnemy(TEnemy enemy);

    public void Clear()
    {
        Free.Clear();
        EntityType.Clear();
    }
}