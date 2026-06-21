using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Enemy;

public class EnemyEntity : ActorEntity, ICombatant
{
    private readonly EnemyStateController _stateController;
    private int _knockdownPhase;
    private float _attackCooldown;
    private float _attackDelayTimer;
    private float _lastFacingX;
    private float _directionUpdateTimer;
    private const float AttackDelayDuration = 1.0f;
    private const float DirectionUpdateInterval = 0.35f;
    public bool IsAlive => Health > 0;
    public event EventHandler Died;
    public ActorEntity Target { get; set; }
    public float AttackRange { get; set; } = 70f;
    public float MinChaseDistance { get; set; } = 60f;

    public EnemyEntity(string name,
                       Vector2 position,
                       float scale,
                       AnimatedSprite sprite) : base(name, position, scale, sprite)
    {
        MaxHealth = 30;
        Health = MaxHealth;
        Speed = 120f;
        Sprite.Color = Color.Red;
        Width = 48;
        Height = 60;
        _stateController = CreateStateController();
    }

    private void SubscribeToAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;

    private void UnsubscribeFromAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted;

    private EnemyStateController CreateStateController() => new(new()
    {
        OnIdleEntry = () => Sprite.SetAnimation(EnemySprite.AnimationIdle),
        OnChasingEntry = () => Sprite.SetAnimation(EnemySprite.AnimationRun),
        OnAttackingEntry = () =>
        {
            Sprite.SetAnimation(EnemySprite.AnimationAttack1);
            CurrentMove = EnemyMoves.All[EnemySprite.AnimationAttack1];
            ResetAnimationFrameIndex();
            SubscribeToAnimationEvent();
        },
        OnAttackingExit = () =>
        {
            UnsubscribeFromAnimationEvent();
            CurrentMove = null;
            HitboxService?.Clear(this);
        },
        OnHurtEntry = () =>
        {
            Sprite.SetAnimation(EnemySprite.AnimationHurt);
            SubscribeToAnimationEvent();
        },
        OnHurtExit = UnsubscribeFromAnimationEvent,
        OnKnockdownEntry = () =>
        {
            Sprite.SetAnimation(EnemySprite.AnimationFall);
            _knockdownPhase = 0;
            SubscribeToAnimationEvent();
        },
        OnKnockdownExit = () =>
        {
            UnsubscribeFromAnimationEvent();
            _knockdownPhase = 0;
        },
        OnDyingEntry = () =>
        {
            Sprite.SetAnimation(EnemySprite.AnimationDie);
            SubscribeToAnimationEvent();
        },
        OnDyingExit = UnsubscribeFromAnimationEvent,
        OnDeadEntry = () => Died?.Invoke(this, EventArgs.Empty)
    });

    public override void Update(GameTime gameTime)
    {
        if (_stateController.State is EnemyState.Dead or EnemyState.Dying or EnemyState.Hurt or EnemyState.KnockedDown)
        {
            MovementDirection = Vector2.Zero;
            base.Update(gameTime);
            return;
        }

        if (Target is not null)
        {
            Vector2 toTarget = Target.Position - Position;
            float distance = toTarget.Length();

            if (distance <= AttackRange && _stateController.State is EnemyState.Idle or EnemyState.Chasing && _attackCooldown <= 0)
            {
                if (_stateController.State == EnemyState.Chasing)
                {
                    _stateController.Fire(EnemyTrigger.StopChase);
                    MovementDirection = Vector2.Zero;
                }

                if (_attackDelayTimer <= 0)
                    _attackDelayTimer = AttackDelayDuration;

                _attackDelayTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackDelayTimer <= 0)
                {
                    _attackDelayTimer = 0;
                    _stateController.Fire(EnemyTrigger.AttackStart);
                }
            }
            else if (distance > AttackRange && _stateController.State is EnemyState.Idle or EnemyState.Chasing)
            {
                _attackDelayTimer = 0;
                _stateController.Fire(EnemyTrigger.StartChase);
                _directionUpdateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_directionUpdateTimer <= 0)
                {
                    _directionUpdateTimer = DirectionUpdateInterval;
                    toTarget /= distance;
                    MovementDirection = PreventDiagonal(toTarget);
                    if (Math.Sign(MovementDirection.X) != Math.Sign(_lastFacingX))
                    {
                        _lastFacingX = MovementDirection.X;
                        UpdateFacingDirection(MovementDirection);
                    }
                }

                if (distance <= MinChaseDistance)
                    MovementDirection = Vector2.Zero;
            }
            else if (_stateController.State == EnemyState.Chasing && distance <= AttackRange)
            {
                _stateController.Fire(EnemyTrigger.StopChase);
                MovementDirection = Vector2.Zero;
            }
        }

        _attackCooldown = Math.Max(0, _attackCooldown - (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_stateController.State == EnemyState.Chasing && MovementDirection != Vector2.Zero)
            Position += MovementDirection * (float)gameTime.ElapsedGameTime.TotalSeconds * Speed;

        base.Update(gameTime);
    }

    private static Vector2 PreventDiagonal(Vector2 direction) =>
        Math.Abs(direction.X) > Math.Abs(direction.Y)
            ? new Vector2(direction.X, 0)
            : new Vector2(0, direction.Y);

    public override void TakeDamage(int amount, bool knockdown = false)
    {
        if (!IsAlive || _stateController.State == EnemyState.KnockedDown) return;

        Health = Math.Max(0, Health - amount);

        if (Health <= 0)
        {
            _stateController.Fire(EnemyTrigger.Die);
        }
        else if (knockdown)
        {
            _stateController.Fire(EnemyTrigger.TakeKnockdown);
        }
        else
        {
            _stateController.Fire(EnemyTrigger.TakeDamage);
        }
    }

    private void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;

        if (_stateController.State == EnemyState.KnockedDown)
        {
            if (_knockdownPhase == 0)
            {
                Sprite.SetAnimation(EnemySprite.AnimationGetUp);
                _knockdownPhase = 1;
                SubscribeToAnimationEvent();
            }
            else
            {
                _stateController.Fire(EnemyTrigger.KnockdownCompleted);
            }
            return;
        }

        _stateController.Fire(_stateController.State switch
        {
            EnemyState.Hurt => EnemyTrigger.HurtCompleted,
            EnemyState.Dying => EnemyTrigger.DeathCompleted,
            _ => SetCooldownAndAttackComplete()
        });
    }

    private EnemyTrigger SetCooldownAndAttackComplete()
    {
        _attackCooldown = 1.5f;
        return EnemyTrigger.AttackCompleted;
    }

    public void Reset(Vector2 position, ActorEntity target)
    {
        Position = position;
        Target = target;
        Health = MaxHealth;
        _attackCooldown = 0;
        _attackDelayTimer = 0;
        _directionUpdateTimer = 0;
        _lastFacingX = 0;
        _knockdownPhase = 0;
        MovementDirection = Vector2.Zero;
        Direction = FacingDirection.Right;
        Sprite.Effect = SpriteEffects.None;
        Sprite.SetAnimation(EnemySprite.AnimationIdle);
        CurrentMove = null;
        ResetAnimationFrameIndex();
        Sprite.Color = Color.Red;
    }
}