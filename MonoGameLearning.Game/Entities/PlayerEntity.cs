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
        Idle,
        Attacking,
        Moving
    }

    private enum Trigger
    {
        Activate,
        StartAttacking,
        AttackingCompleted,
        StartMoving,
        StopMoving
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
            _stateMachine.Fire(Trigger.StopMoving);
        }
        else
        {
            _stateMachine.Fire(Trigger.StartMoving);
            if (_stateMachine.IsInState(State.Moving))
            {
                Move(MovementDirection, gameTime.ElapsedGameTime.Milliseconds);
            }
        }
        base.Update(gameTime);
    }

    public void Attack()
    {
        _stateMachine.Fire(Trigger.StartAttacking);
    }

    public void StopMoving()
    {
        _stateMachine.Fire(Trigger.StopMoving);
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
            .Permit(Trigger.Activate, State.Idle)
            .Ignore(Trigger.AttackingCompleted)
            .Ignore(Trigger.StartAttacking);
        stateMachine.Configure(State.Attacking)
            .OnEntry(t => OnAttackingEntry())
            .OnExit(t => OnAttackingExit())
            .Permit(Trigger.AttackingCompleted, State.Idle)
            .Ignore(Trigger.StartAttacking)
            .Ignore(Trigger.StartMoving)
            .Ignore(Trigger.StopMoving)
            .Ignore(Trigger.Activate);
        stateMachine.Configure(State.Idle)
            .OnEntry(t => OnIdleEntry())
            .OnExit(t => OnIdleExit())
            .Permit(Trigger.StartAttacking, State.Attacking)
            .Permit(Trigger.StartMoving, State.Moving)
            .Ignore(Trigger.Activate)
            .Ignore(Trigger.AttackingCompleted)
            .Ignore(Trigger.StopMoving);
        stateMachine.Configure(State.Moving)
            .OnEntry(t => OnMovingEntry())
            .OnExit(t => OnMovingExit())
            .Permit(Trigger.StartAttacking, State.Attacking)
            .Permit(Trigger.StopMoving, State.Idle)
            .Ignore(Trigger.Activate)
            .Ignore(Trigger.AttackingCompleted)
            .Ignore(Trigger.StartMoving);
        stateMachine.Activate();
        return stateMachine;
    }

    private void OnAttackingEntry()
    {
        Console.WriteLine("Entering attacking.");
        Sprite.SetAnimation("attack");
        Sprite.Controller.OnAnimationEvent += OnAttackingAnimationEvent;
    }

    private void OnAttackingExit()
    {
        Sprite.Controller.OnAnimationEvent -= OnAttackingAnimationEvent;
        Console.WriteLine("Exiting attacking.");
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
        Console.WriteLine("Exiting moving down");
    }

    private void OnAttackingAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {
        if (animationEventTrigger == AnimationEventTrigger.AnimationCompleted)
        {
            _stateMachine.Fire(Trigger.AttackingCompleted);
        }
    }

    private void OnIdleAnimationEvent(IAnimationController animationController, AnimationEventTrigger animationEventTrigger)
    {

    }
}