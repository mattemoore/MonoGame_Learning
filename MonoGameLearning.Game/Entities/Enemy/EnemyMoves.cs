using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Enemy;

public static class EnemyMoves
{
    public static readonly Dictionary<string, MoveData> All = new()
    {
        ["attack1"] = new()
        {
            Name = "Punch",
            AnimationKey = EnemySprite.AnimationAttack1,
            Damage = 5,
            Strength = AttackStrength.Light,
            FrameHitboxes = new()
            {
                [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
                [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            }
        },
    };
}