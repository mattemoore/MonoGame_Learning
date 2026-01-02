using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Entities;
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
        Moving
    }

    private enum Trigger
    {
        Activate,
        Attack1Start,
        Attack2Start,
        Attack3Start,
        AttackCompleted,
        MoveStart,
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
            _stateMachine.Fire(Trigger.MoveStart);
            if (_stateMachine.IsInState(State.Moving))
            {
                Move(MovementDirection, gameTime.ElapsedGameTime.Milliseconds);
            }
        }
        base.Update(gameTime);
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
            .Ignore(Trigger.MoveStart)
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
            .Permit(Trigger.MoveStart, State.Moving)
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
            .Ignore(Trigger.AttackCompleted)
            .Ignore(Trigger.MoveStart);
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
        Sprite.Controller.OnAnimationEvent += OnIdleAnimationEvent;
    }

    private void OnIdleExit()
    {
        Sprite.Controller.OnAnimationEvent -= OnIdleAnimationEvent;
        Console.WriteLine("Exiting idle");
    }

    private void OnMovingEntry()
    {
        Console.WriteLine("Entering moving");
        Sprite.SetAnimation("idle");
        Sprite.Controller.OnAnimationEvent += OnIdleAnimationEvent;
    }

    private void OnMovingExit()
    {
        Sprite.Controller.OnAnimationEvent -= OnIdleAnimationEvent;
        Console.WriteLine("Exiting moving");
    }

    private void OnAttackingAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {
        if (animationEventTrigger == AnimationEventTrigger.AnimationCompleted)
        {
            _stateMachine.Fire(Trigger.AttackCompleted);
        }
    }

    private void OnIdleAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {

    }
}