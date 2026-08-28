using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Levels;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.Levels;

#pragma warning disable CS9107 // Primary constructor params are used by EndTriggerX and CreateBackgroundRenderer
public class Level1(int gameWidth, int gameHeight) : Level(CreateWaveDefs(), gameWidth, gameHeight)
#pragma warning restore CS9107
{
    public override int BackgroundCount => 3;
    public override float WalkableTopY => 420f;
    public override float EndTriggerX => BackgroundCount * gameWidth - 100f;

    public override List<PropSpawnDef> Props =>
    [
        new("OilDrum", new Vector2(200, 560), Anchor: CollisionAnchor.Bottom),
        new("OilDrum", new Vector2(400, 560), Anchor: CollisionAnchor.Bottom),
        new("OilDrum", new Vector2(600, 560), Anchor: CollisionAnchor.Bottom),
        new("OilDrum", new Vector2(800, 460)),
        new("OilDrum", new Vector2(1000, 460), Drops:
        [
            new PickupSpawnDef("Food", default),
        ]),
        new("OilDrum", new Vector2(1200, 460)),
    ];

    public override List<PickupSpawnDef> Pickups =>
    [
        new PickupSpawnDef("Bat", new Vector2(350f, 556f)),
        new PickupSpawnDef("Food", new Vector2(1400f, 556f)),
    ];

    public override BackgroundRenderer CreateBackgroundRenderer(ContentManager content) =>
        BackgroundRenderer.Create(content, gameWidth, gameHeight, BackgroundCount, "backgrounds/background1");

    private static List<WaveDef> CreateWaveDefs() =>
    [
        new WaveDef(TriggerX: 800f, EndX: 1200f, Enemies:
        [
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Bottom),
            new EnemySpawnDef("Grunt", SpawnSide.Right, SpawnVertical.Bottom),
        ]),
        new WaveDef(TriggerX: 1600f, EndX: 2000f, Enemies:
        [
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Top, Drops:
            [
                new PickupSpawnDef("Food", default),
            ]),
            new EnemySpawnDef("Grunt", SpawnSide.Left, SpawnVertical.Bottom, Weapon: "Bat"),
        ])
    ];
}