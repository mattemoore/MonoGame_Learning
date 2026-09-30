#nullable enable
using System;
using MonoGame.Extended;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Levels;

public class LevelDirector(EntityService entityManager, LevelData level, Entity player, AudioService audio,
    Func<PropSpawnDef, PropBase> createProp, Func<PickupSpawnDef, Entity> createPickup,
    Func<string, MeleeWeaponDef> getWeapon, Func<string, int, Func<WorldSnapshot>, EnemyEntity> createEnemy,
    Action<EnemyEntity, EnemySpawnDef, FacingDirection, MeleeWeaponDef?> onEnemySpawned,
    Func<RectangleF> getCameraView)
    : LevelDirectorCore<EnemyEntity>(entityManager, level, player, audio, createProp, createPickup, getWeapon, createEnemy, onEnemySpawned, getCameraView)
{
    protected override EntityPool<EnemyEntity> CreateDefaultPool()
    {
        var pool = new EnemyPool(EntityManager, () => CurrentWorld, CreateEnemy);
        pool.Build(Level);
        return pool;
    }
}