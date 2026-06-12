using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Combat;

public class MoveData
{
    public string Name { get; init; }
    public string AnimationKey { get; init; }
    public int Damage { get; init; }
    public Vector2 Knockback { get; init; } // authored for FacingDirection.Right (negated in HitboxService when facing left)
    public Dictionary<int, List<HitboxData>> FrameHitboxes { get; init; } = [];
}