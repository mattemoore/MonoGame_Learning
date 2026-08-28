using MonoGame.Extended;
using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Core.Combat;

internal readonly record struct ActiveHitbox
{
    public IHitboxProvider Owner { get; init; }
    public Faction OwnerFaction { get; init; }
    public RectangleF Bounds { get; init; }
    public int Damage { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
    public SfxId? ImpactSfx { get; init; }
}