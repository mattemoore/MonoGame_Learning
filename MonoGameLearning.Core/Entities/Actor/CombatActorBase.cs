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

public abstract class CombatActorBase(
    string name, Vector2 position, int width, int height, AnimatedSprite sprite, float scale, int maxHealth,
    AnimationSet animations, AudioService audio)
    : Entity(name, position, width, height), IUpdatable, IRenderable, IDebugDrawable, ICollisionActor, ICollisionLayer, IDamageable, IDamageResponse, IHitboxProvider, IMoveable, IAnimated, IWeaponWielder
{
    public string LayerName => CollisionLayers.Actors;
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));

    public readonly SpriteRenderer SpriteRenderer = new(sprite, scale);
    protected readonly Health HealthComponent = new(maxHealth);
    protected readonly AnimationFrameTracker FrameTracker = new();
    protected readonly AnimationSet Animations = animations;
    protected readonly AudioService Audio = audio;

    private string? _lastWarnedHandKey;

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

        var actorFrame = SpriteRenderer.AnimationFrame;
        // Swing moves do not loop, so a pose index past the run means the frame clock is not
        // animation-relative (e.g. a raw atlas region index) — fail loudly in Debug.
        if (IsWeaponSwingActive && EquippedWeapon is MeleeWeaponDef melee)
            Debug.Assert(actorFrame < melee.SwingHandleOffsets.Length,
                $"{melee.Name}: swing pose index {actorFrame} exceeds the {melee.SwingHandleOffsets.Length}-frame swing run");
        // The actor owns the hand point; the weapon only supplies its own grip offset, so
        // the same hand data drives every weapon it holds.
        var anchor = WeaponDef.ComputeAnchor(
            ResolveHandAnchor(), weapon.ResolveHandleOffset(IsWeaponSwingActive, actorFrame),
            weapon.FrameCenter, weapon.Scale);
        var region = weapon.ResolveRegion(IsWeaponSwingActive, actorFrame);
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

    /// <summary>
    /// The actor's per-animation hand points, or <c>null</c> when the actor has no authored
    /// hand data (the overlay then falls back to <see cref="Vector2.Zero"/>).
    /// </summary>
    protected virtual HandAnchorTable? HandAnchors => null;

    /// <summary>
    /// The hand point for the current animation frame, in actor units relative to
    /// <c>Position</c>. Falls back to <see cref="Vector2.Zero"/> and warns once per animation
    /// when the table has no entry, so authoring can lag the code.
    /// </summary>
    protected Vector2 ResolveHandAnchor()
    {
        var key = SpriteRenderer.CurrentAnimationKey;
        if (HandAnchors is { } table && table.TryResolve(key, SpriteRenderer.AnimationFrame, out var hand))
            return hand;

        if (key != _lastWarnedHandKey)
        {
            _lastWarnedHandKey = key;
            Debug.WriteLine($"{GetType().Name} [{Name}] no hand anchor for '{key}' — actor hand slice/table missing or stale");
        }
        return Vector2.Zero;
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
            var scale = SpriteRenderer.Scale;
            var actorFrame = SpriteRenderer.AnimationFrame;
            var isSwing = IsWeaponSwingActive;
            var hand = ResolveHandAnchor();
            var handleOffset = weapon.ResolveHandleOffset(isSwing, actorFrame);
            var anchor = WeaponDef.ComputeAnchor(hand, handleOffset, weapon.FrameCenter, weapon.Scale);
            var region = weapon.ResolveRegion(isSwing, actorFrame);

            // Hand point (cyan): the actor-side point the weapon must attach to.
            var handScreen = Position + WeaponDef.ApplyWeaponFacing(hand, Direction) * scale;
            context.SpriteBatch.DrawRectangle(new RectangleF(handScreen.X - 2, handScreen.Y - 2, 4, 4), Color.Cyan);

            // Region center the overlay draws the weapon at (orange marker + name).
            var anchorScreen = Position + WeaponDef.ApplyWeaponFacing(anchor, Direction) * scale;
            context.SpriteBatch.DrawRectangle(new RectangleF(anchorScreen.X - 2, anchorScreen.Y - 2, 4, 4), Color.Orange);
            var name = $"{weapon.Name} f{actorFrame}";
            var nameSize = context.Font.MeasureString(name);
            context.SpriteBatch.DrawString(context.Font, name,
                new Vector2(anchorScreen.X - nameSize.X / 2, anchorScreen.Y - nameSize.Y - 2), Color.White);

            if (region is null) return;

            var regionHalf = new Vector2(region.Width / 2f, region.Height / 2f);
            if (weapon.HasHandleOffsets)
            {
                // Grip point (green marker): shares the draw path's math exactly (anchor
                // scaled by actor scale, offset term by actor scale * weapon scale). With
                // regionHalf == FrameCenter it lands on the cyan hand point when both the
                // actor hand table and the weapon handle offsets are correct.
                var gripScreen = WeaponDef.ComputeHandleScreenPoint(
                    Position, Direction, anchor, handleOffset, regionHalf, scale, weapon.Scale);
                context.SpriteBatch.DrawRectangle(new RectangleF(gripScreen.X - 2, gripScreen.Y - 2, 4, 4), Color.Green);
            }

            // Off-sprite check in actor space: the sprite is drawn centered at Position with
            // the region's unscaled half extents, so compare the unscaled hand point.
            var handUnscaled = Position + WeaponDef.ApplyWeaponFacing(hand, Direction);
            bool outsideSprite =
                MathF.Abs(handUnscaled.X - Position.X) > regionHalf.X ||
                MathF.Abs(handUnscaled.Y - Position.Y) > regionHalf.Y;
            if (outsideSprite)
                Debug.WriteLine($"{GetType().Name} [{Name}] {weapon.Name} hand at ({handUnscaled.X:F0},{handUnscaled.Y:F0}) is outside the {region.Width}x{region.Height} sprite — retune the actor hand table for '{SpriteRenderer.CurrentAnimationKey}'");
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
