using System;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Game.Sprites;

public static class PlayerSprite
{
    public static AnimatedSprite GetPlayerSprite(ContentManager content)
    {
        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/adventurer");
        SpriteSheet spriteSheet = new SpriteSheet("adventurer", atlas);
        spriteSheet.DefineAnimation("idle", builder =>
        {
            builder.IsLooping(true)
            .AddFrame("adventurer-idle-00", System.TimeSpan.FromSeconds(0.1))
            .AddFrame("adventurer-idle-01", TimeSpan.FromSeconds(0.1))
            .AddFrame("adventurer-idle-02", TimeSpan.FromSeconds(0.1))
            .AddFrame("adventurer-idle-03", TimeSpan.FromSeconds(0.1));
        });
        spriteSheet.DefineAnimation("attack1", builder =>
        {
            builder.IsLooping(false)
             .AddFrame("adventurer-attack1-00", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack1-01", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack1-02", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack1-03", TimeSpan.FromSeconds(0.1));
        });
        spriteSheet.DefineAnimation("attack2", builder =>
        {
            builder.IsLooping(false)
             .AddFrame("adventurer-attack2-00", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack2-01", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack2-02", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack2-03", TimeSpan.FromSeconds(0.1));
        });
        spriteSheet.DefineAnimation("attack3", builder =>
        {
            builder.IsLooping(false)
             .AddFrame("adventurer-attack3-00", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack3-01", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack3-02", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack3-03", TimeSpan.FromSeconds(0.1));
        });
        spriteSheet.DefineAnimation("run", builder =>
        {
            builder.IsLooping(true)
             .AddFrame("adventurer-run-00", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-run-01", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-run-02", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-run-03", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-run-04", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-run-05", TimeSpan.FromSeconds(0.1));
        });

        AnimatedSprite sprite = new AnimatedSprite(spriteSheet, "idle");
        return sprite;
    }
}