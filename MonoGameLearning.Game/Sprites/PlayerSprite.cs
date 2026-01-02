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
        spriteSheet.DefineAnimation("attack", builder =>
        {
            builder.IsLooping(false)
             .AddFrame("adventurer-attack1-00", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack1-01", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack1-02", TimeSpan.FromSeconds(0.1))
             .AddFrame("adventurer-attack1-03", TimeSpan.FromSeconds(0.1));
        });
        AnimatedSprite sprite = new AnimatedSprite(spriteSheet, "idle");
        return sprite;
    }
}