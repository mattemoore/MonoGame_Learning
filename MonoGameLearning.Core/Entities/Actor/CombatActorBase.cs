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
    public string LayerName => "actors";
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));

    protected readonly SpriteRenderer SpriteRenderer;
    protected readonly Health HealthComponent;
    protected readonly AnimationFrameTracker FrameTracker = new();
    protected readonly AnimationSet Animations;
    protected readonly AudioService Audio;

    public CombatActorCallbacks Callbacks { get; }

    public CombatActorBase(string name, Vector2 position, int width, int height, AnimatedSprite sprite, float scale, int maxHealth, AnimationSet animations, AudioService audio)
        : base(name, position, width, height)
    {
        SpriteRenderer = new(sprite, scale);
        HealthComponent = new(maxHealth);
        Animations = animations;
        Audio = audio;

        Callbacks = new()
        {
            OnAttackingExit = AttackingExitImpl,
            OnHurtEntry = HurtEntryImpl,
            OnHurtExit = UnsubscribeFromAnimationEvent,
            OnKnockdownEntry = KnockdownEntryImpl,
            OnKnockdownExit = KnockdownExitImpl,
            OnDyingEntry = DyingEntryImpl,
            OnDyingExit = UnsubscribeFromAnimationEvent,
            OnDeadEntry = DeadEntryImpl,
        };
    }

    // Sprite is nullable only as a headless-test boundary: production always assigns a sprite, while
    // test doubles construct without one. Interior paths must never see null — `SpriteRequired` is the
    // single assert spot — and null is only tolerated at the entry guards this class intentionally keeps
    // (Update via EnsureSpriteAttached, PlayAnimation, UnsubscribeFromAnimationEvent,
    // AdvanceFrameAndRegisterHitboxes, TryHandleIncapacitatedUpdate, ResetActor), all using `Sprite is {} sprite`.
    public AnimatedSprite? Sprite => SpriteRenderer?.Sprite;

    protected AnimatedSprite SpriteRequired
    {
        get
        {
            Debug.Assert(Sprite is not null, $"{GetType().Name} [{Name}] has no Sprite assigned");
            return Sprite!;
        }
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

    int IDamageable.Health => HealthComponent.Value;
    int IDamageable.MaxHealth => HealthComponent.MaxHealth;
    bool IDamageable.IsAlive => HealthComponent.IsAlive;

    bool IDamageResponse.IsAlive => HealthComponent.IsAlive;
    bool IDamageResponse.CanTakeDamage() => CanTakeDamage();
    void IDamageResponse.ReduceHealth(int amount) => HealthComponent.Subtract(amount);
    void IDamageResponse.OnDeath() => OnDeath();
    void IDamageResponse.OnKnockdown(DamageInfo info) => OnKnockdown(info);
    void IDamageResponse.OnHit(DamageInfo info) => OnHit(info);

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    void IDamageable.Heal(int amount) => HealthComponent.Add(amount);

    protected void PlayAnimation(string key)
    {
        if (Sprite is not { } sprite) return;
        UnsubscribeFromAnimationEvent();
        sprite.SetAnimation(key);
        SubscribeToAnimationEvent();
    }

    private void SubscribeToAnimationEvent()
    {
        SpriteRequired.Controller.OnAnimationEvent += OnAnimationCompleted;
    }

    private void UnsubscribeFromAnimationEvent()
    {
        if (Sprite is not { } sprite) return;
        sprite.Controller.OnAnimationEvent -= OnAnimationCompleted;
    }

    protected void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;

        if (IsInKnockedDownState)
        {
            if (KnockdownPhase == KnockdownPhase.Falling)
            {
                SpriteRequired.SetAnimation(Animations.GetUp);
                KnockdownPhase = KnockdownPhase.GettingUp;
                SubscribeToAnimationEvent();
            }
            else
                FireKnockdownCompleted();
            return;
        }

        if (IsInHurtState) FireHurtCompleted();
        else if (IsInDyingState) FireDeathCompleted();
        else FireAttackCompleted();
    }

    public abstract void Update(GameTime gameTime);

    protected virtual bool CanTakeDamage() => HealthComponent.IsAlive;
    protected virtual void OnDeath() { }
    protected virtual void OnKnockdown(DamageInfo info) { }
    protected virtual void OnHit(DamageInfo info) { }

    protected void RaiseDied() => Died?.Invoke(this, EventArgs.Empty);

    void IAnimated.ResetAnimationFrameIndex() => FrameTracker.Reset();

    public void Render(RenderContext context)
    {
        var sprite = SpriteRequired;
        context.SpriteBatch.Draw(sprite, Position, 0f, new Vector2(SpriteRenderer.Scale));
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

        var (anchor, frame) = ResolveWeaponAnchorAndFrame(weapon, IsInAttackingState, FrameTracker.FrameIndex);
        var effect = WeaponFacingEffect(Direction);
        WeaponSprite.Effect = effect;
        WeaponSprite.Controller.SetFrame(frame);
        // SetFrame only updates the controller's internal frame index — it never refreshes
        // AnimatedSprite.TextureRegion. See AGENTS.md "MonoGame.Extended Pitfalls".
        if (weapon.Sheet is not null)
            WeaponSprite.TextureRegion = weapon.Sheet.TextureAtlas[WeaponSprite.Controller.CurrentFrame];

        var anchorOffset = ApplyWeaponFacing(anchor, Direction);
        var region = WeaponSprite.TextureRegion;
        var origin = new Vector2(region.Width / 2f, region.Height / 2f);
        var scale = new Vector2(SpriteRenderer.Scale);
        context.SpriteBatch.Draw(region,
            new Vector2(Position.X + anchorOffset.X * SpriteRenderer.Scale, Position.Y + anchorOffset.Y * SpriteRenderer.Scale),
            Color.White, 0f, origin, scale, effect, 0f);
    }

    internal static (Vector2 anchor, int frame) ResolveWeaponAnchorAndFrame(
        MeleeWeaponDef weapon, bool isAttacking, int frameIndex)
    {
        if (isAttacking && weapon.SwingAnchors.Length > 0)
        {
            int frame = Math.Clamp(frameIndex, 0, weapon.SwingAnchors.Length - 1);
            return (weapon.SwingAnchors[frame], frame);
        }
        return (weapon.CarryAnchor, 0);
    }

    internal static Vector2 ApplyWeaponFacing(Vector2 anchor, FacingDirection direction) =>
        direction == FacingDirection.Left ? new Vector2(-anchor.X, anchor.Y) : anchor;

    internal static SpriteEffects WeaponFacingEffect(FacingDirection direction) =>
        direction == FacingDirection.Left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

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
            var (anchor, frame) = ResolveWeaponAnchorAndFrame(EquippedWeapon, IsInAttackingState, FrameTracker.FrameIndex);
            var anchorScreen = Position + ApplyWeaponFacing(anchor, Direction);
            context.SpriteBatch.DrawRectangle(new RectangleF(anchorScreen.X - 2, anchorScreen.Y - 2, 4, 4), Color.Orange);
            var name = $"{EquippedWeapon.Name} f{frame}";
            var nameSize = context.Font.MeasureString(name);
            context.SpriteBatch.DrawString(context.Font, name,
                new Vector2(anchorScreen.X - nameSize.X / 2, anchorScreen.Y - nameSize.Y - 2), Color.White);
        }
    }

    protected void AdvanceFrameAndRegisterHitboxes(GameTime gameTime)
    {
        if (Sprite is not { } sprite) return;
        FrameTracker.AdvanceOnFrameChange(sprite, gameTime);

        if (CurrentMove is not null && FrameTracker.TryGetNewFrame(out var newFrameIndex))
        {
            HitboxService?.Clear(this);
            HitboxService?.RegisterFrameHitboxes(this, Faction, CurrentMove, newFrameIndex, Direction);
        }
    }

    // --- State abstractions ---
    protected abstract bool IsIncapacitated { get; }
    protected abstract bool IsInKnockedDownState { get; }
    protected abstract bool IsInHurtState { get; }
    protected abstract bool IsInDyingState { get; }
    protected abstract bool IsInAttackingState { get; }
    protected abstract void FireKnockdownCompleted();
    protected abstract void FireHurtCompleted();
    protected abstract void FireDeathCompleted();
    protected virtual void FireAttackCompleted() { }

    // --- Debug frame color ---
    protected virtual Color GetDebugFrameColor() => Color.Blue;

    // --- Knockdown phase ---
    protected KnockdownPhase KnockdownPhase { get; set; }

    // --- Sprite null guard ---
    protected virtual bool EnsureSpriteAttached()
    {
        Debug.Assert(Sprite is not null, $"{GetType().Name} [{Name}] has no Sprite assigned");
        return Sprite is not null;
    }

    // --- Shared state controller callbacks (cached delegates — zero alloc per use) ---
    private void AttackingExitImpl()
    {
        UnsubscribeFromAnimationEvent();
        CurrentMove = null;
        HitboxService?.Clear(this);
        HitboxService?.ClearAttackDedup(this);
    }

    private void HurtEntryImpl() => PlayAnimation(Animations.Hurt);

    private void KnockdownEntryImpl()
    {
        KnockdownPhase = KnockdownPhase.Falling;
        UnequipWeapon();
        PlayAnimation(Animations.Fall);
    }

    private void KnockdownExitImpl()
    {
        UnsubscribeFromAnimationEvent();
        KnockdownPhase = KnockdownPhase.Falling;
    }

    private void DyingEntryImpl()
    {
        UnequipWeapon();
        PlayAnimation(Animations.Die);
    }

    private void DeadEntryImpl() => RaiseDied();

    // --- Shared Update early-return ---
    protected bool TryHandleIncapacitatedUpdate(GameTime gameTime)
    {
        if (!IsIncapacitated) return false;
        MovementDirection = Vector2.Zero;
        if (Sprite is { } sprite)
            sprite.Update(gameTime);
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
        if (Sprite is { } sprite)
        {
            sprite.Effect = SpriteEffects.None;
            sprite.SetAnimation(Animations.Idle);
        }
        CurrentMove = null;
        FrameTracker.Reset();
        KnockdownPhase = KnockdownPhase.Falling;
        LastImpactSfx = null;
    }
}
