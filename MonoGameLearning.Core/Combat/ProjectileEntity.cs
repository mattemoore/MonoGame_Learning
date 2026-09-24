using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Combat;

/// <summary>
/// A pooled projectile launched by a <see cref="ThrowableWeaponDef"/>. Once per frame it
/// re-registers a single fixed hitbox at its own position (clearing the previous frame's),
/// so <see cref="HitboxService.ResolveHits"/> reports hits with <c>Source == this</c>; the
/// owner marks it <see cref="Expired"/> and <see cref="ProjectileService"/> recycles it.
/// It also owns its flight animation clock (<see cref="AdvanceFlightFrame"/>): unlike the
/// held weapon overlay, which borrows the actor's animation frame, the projectile advances
/// its own region run on <see cref="ThrowableWeaponDef.ProjectileFrameDuration"/>.
/// <para>
/// The run is a plain timer over the def's cached region array — deliberately NOT an
/// <c>AnimatedSprite</c>/<c>AnimationController</c>: a pooled projectile must not allocate
/// per spawn, and driving an <c>AnimatedSprite</c> would reintroduce MonoGame.Extended's
/// <c>SetAnimation</c>-replaces-<c>Controller</c> and <c>SetFrame</c>-doesn't-refresh-
/// <c>TextureRegion</c> pitfalls (see AGENTS.md "MonoGame.Extended Pitfalls"). Indexing
/// the cached array keeps the per-frame Draw path allocation-free with no sync step.
/// </para>
/// </summary>
public sealed class ProjectileEntity : Entity, IUpdatable, IRenderable, IDebugDrawable, IHitboxProvider
{
    private ThrowableWeaponDef? _def;
    private Vector2 _origin;
    private Faction _faction;
    private float _spin;

    // Flight-run clock owned by the projectile, never by the thrower's animation.
    private int _frameIndex;
    private float _frameTimer;

    public ProjectileEntity() : base("projectile", Vector2.Zero, 16, 16) { }

    public bool Expired { get; private set; }

    /// <summary>Current frame of the flight run (see <see cref="ThrowableWeaponDef.ProjectileFrameCount"/>).</summary>
    public int FlightFrameIndex => _frameIndex;

    public MoveData? CurrentMove { get; set; }
    public HitboxService? HitboxService { get; set; }
    public FacingDirection Direction { get; set; }

    public void Launch(ThrowableWeaponDef def, Vector2 origin, FacingDirection facing, Faction faction)
    {
        _def = def;
        _origin = origin;
        _faction = faction;
        Direction = facing;
        _spin = 0f;
        _frameIndex = 0;
        _frameTimer = 0f;
        Expired = false;
        Position = origin;
    }

    /// <summary>Marks the projectile spent because its hitbox scored (see <c>DamageInfo.Source</c>).</summary>
    public void MarkHit() => Expired = true;

    public void Update(GameTime gameTime)
    {
        if (Expired || _def is null) return;

        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var direction = Direction == FacingDirection.Left ? new Vector2(-1, 0) : new Vector2(1, 0);
        Position += direction * _def.Speed * deltaSeconds;
        _spin += _def.SpinDegreesPerSecond * deltaSeconds;
        AdvanceFlightFrame(_def, deltaSeconds);

        if (Vector2.Distance(_origin, Position) >= _def.MaxRange)
        {
            Expired = true;
            return;
        }

        HitboxService?.Clear(this);
        HitboxService?.RegisterHitbox(this, Position, _faction, _def.ProjectileHitbox,
            _def.Damage, _def.Knockdown, _def.Strength, _def.ImpactSfx, Direction);
    }

    /// <summary>
    /// Advances the flight run by elapsed time: a zero-alloc stand-in for an
    /// <c>AnimationController</c>. A pooled projectile cannot own an <c>AnimatedSprite</c>
    /// without per-spawn allocation, and <c>AnimatedSprite.SetFrame</c> would not refresh
    /// <c>TextureRegion</c>; the def's cached array makes <see cref="Render"/> a single
    /// index instead. Wraps when <see cref="ThrowableWeaponDef.ProjectileLoop"/> is set,
    /// otherwise holds the final frame.
    /// </summary>
    private void AdvanceFlightFrame(ThrowableWeaponDef def, float deltaSeconds)
    {
        int frameCount = def.SafeProjectileFrameCount;
        float duration = def.ProjectileFrameDuration;
        if (frameCount <= 1 || duration <= 0f) return;

        _frameTimer += deltaSeconds;
        while (_frameTimer >= duration)
        {
            _frameTimer -= duration;
            _frameIndex = _frameIndex + 1 >= frameCount
                ? (def.ProjectileLoop ? 0 : frameCount - 1)
                : _frameIndex + 1;
        }
    }

    public void Render(RenderContext context)
    {
        // Expired projectiles stay registered until ProcessPending; skip the ghost frame.
        if (Expired || _def is null) return;

        var region = _def.ResolveProjectileRegion(_frameIndex);
        if (region is null) return;

        var origin = new Vector2(region.Width / 2f, region.Height / 2f);
        context.SpriteBatch.Draw(region, Position, Color.White, MathHelper.ToRadians(_spin),
            origin, new Vector2(_def.ProjectileScale), WeaponDef.WeaponFacingEffect(Direction), 0f);
    }

    public void DrawDebug(DebugDrawContext context)
    {
        if (Expired) return;
        context.SpriteBatch.DrawRectangle(Frame, Color.Orange);
    }
}
