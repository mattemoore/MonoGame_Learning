using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.Levels;

public static class Level1
{
    public const string BackgroundAsset = "backgrounds/background1";

    public static LevelData Create(int gameWidth, int gameHeight)
    {
        var data = new LevelData(
            BackgroundCount: 3,
            GameWidth: gameWidth,
            GameHeight: gameHeight,
            EndTriggerX: 3 * gameWidth - 100f,
            WalkableTopY: 420f,
            Props:
            [
                new PropSpawnDef(LevelContent.OilDrum, new Vector2(200, 560), Anchor: CollisionAnchor.Bottom),
                new PropSpawnDef(LevelContent.OilDrum, new Vector2(400, 560), Anchor: CollisionAnchor.Bottom),
                new PropSpawnDef(LevelContent.OilDrum, new Vector2(600, 560), Anchor: CollisionAnchor.Bottom),
                new PropSpawnDef(LevelContent.OilDrum, new Vector2(800, 460)),
                new PropSpawnDef(LevelContent.OilDrum, new Vector2(1000, 460), Drops:
                [
                    new PickupSpawnDef(LevelContent.Food, default),
                ]),
                new PropSpawnDef(LevelContent.OilDrum, new Vector2(1200, 460)),
            ],
            Pickups:
            [
                new PickupSpawnDef(LevelContent.Bat, new Vector2(350f, 556f)),
                new PickupSpawnDef(LevelContent.Food, new Vector2(1400f, 556f)),
            ],
            WaveDefs: CreateWaveDefs());

        LevelData.Validate(data);
        return data;
    }

    public static BackgroundRenderer CreateBackgroundRenderer(ContentManager content, LevelData level) =>
        BackgroundRenderer.Create(content, level.GameWidth, level.GameHeight, level.BackgroundCount, BackgroundAsset);

    private static List<WaveDef> CreateWaveDefs() =>
    [
        new WaveDef(TriggerX: 800f, EndX: 1200f, Enemies:
        [
            new EnemySpawnDef(LevelContent.Grunt, SpawnSide.Left, SpawnVertical.Bottom),
            new EnemySpawnDef(LevelContent.Grunt, SpawnSide.Right, SpawnVertical.Bottom),
        ]),
        new WaveDef(TriggerX: 1600f, EndX: 2000f, Enemies:
        [
            new EnemySpawnDef(LevelContent.Grunt, SpawnSide.Left, SpawnVertical.Top, Drops:
            [
                new PickupSpawnDef(LevelContent.Food, default),
            ]),
            new EnemySpawnDef(LevelContent.Grunt, SpawnSide.Left, SpawnVertical.Bottom, Weapon: LevelContent.Bat),
        ])
    ];
}