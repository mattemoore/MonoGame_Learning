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

    private PlayerStateController CreateStateController() => new(
        onIdleEntry: () => Sprite.SetAnimation(PlayerSprite.AnimationIdle),
        onMovingLeftEntry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationRun);
            Sprite.Effect = SpriteEffects.FlipHorizontally;
        },
        onMovingRightEntry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationRun);
            if (Sprite.Effect == SpriteEffects.FlipHorizontally)
                Sprite.Effect = SpriteEffects.None;
        },
        onMovingUpEntry: () => Sprite.SetAnimation(PlayerSprite.AnimationRun),
        onMovingDownEntry: () => Sprite.SetAnimation(PlayerSprite.AnimationRun),
        onAttacking1Entry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationAttack1);
            Sprite.Controller.OnAnimationEvent += OnAttackAnimationEvent;
        },
        onAttacking1Exit: () => Sprite.Controller.OnAnimationEvent -= OnAttackAnimationEvent,
        onAttacking2Entry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationAttack2);
            Sprite.Controller.OnAnimationEvent += OnAttackAnimationEvent;
        },
        onAttacking2Exit: () => Sprite.Controller.OnAnimationEvent -= OnAttackAnimationEvent,
        onAttacking3Entry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationAttack3);
            Sprite.Controller.OnAnimationEvent += OnAttackAnimationEvent;
        },
        onAttacking3Exit: () => Sprite.Controller.OnAnimationEvent -= OnAttackAnimationEvent,
        onHurtEntry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationHurt);
            Sprite.Controller.OnAnimationEvent += OnHurtAnimationEvent;
        },
        onHurtExit: () => Sprite.Controller.OnAnimationEvent -= OnHurtAnimationEvent,
        onDyingEntry: () =>
        {
            Sprite.SetAnimation(PlayerSprite.AnimationDie);
            Sprite.Controller.OnAnimationEvent += OnDeathAnimationEvent;
        },
        onDyingExit: () => Sprite.Controller.OnAnimationEvent -= OnDeathAnimationEvent,
        onDeadEntry: () => Died?.Invoke(this, EventArgs.Empty)
    );

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

    private void OnAttackAnimationEvent(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger == AnimationEventTrigger.AnimationCompleted)
            _stateController.Fire(PlayerTrigger.AttackCompleted);
    }

    private void OnHurtAnimationEvent(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger == AnimationEventTrigger.AnimationCompleted)
            _stateController.Fire(PlayerTrigger.HurtCompleted);
    }

    private void OnDeathAnimationEvent(IAnimationController controller, AnimationEventTrigger trigger)
    {
        if (trigger == AnimationEventTrigger.AnimationCompleted)
            _stateController.Fire(PlayerTrigger.DeathCompleted);
    }
}