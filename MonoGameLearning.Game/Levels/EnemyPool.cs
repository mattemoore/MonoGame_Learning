using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Sprites;

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

    protected readonly EntityManager EntityManager;
    private readonly LevelDirector _director;
    private readonly Func<string, int, EnemyEntity> _factory;
    protected readonly Dictionary<string, Stack<EnemyEntity>> Free = [];
    protected readonly Dictionary<EnemyEntity, string> EntityType = [];

    public EnemyPool(EntityManager entityManager, LevelDirector director, Func<string, int, EnemyEntity> factory = null)
    {
        EntityManager = entityManager;
        _director = director;
        _factory = factory ?? DefaultFactory;
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
                var enemy = _factory(type, i);
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
        EntityManager.Register(enemy);
        return enemy;
    }

    protected virtual void OnRentEnemy(EnemyEntity enemy, Vector2 position, Entity target)
    {
        enemy.Reset(position, target);
    }

    public void Return(EnemyEntity enemy)
    {
        EntityManager.Destroy(enemy);

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

    private EnemyEntity DefaultFactory(string type, int index)
    {
        EnemyEntity enemy = type switch
        {
            "Grunt" => new EnemyEntity($"grunt_pool_{index}", Sentinel, 2.0f, EnemySprite.Create(), _director),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        WarmUpAnimations(enemy);
        return enemy;
    }

    private void WarmUpAnimations(EnemyEntity enemy)
    {
        for (int i = 0; i < WarmUpKeys.Length; i++)
            enemy.Sprite.SetAnimation(WarmUpKeys[i]);
    }
}
