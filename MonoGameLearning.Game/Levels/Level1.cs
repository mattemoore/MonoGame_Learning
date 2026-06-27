using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Game.Rendering;

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
        new("OilDrum", new Vector2(700, 450)),
        new("OilDrum", new Vector2(900, 450)),
        new("OilDrum", new Vector2(800, 450))
    ];

    public override BackgroundRenderer CreateBackgroundRenderer(ContentManager content) =>
        BackgroundRenderer.Create(content, gameWidth, gameHeight, BackgroundCount);

    private static List<WaveDef> CreateWaveDefs() =>
    [
        new WaveDef(TriggerX: 800f, Enemies:
        [
            new EnemySpawnDef("Grunt", new Vector2(850, 550)),
            new EnemySpawnDef("Grunt", new Vector2(900, 550))
        ]),
        new WaveDef(TriggerX: 1600f, Enemies:
        [
            new EnemySpawnDef("Grunt", new Vector2(1650, 550)),
            new EnemySpawnDef("Grunt", new Vector2(1700, 550))
        ])
    ];
}