using System.Collections.Generic;
using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Core.Combat;

public class MoveData
{
    public string Name { get; init; }
    public string AnimationKey { get; init; }
    public int Damage { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
    public SfxId? AttackSfx { get; init; }
    public SfxId? ImpactSfx { get; init; }
    public Dictionary<int, List<HitboxData>> FrameHitboxes { get; init; } = [];
}