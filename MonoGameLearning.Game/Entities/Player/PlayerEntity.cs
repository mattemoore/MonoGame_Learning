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
    private readonly PlayerStateController _stateController;
    private const float BASE_MOVEMENT_SPEED = 200f;

    public PlayerEntity(string name,
                        Vector2 position,
                        float scale,
                        AnimatedSprite sprite) : base(name, position, scale, sprite)
    {
        _stateController = new PlayerStateController(
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
                Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
            },
            onAttacking1Exit: () => Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent,
            onAttacking2Entry: () =>
            {
                Sprite.SetAnimation(PlayerSprite.AnimationAttack2);
                Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
            },
            onAttacking2Exit: () => Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent,
            onAttacking3Entry: () =>
            {
                Sprite.SetAnimation(PlayerSprite.AnimationAttack3);
                Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
            },
            onAttacking3Exit: () => Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent
        );
    }

    public override void Update(GameTime gameTime)
    {
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

    public void Move(Vector2 direction, float deltaTime)
    {
        Position += direction * deltaTime * BASE_MOVEMENT_SPEED;
    }

    private void OnAttackingAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {
        if (animationEventTrigger == AnimationEventTrigger.AnimationCompleted)
        {
            _stateController.Fire(PlayerTrigger.AttackCompleted);
        }
    }
}