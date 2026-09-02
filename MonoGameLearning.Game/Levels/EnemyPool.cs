using System;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Levels;

public class EnemyPool : EntityPool<EnemyEntity>
{
    public EnemyPool(EntityService entityManager, Func<WorldSnapshot> getWorld, Func<string, int, Func<WorldSnapshot>, EnemyEntity> factory)
        : base(entityManager, getWorld, factory)
    {
    }

    protected override void OnRentEnemy(EnemyEntity enemy, Vector2 position, Entity target)
    {
        enemy.Reset(position, target);
    }

    protected override void OnReturnEnemy(EnemyEntity enemy)
    {
        enemy.ClearCombatState();
    }
}