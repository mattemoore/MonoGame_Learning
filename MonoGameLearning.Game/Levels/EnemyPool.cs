using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.AnimatedSprites;

namespace MonoGameLearning.Game.Levels;

public class EnemyPool
{
    private static readonly Vector2 Sentinel = new(-99999, -99999);

    private static readonly string[] WarmUpKeys =
    [
        EnemySprite.AnimationIdle,
        EnemySprite.AnimationRun,
        EnemySprite.AnimationAttack1,
        EnemySprite.AnimationHurt,
        EnemySprite.AnimationFall,
        EnemySprite.AnimationDie,
        EnemySprite.AnimationGetUp,
    ];

    protected readonly EntityService EntityService;
    private readonly Func<WorldSnapshot> _getWorld;
    private readonly Func<string, int, Func<WorldSnapshot>, EnemyEntity> _factory;
    protected readonly Dictionary<string, Stack<EnemyEntity>> Free = [];
    protected readonly Dictionary<EnemyEntity, string> EntityType = [];

    public EnemyPool(EntityService entityManager, Func<WorldSnapshot> getWorld, Func<string, int, Func<WorldSnapshot>, EnemyEntity> factory)
    {
        EntityService = entityManager;
        _getWorld = getWorld;
        _factory = factory;
    }

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
            var stack = new Stack<EnemyEntity>(count);
            for (int i = 0; i < count; i++)
            {
                var enemy = _factory(type, i, _getWorld);
                WarmUpAnimations(enemy);
                enemy.Position = Sentinel;
                stack.Push(enemy);
                EntityType[enemy] = type;
            }
            Free[type] = stack;
        }
    }

    public virtual EnemyEntity Rent(string type, Vector2 position, Entity target)
    {
        if (!Free.TryGetValue(type, out var stack) || stack.Count == 0)
            throw new InvalidOperationException($"Pool exhausted for enemy type '{type}'.");

        var enemy = stack.Pop();
        OnRentEnemy(enemy, position, target);
        EntityService.Register(enemy);
        return enemy;
    }

    protected virtual void OnRentEnemy(EnemyEntity enemy, Vector2 position, Entity target)
    {
        enemy.Reset(position, target);
    }

    public void Return(EnemyEntity enemy)
    {
        EntityService.Destroy(enemy);

        if (enemy.HitboxService is not null)
        {
            enemy.HitboxService.Clear(enemy);
            enemy.HitboxService.ClearAttackDedup(enemy);
        }

        enemy.Position = Sentinel;

        if (EntityType.TryGetValue(enemy, out var type) &&
            Free.TryGetValue(type, out var stack))
        {
            stack.Push(enemy);
        }
    }

    public void Clear()
    {
        Free.Clear();
        EntityType.Clear();
    }

    private void WarmUpAnimations(EnemyEntity enemy)
    {
        for (int i = 0; i < WarmUpKeys.Length; i++)
            enemy.SpriteRenderer.SetAnimation(WarmUpKeys[i]);
    }
}
