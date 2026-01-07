using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Sprites;
using Stateless;

namespace MonoGameLearning.Game.Entities;

public class PlayerEntity : ActorEntity
{
    public Vector2 MovementDirection { get; set; }
    private enum State
    {
        Dummy,
        Idling,
        Attacking,
        Attacking1,
        Attacking2,
        Attacking3,
        Moving,
        MovingLeft,
        MovingRight,
        MovingUp,
        MovingDown
    }

    private enum Trigger
    {
        Activate,
        Attack1Start,
        Attack2Start,
        Attack3Start,
        AttackCompleted,
        MoveLeftStart,
        MoveRightStart,
        MoveUpStart,
        MoveDownStart,
        MoveStop
    }

    private readonly StateMachine<State, Trigger> _stateMachine;
    private const float BASE_MOVEMENT_SPEED = 200f;

    public PlayerEntity(Vector2 position,
                        int width,
                        int height,
                        AnimatedSprite sprite) : base(position, width, height, sprite)
    {
        _stateMachine = InitStateMachine();
    }

    public override void Update(GameTime gameTime)
    {
        if (MovementDirection == Vector2.Zero)
        {
            _stateMachine.Fire(Trigger.MoveStop);
        }
        else
        {
            Vector2 movementDirectionNoDiagonal = PreventDiagonal(MovementDirection);
            Trigger directionTrigger = GetDirectionalTrigger(movementDirectionNoDiagonal);
            _stateMachine.Fire(directionTrigger);
            if (_stateMachine.IsInState(State.Moving))
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

    private static Trigger GetDirectionalTrigger(Vector2 direction) =>
        Math.Abs(direction.X) > Math.Abs(direction.Y)
            ? (direction.X > 0 ? Trigger.MoveRightStart : Trigger.MoveLeftStart)
            : (direction.Y > 0 ? Trigger.MoveDownStart : Trigger.MoveUpStart);

    public void Attack1() => _stateMachine.Fire(Trigger.Attack1Start);

    public void Attack2() => _stateMachine.Fire(Trigger.Attack2Start);

    public void Attack3() => _stateMachine.Fire(Trigger.Attack3Start);

    public void Move(Vector2 direction, float deltaTime) =>
        Position += direction * deltaTime * BASE_MOVEMENT_SPEED;

    private StateMachine<State, Trigger> InitStateMachine()
    {
        StateMachine<State, Trigger> stateMachine = new(State.Dummy);
        stateMachine.Configure(State.Dummy)
            .OnActivate(() => stateMachine.Fire(Trigger.Activate))
            .Permit(Trigger.Activate, State.Idling)
            .Ignore(Trigger.AttackCompleted)
            .Ignore(Trigger.Attack1Start)
            .Ignore(Trigger.Attack2Start)
            .Ignore(Trigger.Attack3Start);

        stateMachine.Configure(State.Attacking)
            .OnEntry(_ => OnAttackingEntry())
            .OnExit(_ => OnAttackingExit())
            .Permit(Trigger.AttackCompleted, State.Idling)
            .Ignore(Trigger.Attack1Start)
            .Ignore(Trigger.Attack2Start)
            .Ignore(Trigger.Attack3Start)
            .Ignore(Trigger.MoveLeftStart)
            .Ignore(Trigger.MoveRightStart)
            .Ignore(Trigger.MoveUpStart)
            .Ignore(Trigger.MoveDownStart)
            .Ignore(Trigger.MoveStop)
            .Ignore(Trigger.Activate);

        stateMachine.Configure(State.Attacking1)
            .OnEntry(_ => OnAttacking1Entry())
            .OnExit(_ => OnAttacking1Exit())
            .SubstateOf(State.Attacking);

        stateMachine.Configure(State.Attacking2)
            .OnEntry(_ => OnAttacking2Entry())
            .OnExit(_ => OnAttacking2Exit())
            .SubstateOf(State.Attacking);

        stateMachine.Configure(State.Attacking3)
            .OnEntry(_ => OnAttacking3Entry())
            .OnExit(_ => OnAttacking3Exit())
            .SubstateOf(State.Attacking);

        stateMachine.Configure(State.Idling)
            .OnEntry(_ => OnIdleEntry())
            .OnExit(_ => OnIdleExit())
            .Permit(Trigger.Attack1Start, State.Attacking1)
            .Permit(Trigger.Attack2Start, State.Attacking2)
            .Permit(Trigger.Attack3Start, State.Attacking3)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.Activate)
            .Ignore(Trigger.AttackCompleted)
            .Ignore(Trigger.MoveStop);

        stateMachine.Configure(State.Moving)
            .OnEntry(_ => OnMovingEntry())
            .OnExit(_ => OnMovingExit())
            .Permit(Trigger.Attack1Start, State.Attacking1)
            .Permit(Trigger.Attack2Start, State.Attacking2)
            .Permit(Trigger.Attack3Start, State.Attacking3)
            .Permit(Trigger.MoveStop, State.Idling)
            .Ignore(Trigger.Activate)
            .Ignore(Trigger.AttackCompleted);

        stateMachine.Configure(State.MovingLeft)
            .OnEntry(_ => OnMovingLeftEntry())
            .OnExit(_ => OnMovingLeftExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.MoveLeftStart);

        stateMachine.Configure(State.MovingRight)
            .OnEntry(_ => OnMovingRightEntry())
            .OnExit(_ => OnMovingRightExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.MoveRightStart);

        stateMachine.Configure(State.MovingUp)
            .OnEntry(_ => OnMovingUpEntry())
            .OnExit(_ => OnMovingUpExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.MoveUpStart);

        stateMachine.Configure(State.MovingDown)
            .OnEntry(_ => OnMovingDownEntry())
            .OnExit(_ => OnMovingDownExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Ignore(Trigger.MoveDownStart);

        stateMachine.Activate();
        return stateMachine;
    }

    private void OnAttackingEntry() => Debug.WriteLine("Entering attacking.");

    private void OnAttackingExit() => Debug.WriteLine("Exiting attacking.");

    private void OnAttacking1Entry()
    {
        Debug.WriteLine("Entering attacking 1.");
        Sprite.SetAnimation(PlayerSprite.AnimationAttack1);
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
    }

    private void OnAttacking1Exit()
    {
        Debug.WriteLine("Exiting attacking 1.");
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;
    }

    private void OnAttacking2Entry()
    {
        Debug.WriteLine("Entering attacking 2.");
        Sprite.SetAnimation(PlayerSprite.AnimationAttack2);
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
    }

    private void OnAttacking2Exit()
    {
        Debug.WriteLine("Exiting attacking 2.");
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;
    }

    private void OnAttacking3Entry()
    {
        Debug.WriteLine("Entering attacking 3.");
        Sprite.SetAnimation(PlayerSprite.AnimationAttack3);
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
    }

    private void OnAttacking3Exit()
    {
        Debug.WriteLine("Exiting attacking 3.");
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;
    }

    private void OnIdleEntry()
    {
        Debug.WriteLine("Entering idle");
        Sprite.SetAnimation(PlayerSprite.AnimationIdle);
    }

    private void OnIdleExit() => Debug.WriteLine("Exiting idle");

    private void OnMovingEntry() => Debug.WriteLine("Entering moving");

    private void OnMovingExit() => Debug.WriteLine("Exiting moving");

    private void OnMovingLeftEntry()
    {
        Debug.WriteLine("Entering moving left");
        Sprite.SetAnimation(PlayerSprite.AnimationRun);
        Sprite.Effect = SpriteEffects.FlipHorizontally;
    }

    private void OnMovingLeftExit() => Debug.WriteLine("Exiting moving left");

    private void OnMovingRightEntry()
    {
        Debug.WriteLine("Entering moving right");
        Sprite.SetAnimation(PlayerSprite.AnimationRun);
        if (Sprite.Effect == SpriteEffects.FlipHorizontally)
        {
            Sprite.Effect = SpriteEffects.None;
        }
    }

    private void OnMovingRightExit() => Debug.WriteLine("Exiting moving right");

    private void OnMovingUpEntry()
    {
        Debug.WriteLine("Entering moving up");
        Sprite.SetAnimation(PlayerSprite.AnimationRun);
    }

    private void OnMovingUpExit() => Debug.WriteLine("Exiting moving up");

    private void OnMovingDownEntry()
    {
        Debug.WriteLine("Entering moving down");
        Sprite.SetAnimation(PlayerSprite.AnimationRun);
    }

    private void OnMovingDownExit() => Debug.WriteLine("Exiting moving down");

    private void OnAttackingAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {
        if (animationEventTrigger == AnimationEventTrigger.AnimationCompleted)
        {
            _stateMachine.Fire(Trigger.AttackCompleted);
        }
    }
}