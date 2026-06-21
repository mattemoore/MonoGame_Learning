using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class CombatActorBase : Entity, IUpdatable, IRenderable, IDebugDrawable, ICollisionActor, ICombatant, IHitboxProvider, IMoveableEntity, IAnimated
{
    public string LayerName => "actors";
    public IShapeF Bounds => Frame;

    protected readonly SpriteRenderer SpriteRenderer;
    protected readonly Health HealthComponent;
    protected readonly AnimationFrameTracker FrameTracker = new();

    public AnimatedSprite Sprite => SpriteRenderer.Sprite;
    public RectangleF MovementBounds { get; set; }
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; }
    public HitboxService HitboxService { get; set; }
    public MoveData CurrentMove { get; set; }
    public FacingDirection Direction { get; set; } = FacingDirection.Right;
    public Faction Faction { get; protected set; }
    public event EventHandler Died;

    int IHasHealth.Health => HealthComponent.Value;
    int IHasHealth.MaxHealth => HealthComponent.MaxHealth;
    bool ICombatant.IsAlive => HealthComponent.IsAlive;

    protected CombatActorBase(string name, Vector2 position, int width, int height, AnimatedSprite sprite, float scale, int maxHealth)
        : base(name, position, width, height)
    {
        SpriteRenderer = new(sprite, scale);
        HealthComponent = new(maxHealth);
    }

    protected void SubscribeToAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;

    protected void UnsubscribeFromAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted;

    protected void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;

        if (IsInKnockedDownState)
        {
            if (KnockdownPhase == 0)
            {
                Sprite.SetAnimation(GetUpAnimation);
                KnockdownPhase = 1;
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

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    bool ICombatant.CanTakeDamage() => CanTakeDamage();
    void ICombatant.ReduceHealth(int amount) => HealthComponent.Subtract(amount);
    void ICombatant.OnDeath() => OnDeath();
    void ICombatant.OnKnockdown(DamageInfo info) => OnKnockdown(info);
    void ICombatant.OnHit(DamageInfo info) => OnHit(info);

    protected virtual bool CanTakeDamage() => HealthComponent.IsAlive;
    protected virtual void OnDeath() { }
    protected virtual void OnKnockdown(DamageInfo info) { }
    protected virtual void OnHit(DamageInfo info) { }

    protected void RaiseDied() => Died?.Invoke(this, EventArgs.Empty);

    public void OnCollision(CollisionEventArgs collisionInfo)
    {
        if (collisionInfo.Other is IMoveableEntity) return;
        Position -= collisionInfo.PenetrationVector;
    }

    void IAnimated.ResetAnimationFrameIndex() => FrameTracker.Reset();

    public void Render(RenderContext context)
    {
        if (Sprite is null) return;
        context.SpriteBatch.Draw(Sprite, Position, MathHelper.ToRadians(Rotation), new Vector2(SpriteRenderer.Scale));
    }

    public void DrawDebug(DebugDrawContext context)
    {
        context.SpriteBatch.DrawRectangle(Frame, Color.AntiqueWhite);
        context.SpriteBatch.DrawRectangle(Frame, Color.Blue);
        HealthDisplay.Draw(context.SpriteBatch, context.Font, Frame, HealthComponent.Value, HealthComponent.MaxHealth);

        if (HitboxService is not null)
        {
            foreach (var bounds in HitboxService.GetActiveHitboxBounds(this))
                context.SpriteBatch.DrawRectangle(bounds, Color.Red);
        }
    }

    protected void AdvanceFrameAndRegisterHitboxes(GameTime gameTime)
    {
        Debug.Assert(Sprite is not null, $"CombatActorBase [{Name}] has no Sprite assigned");
        FrameTracker.AdvanceOnFrameChange(Sprite, gameTime);

        if (CurrentMove is not null && FrameTracker.TryGetNewFrame(out var newFrameIndex))
        {
            HitboxService?.Clear(this);
            HitboxService?.RegisterFrameHitboxes(this, CurrentMove, newFrameIndex, Direction);
        }
    }

    // --- Animation key abstractions ---
    protected abstract string IdleAnimation { get; }
    protected abstract string RunAnimation { get; }
    protected abstract string HurtAnimation { get; }
    protected abstract string FallAnimation { get; }
    protected abstract string DieAnimation { get; }
    protected abstract string GetUpAnimation { get; }

    // --- State abstractions ---
    protected abstract bool IsIncapacitated { get; }
    protected abstract bool IsInKnockedDownState { get; }
    protected abstract bool IsInHurtState { get; }
    protected abstract bool IsInDyingState { get; }
    protected abstract void FireKnockdownCompleted();
    protected abstract void FireHurtCompleted();
    protected abstract void FireDeathCompleted();
    protected virtual void FireAttackCompleted() { }

    // --- Knockdown phase ---
    protected int KnockdownPhase { get; set; }

    // --- Sprite null guard ---
    protected bool EnsureSpriteAttached()
    {
        Debug.Assert(Sprite is not null, $"{GetType().Name} [{Name}] has no Sprite assigned");
        return Sprite is not null;
    }

    // --- Shared state controller callbacks ---
    protected Action AttackingExit() => () => { UnsubscribeFromAnimationEvent(); CurrentMove = null; HitboxService?.Clear(this); };
    protected Action HurtEntry() => () => { Sprite.SetAnimation(HurtAnimation); SubscribeToAnimationEvent(); };
    protected Action HurtExit() => UnsubscribeFromAnimationEvent;
    protected Action KnockdownEntry() => () => { Sprite.SetAnimation(FallAnimation); KnockdownPhase = 0; SubscribeToAnimationEvent(); };
    protected Action KnockdownExit() => () => { UnsubscribeFromAnimationEvent(); KnockdownPhase = 0; };
    protected Action DyingEntry() => () => { Sprite.SetAnimation(DieAnimation); SubscribeToAnimationEvent(); };
    protected Action DyingExit() => UnsubscribeFromAnimationEvent;
    protected Action DeadEntry() => () => RaiseDied();

    // --- Shared Update early-return ---
    protected bool TryHandleIncapacitatedUpdate(GameTime gameTime)
    {
        if (!IsIncapacitated) return false;
        MovementDirection = Vector2.Zero;
        Sprite.Update(gameTime);
        return true;
    }

    // --- Shared Reset common parts ---
    protected void ResetActor(Vector2 position)
    {
        Position = position;
        HealthComponent.SetToMax();
        MovementDirection = Vector2.Zero;
        Direction = FacingDirection.Right;
        Sprite.Effect = SpriteEffects.None;
        Sprite.SetAnimation(IdleAnimation);
        CurrentMove = null;
        FrameTracker.Reset();
        KnockdownPhase = 0;
    }
}