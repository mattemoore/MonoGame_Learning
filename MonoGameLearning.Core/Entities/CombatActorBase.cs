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

public record struct AnimationSet(string Idle, string Run, string Hurt, string Fall, string Die, string GetUp);

public enum KnockdownPhase { Falling, GettingUp }

public abstract class CombatActorBase(string name, Vector2 position, int width, int height, AnimatedSprite sprite, float scale, int maxHealth, AnimationSet animations) : Entity(name, position, width, height), IUpdatable, IRenderable, IDebugDrawable, ICollisionActor, IDamageable, IHitboxProvider, IMoveableEntity, IAnimated
{
    public string LayerName => "actors";
    public IShapeF Bounds => Frame;

    protected readonly SpriteRenderer SpriteRenderer = new(sprite, scale);
    protected readonly Health HealthComponent = new(maxHealth);
    protected readonly AnimationFrameTracker FrameTracker = new();
    protected readonly AnimationSet Animations = animations;

    public AnimatedSprite Sprite => SpriteRenderer.Sprite;
    public RectangleF MovementBounds { get; set; }
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; }
    public HitboxService HitboxService { get; set; }
    public MoveData CurrentMove { get; set; }
    public FacingDirection Direction { get; set; } = FacingDirection.Right;
    public Faction Faction { get; protected set; }
    public event EventHandler Died;

    int IDamageable.Health => HealthComponent.Value;
    int IDamageable.MaxHealth => HealthComponent.MaxHealth;
    bool IDamageable.IsAlive => HealthComponent.IsAlive;

    protected void PlayAnimation(string key)
    {
        UnsubscribeFromAnimationEvent();
        Sprite.SetAnimation(key);
        SubscribeToAnimationEvent();
    }

    private void SubscribeToAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;

    private void UnsubscribeFromAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted;

    protected void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;

        if (IsInKnockedDownState)
        {
            if (KnockdownPhase == KnockdownPhase.Falling)
            {
                Sprite.SetAnimation(Animations.GetUp);
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

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    bool IDamageable.CanTakeDamage() => CanTakeDamage();
    void IDamageable.ReduceHealth(int amount) => HealthComponent.Subtract(amount);
    void IDamageable.OnDeath() => OnDeath();
    void IDamageable.OnKnockdown(DamageInfo info) => OnKnockdown(info);
    void IDamageable.OnHit(DamageInfo info) => OnHit(info);

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
        context.SpriteBatch.Draw(Sprite, Position, 0f, new Vector2(SpriteRenderer.Scale));
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
    protected KnockdownPhase KnockdownPhase { get; set; }

    // --- Sprite null guard ---
    protected bool EnsureSpriteAttached()
    {
        Debug.Assert(Sprite is not null, $"{GetType().Name} [{Name}] has no Sprite assigned");
        return Sprite is not null;
    }

    // --- Shared state controller callbacks ---
    protected Action AttackingExit() => () => { UnsubscribeFromAnimationEvent(); CurrentMove = null; HitboxService?.Clear(this); HitboxService?.ClearAttackDedup(this); };
    protected Action HurtEntry() => () => PlayAnimation(Animations.Hurt);
    protected Action HurtExit() => UnsubscribeFromAnimationEvent;
    protected Action KnockdownEntry() => () => { KnockdownPhase = KnockdownPhase.Falling; PlayAnimation(Animations.Fall); };
    protected Action KnockdownExit() => () => { UnsubscribeFromAnimationEvent(); KnockdownPhase = KnockdownPhase.Falling; };
    protected Action DyingEntry() => () => PlayAnimation(Animations.Die);
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
        Sprite.SetAnimation(Animations.Idle);
        CurrentMove = null;
        FrameTracker.Reset();
        KnockdownPhase = KnockdownPhase.Falling;
    }
}