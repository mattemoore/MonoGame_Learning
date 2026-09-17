using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Core.Combat;

public enum AttackStrength { Light, Medium, Heavy }

public readonly record struct DamageInfo
{
    public IDamageable? Target { get; init; }
    public int Amount { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
    public SfxId? ImpactSfx { get; init; }

    /// <summary>The hitbox owner that produced this hit (e.g. the projectile to despawn).</summary>
    public IHitboxProvider? Source { get; init; }
}