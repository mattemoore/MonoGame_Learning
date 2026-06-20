using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Player;

public enum AttackType { Attack1, Attack2, Attack3 }

public class PlayerEntity : ActorEntity, ICombatant
{
    private PlayerStateController _stateController;
    private int _knockdownPhase;
    private float _invincibilityTimer;
    public int Health { get; private set; }
    public int MaxHealth { get; } = 100;
    public bool IsAlive => Health > 0;
    public event EventHandler Died;
    private AttackType _pendingAttackType;

    public PlayerEntity(string name,
                        Vector2 position,
                        float scale,
                        AnimatedSprite sprite) : base(name, position, scale, sprite)
    {
        Health = MaxHealth;
        Width = 48;
        Height = 60;
        _stateController = CreateStateController();
    }

    private void SubscribeToAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;

    private void UnsubscribeFromAnimationEvent() =>
        Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted;

    private PlayerStateController CreateStateController() => new(new()
    {
        OnIdleEntry = () => Sprite.SetAnimation(PlayerSprite.AnimationIdle),
        OnMovingEntry = () => Sprite.SetAnimation(PlayerSprite.AnimationRun),
        OnAttackingEntry = () =>
        {
            var animKey = _pendingAttackType switch
            {
                AttackType.Attack2 => PlayerSprite.AnimationAttack2,
                AttackType.Attack3 => PlayerSprite.AnimationAttack3,
                _ => PlayerSprite.AnimationAttack1
            };
            Sprite.SetAnimation(animKey);
            CurrentMove = PlayerMoves.All[animKey];
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
            Sprite.SetAnimation(PlayerSprite.AnimationHurt);
            SubscribeToAnimationEvent();
        },
        OnHurtExit = UnsubscribeFromAnimationEvent,
        OnKnockdownEntry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationFall);
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
            Sprite.SetAnimation(PlayerSprite.AnimationDie);
            SubscribeToAnimationEvent();
        },
        OnDyingExit = UnsubscribeFromAnimationEvent,
        OnDeadEntry = () => Died?.Invoke(this, EventArgs.Empty)
    });

    public override void Update(GameTime gameTime)
    {
        if (_invincibilityTimer > 0)
            _invincibilityTimer = Math.Max(0, _invincibilityTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_stateController.State is PlayerState.Dead or PlayerState.Dying or PlayerState.Hurt or PlayerState.KnockedDown)
        {
            MovementDirection = Vector2.Zero;
            base.Update(gameTime);
            return;
        }

        if (MovementDirection == Vector2.Zero)
        {
            _stateController.Fire(PlayerTrigger.MoveStop);
        }
        else
        {
            Vector2 movementDirectionNoDiagonal = PreventDiagonal(MovementDirection);
            _stateController.Fire(PlayerTrigger.MoveStart);
            UpdateFacingDirection(movementDirectionNoDiagonal);
            if (_stateController.IsInState(PlayerState.Moving))
            {
                Move(movementDirectionNoDiagonal, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
        }
        base.Update(gameTime);
    }

    private static Vector2 PreventDiagonal(Vector2 direction) =>
        Math.Abs(direction.X) > Math.Abs(direction.Y)
            ? new Vector2(direction.X, 0)
            : new Vector2(0, direction.Y);

    public void Attack1() { _pendingAttackType = AttackType.Attack1; _stateController.Fire(PlayerTrigger.AttackStart); }
    public void Attack2() { _pendingAttackType = AttackType.Attack2; _stateController.Fire(PlayerTrigger.AttackStart); }
    public void Attack3() { _pendingAttackType = AttackType.Attack3; _stateController.Fire(PlayerTrigger.AttackStart); }

    public override void TakeDamage(int amount, bool knockdown = false)
    {
        if (!IsAlive || _invincibilityTimer > 0 || _stateController.State == PlayerState.KnockedDown) return;

        Health = Math.Max(0, Health - amount);

        if (Health <= 0)
        {
            _stateController.Fire(PlayerTrigger.Die);
        }
        else if (knockdown)
        {
            _invincibilityTimer = 1.5f;
            _stateController.Fire(PlayerTrigger.TakeKnockdown);
        }
        else
        {
            _invincibilityTimer = 1.0f;
            _stateController.Fire(PlayerTrigger.TakeDamage);
        }
    }

    public void Move(Vector2 direction, float deltaTime)
    {
        Position += direction * deltaTime * Speed;
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        Health = MaxHealth;
        MovementDirection = Vector2.Zero;
        Direction = FacingDirection.Right;
        Sprite.Effect = SpriteEffects.None;
        Sprite.SetAnimation(PlayerSprite.AnimationIdle);
        CurrentMove = null;
        ResetAnimationFrameIndex();
        _stateController = CreateStateController();
        _knockdownPhase = 0;
    }

    private void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;

        if (_stateController.State == PlayerState.KnockedDown)
        {
            if (_knockdownPhase == 0)
            {
                Sprite.SetAnimation(PlayerSprite.AnimationGetUp);
                _knockdownPhase = 1;
                SubscribeToAnimationEvent();
            }
            else
            {
                _stateController.Fire(PlayerTrigger.KnockdownCompleted);
            }
            return;
        }

        _stateController.Fire(_stateController.State switch
        {
            PlayerState.Hurt => PlayerTrigger.HurtCompleted,
            PlayerState.Dying => PlayerTrigger.DeathCompleted,
            _ => PlayerTrigger.AttackCompleted
        });
    }
}