using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Player;

public class PlayerEntity : ActorEntity
{
    public Vector2 MovementDirection { get; set; }
    private PlayerStateController _stateController;
    private const float BASE_MOVEMENT_SPEED = 200f;
    public int Health { get; private set; }
    public int MaxHealth { get; } = 100;
    public bool IsAlive => Health > 0;
    public event EventHandler Died;

    public PlayerEntity(string name,
                        Vector2 position,
                        float scale,
                        AnimatedSprite sprite) : base(name, position, scale, sprite)
    {
        Health = MaxHealth;
        _stateController = CreateStateController();
    }

    private PlayerStateController CreateStateController() => new(new()
    {
        OnIdleEntry = () => Sprite.SetAnimation(PlayerSprite.AnimationIdle),
        OnMovingLeftEntry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationRun);
            Sprite.Effect = SpriteEffects.FlipHorizontally;
        },
        OnMovingRightEntry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationRun);
            if (Sprite.Effect == SpriteEffects.FlipHorizontally)
                Sprite.Effect = SpriteEffects.None;
        },
        OnMovingUpEntry = () => Sprite.SetAnimation(PlayerSprite.AnimationRun),
        OnMovingDownEntry = () => Sprite.SetAnimation(PlayerSprite.AnimationRun),
        OnAttacking1Entry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationAttack1);
            Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;
        },
        OnAttacking1Exit = () => Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted,
        OnAttacking2Entry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationAttack2);
            Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;
        },
        OnAttacking2Exit = () => Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted,
        OnAttacking3Entry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationAttack3);
            Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;
        },
        OnAttacking3Exit = () => Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted,
        OnHurtEntry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationHurt);
            Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;
        },
        OnHurtExit = () => Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted,
        OnDyingEntry = () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationDie);
            Sprite.Controller.OnAnimationEvent += OnAnimationCompleted;
        },
        OnDyingExit = () => Sprite.Controller.OnAnimationEvent -= OnAnimationCompleted,
        OnDeadEntry = () => Died?.Invoke(this, EventArgs.Empty)
    });

    public override void Update(GameTime gameTime)
    {
        if (_stateController.State is PlayerState.Dead or PlayerState.Dying)
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
            PlayerTrigger directionTrigger = GetDirectionalTrigger(movementDirectionNoDiagonal);
            _stateController.Fire(directionTrigger);
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

    private static PlayerTrigger GetDirectionalTrigger(Vector2 direction) =>
        Math.Abs(direction.X) > Math.Abs(direction.Y)
            ? (direction.X > 0 ? PlayerTrigger.MoveRightStart : PlayerTrigger.MoveLeftStart)
            : (direction.Y > 0 ? PlayerTrigger.MoveDownStart : PlayerTrigger.MoveUpStart);

    public void Attack1() => _stateController.Fire(PlayerTrigger.Attack1Start);
    public void Attack2() => _stateController.Fire(PlayerTrigger.Attack2Start);
    public void Attack3() => _stateController.Fire(PlayerTrigger.Attack3Start);

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        Health = Math.Max(0, Health - amount);

        if (Health <= 0)
        {
            _stateController.Fire(PlayerTrigger.Die);
        }
        else
        {
            _stateController.Fire(PlayerTrigger.TakeDamage);
        }
    }

    public void Move(Vector2 direction, float deltaTime)
    {
        Position += direction * deltaTime * BASE_MOVEMENT_SPEED;
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        Health = MaxHealth;
        MovementDirection = Vector2.Zero;
        Sprite.SetAnimation(PlayerSprite.AnimationIdle);
        _stateController = CreateStateController();
    }

    private void OnAnimationCompleted(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger != AnimationEventTrigger.AnimationCompleted) return;
        _stateController.Fire(_stateController.State switch
        {
            PlayerState.Hurt => PlayerTrigger.HurtCompleted,
            PlayerState.Dying => PlayerTrigger.DeathCompleted,
            _ => PlayerTrigger.AttackCompleted
        });
    }
}