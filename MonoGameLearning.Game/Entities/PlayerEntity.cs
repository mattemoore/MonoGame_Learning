using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
using Stateless;

//test

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
    private const float BASE_MOVEMENT_SPEED = 0.2f;

    public PlayerEntity(Vector2 position,
                        float width,
                        float height,
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
                Move(movementDirectionNoDiagonal, gameTime.ElapsedGameTime.Milliseconds);
            }
        }
        base.Update(gameTime);
    }

    private static Vector2 PreventDiagonal(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            direction.Y = 0;
        }
        else
        {
            direction.X = 0;
        }
        return direction;
    }


    private static Trigger GetDirectionalTrigger(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            return direction.X > 0 ? Trigger.MoveRightStart : Trigger.MoveLeftStart;
        }
        else
        {
            return direction.Y > 0 ? Trigger.MoveDownStart : Trigger.MoveUpStart;
        }
    }

    public void Attack1()
    {
        _stateMachine.Fire(Trigger.Attack1Start);
    }

    public void Attack2()
    {
        _stateMachine.Fire(Trigger.Attack2Start);
    }

    public void Attack3()
    {
        _stateMachine.Fire(Trigger.Attack3Start);
    }

    public void Move(Vector2 direction, int deltaTimeInMilliseconds)
    {
        Vector2 newPosition = new Vector2(Position.X + (direction.X * deltaTimeInMilliseconds * BASE_MOVEMENT_SPEED),
                                          Position.Y + (direction.Y * deltaTimeInMilliseconds * BASE_MOVEMENT_SPEED));
        Position = newPosition;
    }

    private StateMachine<State, Trigger> InitStateMachine()
    {
        StateMachine<State, Trigger> stateMachine = new StateMachine<State, Trigger>(State.Dummy);
        stateMachine.Configure(State.Dummy)
            .OnActivate(() => stateMachine.Fire(Trigger.Activate))
            .Permit(Trigger.Activate, State.Idling)
            .Ignore(Trigger.AttackCompleted)
            .Ignore(Trigger.Attack1Start)
            .Ignore(Trigger.Attack2Start)
            .Ignore(Trigger.Attack3Start);
        stateMachine.Configure(State.Attacking)
            .OnEntry(t => OnAttackingEntry())
            .OnExit(t => OnAttackingExit())
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
            .OnEntry(t => OnAttacking1Entry())
            .OnExit(t => OnAttacking1Exit())
            .SubstateOf(State.Attacking);
        stateMachine.Configure(State.Attacking2)
            .OnEntry(t => OnAttacking2Entry())
            .OnExit(t => OnAttacking2Exit())
            .SubstateOf(State.Attacking);
        stateMachine.Configure(State.Attacking3)
            .OnEntry(t => OnAttacking3Entry())
            .OnExit(t => OnAttacking3Exit())
            .SubstateOf(State.Attacking);
        stateMachine.Configure(State.Idling)
            .OnEntry(t => OnIdleEntry())
            .OnExit(t => OnIdleExit())
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
            .OnEntry(t => OnMovingEntry())
            .OnExit(t => OnMovingExit())
            .Permit(Trigger.Attack1Start, State.Attacking1)
            .Permit(Trigger.Attack2Start, State.Attacking2)
            .Permit(Trigger.Attack3Start, State.Attacking3)
            .Permit(Trigger.MoveStop, State.Idling)
            .Ignore(Trigger.Activate)
            .Ignore(Trigger.AttackCompleted);
        stateMachine.Configure(State.MovingLeft)
            .OnEntry(t => OnMovingLeftEntry())
            .OnExit(t => OnMovingLeftExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.MoveLeftStart);
        stateMachine.Configure(State.MovingRight)
            .OnEntry(t => OnMovingRightEntry())
            .OnExit(t => OnMovingRightExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.MoveRightStart);
        stateMachine.Configure(State.MovingUp)
            .OnEntry(t => OnMovingUpEntry())
            .OnExit(t => OnMovingUpExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveDownStart, State.MovingDown)
            .Ignore(Trigger.MoveUpStart);
        stateMachine.Configure(State.MovingDown)
            .OnEntry(t => OnMovingDownEntry())
            .OnExit(t => OnMovingDownExit())
            .SubstateOf(State.Moving)
            .Permit(Trigger.MoveRightStart, State.MovingRight)
            .Permit(Trigger.MoveUpStart, State.MovingUp)
            .Permit(Trigger.MoveLeftStart, State.MovingLeft)
            .Ignore(Trigger.MoveDownStart);

        stateMachine.Activate();
        return stateMachine;
    }

    private void OnAttackingEntry()
    {
        Console.WriteLine("Entering attacking.");

    }

    private void OnAttackingExit()
    {
        Console.WriteLine("Exiting attacking.");
    }

    private void OnAttacking1Entry()
    {
        Console.WriteLine("Entering attacking 1.");
        Sprite.SetAnimation("attack1");
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
    }

    private void OnAttacking1Exit()
    {
        Console.WriteLine("Exiting attacking 1.");
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;
    }

    private void OnAttacking2Entry()
    {
        Console.WriteLine("Entering attacking 2.");
        Sprite.SetAnimation("attack2");
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;

    }

    private void OnAttacking2Exit()
    {
        Console.WriteLine("Exiting attacking 2.");
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;

    }

    private void OnAttacking3Entry()
    {
        Console.WriteLine("Entering attacking 3.");
        Sprite.SetAnimation("attack3");
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;

    }

    private void OnAttacking3Exit()
    {
        Console.WriteLine("Exiting attacking 3.");
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;
    }

    private void OnIdleEntry()
    {
        Console.WriteLine("Entering idle");
        Sprite.SetAnimation("idle");
    }

    private void OnIdleExit()
    {
        Console.WriteLine("Exiting idle");
    }

    private void OnMovingEntry()
    {
        Console.WriteLine("Entering moving");
    }

    private void OnMovingExit()
    {
        Console.WriteLine("Exiting moving");
    }

    private void OnMovingLeftEntry()
    {
        Console.WriteLine("Entering moving left");
        Sprite.SetAnimation("run");
        Sprite.Effect = SpriteEffects.FlipHorizontally;
    }

    private void OnMovingLeftExit()
    {
        Console.WriteLine("Exiting moving left");
    }

    private void OnMovingRightEntry()
    {
        Console.WriteLine("Entering moving right");
        Sprite.SetAnimation("run");
        if (Sprite.Effect == SpriteEffects.FlipHorizontally)
        {
            Sprite.Effect = SpriteEffects.None;
        }
    }

    private void OnMovingRightExit()
    {
        Console.WriteLine("Exiting moving right");
    }

    private void OnMovingUpEntry()
    {
        Console.WriteLine("Entering moving up");
        Sprite.SetAnimation("run");
    }

    private void OnMovingUpExit()
    {
        Console.WriteLine("Exiting moving up");
    }

    private void OnMovingDownEntry()
    {
        Console.WriteLine("Entering moving down");
        Sprite.SetAnimation("run");
    }

    private void OnMovingDownExit()
    {
        Console.WriteLine("Exiting moving down");
    }


    private void OnAttackingAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {
        if (animationEventTrigger == AnimationEventTrigger.AnimationCompleted)
        {
            _stateMachine.Fire(Trigger.AttackCompleted);
        }
    }
}