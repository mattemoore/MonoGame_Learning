using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Audio;

namespace MonoGameLearning.Core.Combat;

/// <summary>
/// A throwable weapon: held using the base carry pose, then thrown on primary attack.
/// The actor plays <see cref="ThrowMove"/>; at <see cref="SpawnFrame"/> the projectile is
/// spawned and the weapon is consumed. The projectile's hitbox/damage live here so the
/// entity stays a dumb carrier of the launched state.
/// </summary>
public class ThrowableWeaponDef : WeaponDef
{
    public required MoveData ThrowMove { get; init; }

    /// <summary>Animation frame (after the attack starts) on which the projectile leaves the hand.</summary>
    public int SpawnFrame { get; init; } = 1;

    public HitboxData ProjectileHitbox { get; init; }
    public int Damage { get; init; }
    public bool Knockdown { get; init; }
    public AttackStrength Strength { get; init; }
    public SfxId? ImpactSfx { get; init; }

    public required string ProjectileRegionPrefix { get; init; }
    public int ProjectileFrameCount { get; init; } = 1;

    /// <summary>Seconds each flight frame is shown. Aseprite exports 100ms/frame by default.</summary>
    public float ProjectileFrameDuration { get; init; } = 0.1f;

    /// <summary>True to loop the flight run; false to hold the last frame (e.g. a one-shot effect).</summary>
    public bool ProjectileLoop { get; init; } = true;

    public float ProjectileScale { get; init; } = 1f;
    public float Speed { get; init; }
    public float MaxRange { get; init; }

    /// <summary>
    /// Extra code-driven rotation in degrees/second, added on top of the flight frames.
    /// Keep 0 for an authored spin run so the art drives the motion.
    /// </summary>
    public float SpinDegreesPerSecond { get; init; }

    /// <summary>Spawn point offset in actor units, mirrored by facing.</summary>
    public Vector2 SpawnOffset { get; init; }

    private Texture2DRegion[]? _projectileFrames;

    /// <summary>Frame count clamped to at least 1 so the indexer below never divides by zero.</summary>
    public int SafeProjectileFrameCount => ProjectileFrameCount < 1 ? 1 : ProjectileFrameCount;

    protected override void ResetRegionCache()
    {
        base.ResetRegionCache();
        _projectileFrames = null;
    }

    /// <summary>
    /// Resolves the flight region for the run's <paramref name="flightFrameIndex"/>,
    /// wrapping modulo the run. Unlike the held overlay (which borrows the actor's
    /// animation frame), this index comes from the projectile's OWN timer
    /// (<c>ProjectileEntity.AdvanceFlightFrame</c>), so an authored spin/ignition run plays
    /// independently of the thrower. Regions are cached on first use so the per-frame Draw
    /// path is a single array index: the projectile never uses an <c>AnimatedSprite</c>, so
    /// it allocates nothing per spawn and avoids the <c>Controller</c>/<c>TextureRegion</c>
    /// pitfalls (see AGENTS.md).
    /// </summary>
    public Texture2DRegion? ResolveProjectileRegion(int flightFrameIndex)
    {
        if (Sheet is null) return null;

        var frames = _projectileFrames ??= BuildProjectileFrames();
        return frames.Length == 0 ? null : frames[flightFrameIndex % frames.Length];
    }

    private Texture2DRegion[] BuildProjectileFrames()
    {
        int count = SafeProjectileFrameCount;
        var frames = new Texture2DRegion[count];
        // Sheet is non-null: ResolveProjectileRegion returns before calling this.
        var atlas = Sheet!.TextureAtlas;
        for (int i = 0; i < count; i++)
            frames[i] = atlas[$"{ProjectileRegionPrefix}-{i:00}"];
        return frames;
    }
}
