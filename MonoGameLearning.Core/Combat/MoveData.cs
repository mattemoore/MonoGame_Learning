using System.Collections.Generic;

namespace MonoGameLearning.Core.Combat;

public class MoveData
{
    public string Name { get; init; }
    public string AnimationKey { get; init; }
    public int Damage { get; init; }
    public bool Knockdown { get; init; }
    public Dictionary<int, List<HitboxData>> FrameHitboxes { get; init; } = [];
}