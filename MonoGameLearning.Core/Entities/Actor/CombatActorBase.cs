using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Animation;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Core.Entities.Actor;

public record struct AnimationSet(string Idle, string Run, string Hurt, string Fall, string Die, string GetUp);

public enum KnockdownPhase { Falling, GettingUp }

public abstract class CombatActorBase : Entity, IUpdatable, IRenderable, IDebugDrawable, ICollisionActor, ICollisionLayer, IDamageable, IDamageResponse, IHitboxProvider, IMoveable, IAnimated, IWeaponWielder
{
    public string LayerName => CollisionLayers.Actors;
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));

    public readonly SpriteRenderer SpriteRenderer;
    protected readonly Health HealthComponent;
    protected readonly AnimationFrameTracker FrameTracker = new();
    protected readonly AnimationSet Animations;
    protected readonly AudioService Audio;

    public CombatActorBase(string name, Vector2 position, int width, int height, AnimatedSprite sprite, float scale, int maxHealth, AnimationSet animations, AudioService audio)
        : base(name, position, width, height)
    {
        SpriteRenderer = new(sprite, scale);
        HealthComponent = new(maxHealth);
        Animations = animations;
        Audio = audio;
    }

    public RectangleF MovementBounds { get; set; }
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; }
    public HitboxService? HitboxService { get; set; }
    public MoveData? CurrentMove { get; set; }
    public FacingDirection Direction { get; set; } = FacingDirection.Right;
    public Faction Faction { get; protected set; }
    public event EventHandler? Died;
    protected SfxId? LastImpactSfx { get; set; }
    public WeaponDef? EquippedWeapon { get; private set; }

    public int Health => HealthComponent.Value;
    public int MaxHealth => HealthComponent.MaxHealth;
    public bool IsAlive => HealthComponent.IsAlive;
    public void Heal(int amount) => HealthComponent.Add(amount);

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    public void ReduceHealth(int amount) => HealthComponent.Subtract(amount);

    public virtual bool CanTakeDamage() => HealthComponent.IsAlive;
    public virtual void OnDeath() { }

    public void EquipWeapon(WeaponDef weapon) => EquippedWeapon = weapon;

    public void UnequipWeapon() => EquippedWeapon = null;

    protected void PlayAnimation(string key)
    {
        UnsubscribeFromAnimationEvent();
        SpriteRenderer.SetAnimation(key);
        SubscribeToAnimationEvent();
    }

    private void SubscribeToAnimationEvent()
    {
        SpriteRenderer.SubscribeAnimationEvents(OnAnimationCompleted);
    }

    private void UnsubscribeFromAnimationEvent()
    {
        SpriteRenderer.UnsubscribeAnimationEvents(OnAnimationCompleted);
    }

    protected void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;

        if (Phase == ActorPhase.KnockedDown)
        {
            if (KnockdownPhase == KnockdownPhase.Falling)
            {
                SpriteRenderer.SetAnimation(Animations.GetUp);
                KnockdownPhase = KnockdownPhase.GettingUp;
                SubscribeToAnimationEvent();
            }
            else
                FirePhaseCompleted();
            return;
        }

        FirePhaseCompleted();
    }

    public abstract void Update(GameTime gameTime);

    protected void RaiseDied() => Died?.Invoke(this, EventArgs.Empty);

    void IAnimated.ResetAnimationFrameIndex() => FrameTracker.Reset();

    public void Render(RenderContext context)
    {
        SpriteRenderer.Render(context.SpriteBatch, Position, 0f);
        RenderWeaponOverlay(context);
    }

    private void RenderWeaponOverlay(RenderContext context)
    {
        var weapon = EquippedWeapon;
        if (weapon is null) return;
        if (weapon.Sheet is null)
        {
            Debug.WriteLine($"{GetType().Name} [{Name}] armed with '{weapon.Name}' but no weapon sheet — Sheet not loaded?");
            return;
        }

        var (anchor, frame) = weapon.ResolveAnchorAndFrame(IsWeaponSwingActive, FrameTracker.FrameIndex);
        var region = weapon.ResolveRegion(IsWeaponSwingActive, frame);
        if (region is null)
        {
            Debug.Assert(false, $"{weapon.Name} weapon is missing SwingPrefix/CarryRegion region mapping");
            return;
        }

        var effect = WeaponDef.WeaponFacingEffect(Direction);
        var anchorOffset = WeaponDef.ApplyWeaponFacing(anchor, Direction);
        var origin = new Vector2(region.Width / 2f, region.Height / 2f);
        // The grip-on-hand invariant cancels only when the region half-size equals
        // weapon.FrameCenter (the handle offsets are expressed against it).
        Debug.Assert(
            MathF.Abs(region.Width / 2f - weapon.FrameCenter.X) < 0.5f &&
            MathF.Abs(region.Height / 2f - weapon.FrameCenter.Y) < 0.5f,
            $"{weapon.Name} region {region.Width}x{region.Height} does not match FrameCenter {weapon.FrameCenter} — the anchor formula assumes frame center = region size / 2");
        // Anchor positions in actor space (tracks the hand); only the region scales by
        // weapon.Scale, so the grip stays on the hand for any weapon scale.
        var drawScale = new Vector2(SpriteRenderer.Scale * weapon.Scale);
        context.SpriteBatch.Draw(region,
            new Vector2(Position.X + anchorOffset.X * SpriteRenderer.Scale, Position.Y + anchorOffset.Y * SpriteRenderer.Scale),
            Color.White, 0f, origin, drawScale, effect, 0f);
    }

    public virtual void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(Frame, GetDebugFrameColor());
        var text = HealthComponent.ToDisplayString();
        var size = context.Font.MeasureString(text);
        context.SpriteBatch.DrawString(context.Font, text,
            new Vector2(Frame.Center.X - size.X / 2, Frame.Top - size.Y - 2), Color.White);

        if (HitboxService is not null)
        {
            foreach (var actorBounds in HitboxService.GetActiveHitboxBounds(this))
                context.SpriteBatch.DrawRectangle(actorBounds, Color.Red);
        }

        if (EquippedWeapon is not null)
        {
            var weapon = EquippedWeapon;
            var (anchor, frame) = weapon.ResolveAnchorAndFrame(IsWeaponSwingActive, FrameTracker.FrameIndex);
            var scale = SpriteRenderer.Scale;
            // Region center the overlay draws the weapon at (orange marker + name).
            var anchorScreen = Position + WeaponDef.ApplyWeaponFacing(anchor, Direction) * scale;

            if (weapon.HasHandleOffsets)
            {
                // Grip point (green marker): the handle sits handleOffset - regionHalf from
                // the region's center. Shares the draw path's math exactly (anchor scaled by
                // actor scale, offset term by actor scale * weapon scale). Coincides with the
                // orange anchor center only when the grip offset is zero; against the drawn
                // bat it lands on the actor hand when the anchor formula holds.
                var region = weapon.ResolveRegion(IsWeaponSwingActive, frame);
                if (region is not null)
                {
                    var handleOffset = weapon.ResolveHandleOffset(IsWeaponSwingActive, frame);
                    var regionHalf = new Vector2(region.Width / 2f, region.Height / 2f);
                    var handleScreen = WeaponDef.ComputeHandleScreenPoint(
                        Position, Direction, anchor, handleOffset, regionHalf, scale, weapon.Scale);
                    // Off-sprite check in actor space: Frame is unscaled (48x60), so use the
                    // scale=1 handle point, not the scaled world marker.
                    var handScreen = WeaponDef.ComputeHandleScreenPoint(
                        Position, Direction, anchor, handleOffset, regionHalf, 1f, weapon.Scale);
                    var attachBounds = Frame;
                    bool outsideSprite =
                        handScreen.X < attachBounds.Left || handScreen.X > attachBounds.Right ||
                        handScreen.Y < attachBounds.Top || handScreen.Y > attachBounds.Bottom;
                    if (outsideSprite)
                        Debug.WriteLine($"{GetType().Name} [{Name}] {weapon.Name} handle at ({handScreen.X:F0},{handScreen.Y:F0}) is outside the actor sprite {attachBounds} — retune CarryHandAnchor/SwingHandAnchors so the grip sits on the sprite");
                    context.SpriteBatch.DrawRectangle(new RectangleF(handleScreen.X - 2, handleScreen.Y - 2, 4, 4), Color.Green);
                }
            }

            context.SpriteBatch.DrawRectangle(new RectangleF(anchorScreen.X - 2, anchorScreen.Y - 2, 4, 4), Color.Orange);
            var name = $"{weapon.Name} f{frame}";
            var nameSize = context.Font.MeasureString(name);
            context.SpriteBatch.DrawString(context.Font, name,
                new Vector2(anchorScreen.X - nameSize.X / 2, anchorScreen.Y - nameSize.Y - 2), Color.White);
        }
    }

    protected void AdvanceFrameAndRegisterHitboxes(GameTime gameTime)
    {
        SpriteRenderer.AdvanceFrame(FrameTracker, gameTime);

        if (CurrentMove is not null && FrameTracker.TryGetNewFrame(out var newFrameIndex))
        {
            OnFrameAdvanced(newFrameIndex);
            HitboxService?.Clear(this);
            HitboxService?.RegisterFrameHitboxes(this, Faction, CurrentMove, newFrameIndex, Direction);
        }
    }

    /// <summary>
    /// Frame-advance hook for subclasses (e.g. spawning a throwable projectile mid-swing).
    /// Called only when a move is active and the animation frame actually advances, before
    /// that frame's hitboxes are registered.
    /// </summary>
    protected virtual void OnFrameAdvanced(int frameIndex) { }

    // --- State abstractions ---
    protected abstract ActorPhase Phase { get; }
    protected abstract void FirePhaseCompleted();

    protected bool IsIncapacitated => Phase is ActorPhase.Dead or ActorPhase.Dying or ActorPhase.Hurt or ActorPhase.KnockedDown;
    protected bool IsInAttackingState => Phase == ActorPhase.Attacking;

    /// <summary>
    /// The weapon overlay plays its swing run only while the actor performs the weapon's own
    /// swing move. Attack2/Attack3 (and throwables on the carry pose) keep the held pose even
    /// though they are Attacking, so weapon art can't play over a non-weapon animation.
    /// </summary>
    protected bool IsWeaponSwingActive =>
        IsInAttackingState && EquippedWeapon is MeleeWeaponDef melee && ReferenceEquals(CurrentMove, melee.SwingMove);

    // --- Debug frame color ---
    protected virtual Color GetDebugFrameColor() => Color.Blue;

    // --- Knockdown phase ---
    protected KnockdownPhase KnockdownPhase { get; set; }

    // --- Shared state controller steps (single source of truth; subclasses call these and add audio) ---
    protected void AttackingExitImpl()
    {
        UnsubscribeFromAnimationEvent();
        ClearCombatState();
    }

    /// <summary>
    /// Clears per-life combat state (active move, hitboxes, attack dedup). Called on attack exit
    /// and by pool return so retired enemies cannot leak hitboxes back into HitboxService.
    /// </summary>
    public void ClearCombatState()
    {
        CurrentMove = null;
        HitboxService?.Clear(this);
        HitboxService?.ClearAttackDedup(this);
    }

    protected void HurtEntryImpl() => PlayAnimation(Animations.Hurt);

    protected void HurtExitImpl() => UnsubscribeFromAnimationEvent();

    protected void KnockdownEntryImpl()
    {
        KnockdownPhase = KnockdownPhase.Falling;
        UnequipWeapon();
        PlayAnimation(Animations.Fall);
    }

    protected void KnockdownExitImpl()
    {
        UnsubscribeFromAnimationEvent();
        KnockdownPhase = KnockdownPhase.Falling;
    }

    protected void DyingEntryImpl()
    {
        UnequipWeapon();
        PlayAnimation(Animations.Die);
    }

    protected void DyingExitImpl() => UnsubscribeFromAnimationEvent();

    protected void DeadEntryImpl() => RaiseDied();

    // --- Shared Update early-return ---
    protected bool TryHandleIncapacitatedUpdate(GameTime gameTime)
    {
        if (!IsIncapacitated) return false;
        MovementDirection = Vector2.Zero;
        SpriteRenderer.Update(gameTime);
        return true;
    }

    // --- Shared Reset common parts ---
    protected void ResetActor(Vector2 position)
    {
        Position = position;
        HealthComponent.SetToMax();
        MovementDirection = Vector2.Zero;
        Direction = FacingDirection.Right;
        UnequipWeapon();
        SpriteRenderer.SetEffect(SpriteEffects.None);
        SpriteRenderer.SetAnimation(Animations.Idle);
        CurrentMove = null;
        FrameTracker.Reset();
        KnockdownPhase = KnockdownPhase.Falling;
        LastImpactSfx = null;
    }
}
