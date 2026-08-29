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
    public MeleeWeaponDef? EquippedWeapon { get; private set; }
    protected AnimatedSprite? WeaponSprite { get; private set; }

    public int Health => HealthComponent.Value;
    public int MaxHealth => HealthComponent.MaxHealth;
    public bool IsAlive => HealthComponent.IsAlive;
    public void Heal(int amount) => HealthComponent.Add(amount);

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    public void ReduceHealth(int amount) => HealthComponent.Subtract(amount);

    public virtual bool CanTakeDamage() => HealthComponent.IsAlive;
    public virtual void OnDeath() { }
    public virtual void OnKnockdown(DamageInfo info) { }
    public virtual void OnHit(DamageInfo info) { }

    public void EquipWeapon(MeleeWeaponDef weapon)
    {
        EquippedWeapon = weapon;
        WeaponSprite = weapon.CreateSprite();
    }

    public void UnequipWeapon()
    {
        EquippedWeapon = null;
        WeaponSprite = null;
    }

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
        if (WeaponSprite is null)
        {
            Debug.WriteLine($"{GetType().Name} [{Name}] armed with '{weapon.Name}' but no weapon sprite — Sheet not loaded?");
            return;
        }

        var (anchor, frame) = MeleeWeaponDef.ResolveWeaponAnchorAndFrame(weapon, IsInAttackingState, FrameTracker.FrameIndex);
        var effect = MeleeWeaponDef.WeaponFacingEffect(Direction);
        WeaponSprite.Effect = effect;
        WeaponSprite.Controller.SetFrame(frame);
        // SetFrame only updates the controller's internal frame index — it never refreshes
        // AnimatedSprite.TextureRegion. See AGENTS.md "MonoGame.Extended Pitfalls".
        if (weapon.Sheet is not null)
            WeaponSprite.TextureRegion = weapon.Sheet.TextureAtlas[WeaponSprite.Controller.CurrentFrame];

        var anchorOffset = MeleeWeaponDef.ApplyWeaponFacing(anchor, Direction);
        var region = WeaponSprite.TextureRegion;
        var origin = new Vector2(region.Width / 2f, region.Height / 2f);
        var scale = new Vector2(SpriteRenderer.Scale);
        context.SpriteBatch.Draw(region,
            new Vector2(Position.X + anchorOffset.X * SpriteRenderer.Scale, Position.Y + anchorOffset.Y * SpriteRenderer.Scale),
            Color.White, 0f, origin, scale, effect, 0f);
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
            var (anchor, frame) = MeleeWeaponDef.ResolveWeaponAnchorAndFrame(EquippedWeapon, IsInAttackingState, FrameTracker.FrameIndex);
            var anchorScreen = Position + MeleeWeaponDef.ApplyWeaponFacing(anchor, Direction);
            context.SpriteBatch.DrawRectangle(new RectangleF(anchorScreen.X - 2, anchorScreen.Y - 2, 4, 4), Color.Orange);
            var name = $"{EquippedWeapon.Name} f{frame}";
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
            HitboxService?.Clear(this);
            HitboxService?.RegisterFrameHitboxes(this, Faction, CurrentMove, newFrameIndex, Direction);
        }
    }

    // --- State abstractions ---
    protected abstract ActorPhase Phase { get; }
    protected abstract void FirePhaseCompleted();

    protected bool IsIncapacitated => Phase is ActorPhase.Dead or ActorPhase.Dying or ActorPhase.Hurt or ActorPhase.KnockedDown;
    protected bool IsInAttackingState => Phase == ActorPhase.Attacking;

    // --- Debug frame color ---
    protected virtual Color GetDebugFrameColor() => Color.Blue;

    // --- Knockdown phase ---
    protected KnockdownPhase KnockdownPhase { get; set; }

    // --- Shared state controller steps (single source of truth; subclasses call these and add audio) ---
    protected void AttackingExitImpl()
    {
        UnsubscribeFromAnimationEvent();
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
