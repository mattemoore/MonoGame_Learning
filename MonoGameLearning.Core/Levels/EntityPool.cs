using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Core.Levels;

public class EntityPool<TEnemy>(
    EntityService entityManager,
    Func<WorldSnapshot> getWorld,
    Func<string, int, Func<WorldSnapshot>, TEnemy> factory)
    where TEnemy : Entity
{
    private static readonly Vector2 Sentinel = new(-99999, -99999);

    protected readonly EntityService EntityService = entityManager;
    private readonly Func<WorldSnapshot> _getWorld = getWorld;
    private readonly Func<string, int, Func<WorldSnapshot>, TEnemy> _factory = factory;
    private readonly Dictionary<string, Stack<TEnemy>> _free = [];
    private readonly Dictionary<TEnemy, string> _entityType = [];

    public void Build(LevelData level)
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
                _entityType[enemy] = type;
            }
            _free[type] = stack;
        }
    }

    public virtual TEnemy Rent(string type, Vector2 position, Entity target)
    {
        if (!_free.TryGetValue(type, out var stack) || stack.Count == 0)
            throw new InvalidOperationException($"Pool exhausted for enemy type '{type}'.");

        var enemy = stack.Pop();
        OnRentEnemy(enemy, position, target);
        EntityService.Register(enemy);
        return enemy;
    }

    protected virtual void OnRentEnemy(TEnemy enemy, Vector2 position, Entity target) { }

    public void Return(TEnemy enemy)
    {
        EntityService.Destroy(enemy);
        OnReturnEnemy(enemy);
        enemy.Position = Sentinel;

        if (_entityType.TryGetValue(enemy, out var type) &&
            _free.TryGetValue(type, out var stack))
            stack.Push(enemy);
    }

    protected virtual void OnReturnEnemy(TEnemy enemy) { }

    public void Clear()
    {
        _free.Clear();
        _entityType.Clear();
    }
}