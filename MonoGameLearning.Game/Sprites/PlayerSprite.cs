using System;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Graphics;

namespace MonoGameLearning.Game.Sprites;

public static class PlayerSprite
{
    public const string AnimationIdle = "idle";
    public const string AnimationRun = "run";
    public const string AnimationAttack1 = "attack1";
    public const string AnimationAttack2 = "attack2";
    public const string AnimationAttack3 = "attack3";
    private const double FrameDuration = 0.1;

    public static AnimatedSprite GetPlayerSprite(ContentManager content)
    {
        Texture2DAtlas atlas = content.Load<Texture2DAtlas>("images/adventurer");
        SpriteSheet spriteSheet = new("adventurer", atlas);
        
        spriteSheet.DefineAnimation(AnimationIdle, builder => builder
            .IsLooping(true)
            .AddFrame("adventurer-idle-00", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-idle-01", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-idle-02", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-idle-03", TimeSpan.FromSeconds(FrameDuration)));

        spriteSheet.DefineAnimation(AnimationAttack1, builder => builder
            .IsLooping(false)
            .AddFrame("adventurer-attack1-00", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack1-01", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack1-02", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack1-03", TimeSpan.FromSeconds(FrameDuration)));

        spriteSheet.DefineAnimation(AnimationAttack2, builder => builder
            .IsLooping(false)
            .AddFrame("adventurer-attack2-00", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack2-01", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack2-02", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack2-03", TimeSpan.FromSeconds(FrameDuration)));

        spriteSheet.DefineAnimation(AnimationAttack3, builder => builder
            .IsLooping(false)
            .AddFrame("adventurer-attack3-00", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack3-01", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack3-02", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-attack3-03", TimeSpan.FromSeconds(FrameDuration)));

        spriteSheet.DefineAnimation(AnimationRun, builder => builder
            .IsLooping(true)
            .AddFrame("adventurer-run-00", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-run-01", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-run-02", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-run-03", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-run-04", TimeSpan.FromSeconds(FrameDuration))
            .AddFrame("adventurer-run-05", TimeSpan.FromSeconds(FrameDuration)));

        return new(spriteSheet, AnimationIdle);
    }
}