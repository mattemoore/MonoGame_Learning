using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Entities.Enemy;
using MonoGameLearning.Game.Entities.Pickups;
using MonoGameLearning.Game.Entities.Props;
using MonoGameLearning.Game.Weapons;

namespace MonoGameLearning.Game.Levels;

public sealed class LevelEntityFactory(
    AudioService audio,
    Func<AnimatedSprite> createEnemySprite,
    Func<AnimatedSprite> createOilDrumSprite,
    Texture2D foodPickupTexture,
    MeleeWeaponDef batWeapon,
    Func<RectangleF> getCameraView)
{
    private readonly AudioService _audio = audio;
    private readonly Func<AnimatedSprite> _createEnemySprite = createEnemySprite;
    private readonly Func<AnimatedSprite> _createOilDrumSprite = createOilDrumSprite;
    private readonly Texture2D _foodPickupTexture = foodPickupTexture;
    private readonly MeleeWeaponDef _batWeapon = batWeapon;
    private readonly Func<RectangleF> _getCameraView = getCameraView;

    public static readonly string[] EnemyWarmUpKeys =
    [
        EnemySprite.AnimationIdle,
        EnemySprite.AnimationRun,
        EnemySprite.AnimationAttack1,
        EnemySprite.AnimationHurt,
        EnemySprite.AnimationFall,
        EnemySprite.AnimationDie,
        EnemySprite.AnimationGetUp,
    ];

    public PropBase CreateProp(PropSpawnDef def) =>
        new OilDrumEntity(def.Type, def.Position, 1.0f, _createOilDrumSprite(), _audio, anchor: def.Anchor)
        {
            Drops = def.Drops
        };

    public Entity CreatePickup(PickupSpawnDef def) => def.Type switch
    {
        LevelContent.Food => new FoodPickupEntity(def.Type, def.Position, _foodPickupTexture),
        LevelContent.Bat => new WeaponPickupEntity(def.Type, def.Position, _batWeapon),
        _ => throw new ArgumentException($"Unknown pickup type: {def.Type}", nameof(def)),
    };

    public EnemyEntity CreateEnemy(string type, int index, Func<WorldSnapshot> getWorld)
    {
        var enemy = type switch
        {
            LevelContent.Grunt => new EnemyEntity($"grunt_pool_{index}", Vector2.Zero, 2.0f, _createEnemySprite(), _audio, getWorld),
            _ => throw new ArgumentException($"Unknown enemy type: {type}", nameof(type)),
        };
        foreach (var key in EnemyWarmUpKeys)
            enemy.SpriteRenderer.SetAnimation(key);
        return enemy;
    }

    public void ConfigureSpawnedEnemy(EnemyEntity enemy, EnemySpawnDef def, FacingDirection initialFacing, MeleeWeaponDef weapon)
    {
        if (weapon is not null)
            enemy.EquipWeapon(weapon);

        // SpriteRenderer without an attached sprite (test enemies) → skip visual setup.
        if (enemy.SpriteRenderer.Sprite is not null)
        {
            float halfW = enemy.Width * 0.5f;
            var view = _getCameraView();
            float targetX = initialFacing == FacingDirection.Left
                ? view.X + view.Width - halfW - 50f
                : view.X + halfW + 50f;
            enemy.PrepareSpawn(initialFacing, targetX);
        }
    }
}