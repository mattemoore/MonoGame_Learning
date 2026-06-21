using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities;

public static class PlayerMoves
{
    public static readonly Dictionary<string, MoveData> All = new()
    {
        ["attack1"] = new()
        {
            Name = "Punch",
            AnimationKey = PlayerSprite.AnimationAttack1,
            Damage = 5,
            Strength = AttackStrength.Light,
            FrameHitboxes = new()
            {
                [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
                [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            }
        },
        ["attack2"] = new()
        {
            Name = "Uppercut",
            AnimationKey = PlayerSprite.AnimationAttack2,
            Damage = 8,
            Strength = AttackStrength.Medium,
            FrameHitboxes = new()
            {
                [1] = [new() { Offset = new Vector2(45, -10), Size = new Point(50, 50) }],
                [2] = [new() { Offset = new Vector2(45, -10), Size = new Point(50, 50) }],
            }
        },
        ["attack3"] = new()
        {
            Name = "Strong Punch",
            AnimationKey = PlayerSprite.AnimationAttack3,
            Damage = 12,
            Knockdown = true,
            Strength = AttackStrength.Heavy,
            FrameHitboxes = new()
            {
                [2] = [new() { Offset = new Vector2(50, 0), Size = new Point(55, 40) }],
            }
        },
    };
}