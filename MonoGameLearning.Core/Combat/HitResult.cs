using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Core.Combat;

public readonly record struct HitResult
{
    public IDamageable Target { get; init; }
    public int Damage { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
    public SfxId? ImpactSfx { get; init; }
}
