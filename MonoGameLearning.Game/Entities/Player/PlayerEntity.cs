using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Player;

public class PlayerEntity : CombatActorBase
{
    private PlayerStateController _stateController;
    private float _invincibilityTimer;
    private MoveData _pendingMove;

    public readonly MoveData Attack1Move = new()
    {
        AnimationKey = PlayerSprite.AnimationAttack1,
        Damage = 5,
        Strength = AttackStrength.Light,
        FrameHitboxes = new()
        {
            [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
        }
    };
    public readonly MoveData Attack2Move = new()
    {
        AnimationKey = PlayerSprite.AnimationAttack2,
        Damage = 8,
        Strength = AttackStrength.Medium,
        FrameHitboxes = new()
        {
            [1] = [new() { Offset = new Vector2(45, -10), Size = new Point(50, 50) }],
            [2] = [new() { Offset = new Vector2(45, -10), Size = new Point(50, 50) }],
        }
    };
    public readonly MoveData Attack3Move = new()
    {
        AnimationKey = PlayerSprite.AnimationAttack3,
        Damage = 12,
        Knockdown = true,
        Strength = AttackStrength.Heavy,
        FrameHitboxes = new()
        {
            [2] = [new() { Offset = new Vector2(50, 0), Size = new Point(55, 40) }],
        }
    };

    protected override bool IsIncapacitated => _stateController.State is PlayerState.Dead or PlayerState.Dying or PlayerState.Hurt or PlayerState.KnockedDown;
    protected override bool IsInKnockedDownState => _stateController.State == PlayerState.KnockedDown;
    protected override bool IsInHurtState => _stateController.State == PlayerState.Hurt;
    protected override bool IsInDyingState => _stateController.State == PlayerState.Dying;
    protected override void FireKnockdownCompleted() => _stateController.Fire(PlayerTrigger.KnockdownCompleted);
    protected override void FireHurtCompleted() => _stateController.Fire(PlayerTrigger.HurtCompleted);
    protected override void FireDeathCompleted() => _stateController.Fire(PlayerTrigger.DeathCompleted);
    protected override void FireAttackCompleted() => _stateController.Fire(PlayerTrigger.AttackCompleted);

    public PlayerEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, 48, 60, sprite, scale, 100, new(PlayerSprite.AnimationIdle, PlayerSprite.AnimationRun, PlayerSprite.AnimationHurt, PlayerSprite.AnimationFall, PlayerSprite.AnimationDie, PlayerSprite.AnimationGetUp))
    {
        Speed = 200f;
        Faction = Faction.Player;
        _stateController = CreateStateController();
    }

    protected override bool CanTakeDamage() =>
        HealthComponent.IsAlive && _invincibilityTimer <= 0 && _stateController.State != PlayerState.KnockedDown;

    protected override void OnDeath() => _stateController.Fire(PlayerTrigger.Die);

    protected override void OnKnockdown(DamageInfo info)
    {
        _invincibilityTimer = 1.5f;
        _stateController.Fire(PlayerTrigger.TakeKnockdown);
    }

    protected override void OnHit(DamageInfo info)
    {
        _invincibilityTimer = 1.0f;
        _stateController.Fire(PlayerTrigger.TakeDamage);
    }

    private PlayerStateController CreateStateController() => new(new()
    {
        OnIdleEntry = () => Sprite.SetAnimation(Animations.Idle),
        OnMovingEntry = () => Sprite.SetAnimation(Animations.Run),
        OnAttackingEntry = () =>
        {
            CurrentMove = _pendingMove;
            FrameTracker.Reset();
            PlayAnimation(_pendingMove.AnimationKey);
        },
        OnAttackingExit = AttackingExit(),
        OnHurtEntry = HurtEntry(),
        OnHurtExit = HurtExit(),
        OnKnockdownEntry = KnockdownEntry(),
        OnKnockdownExit = KnockdownExit(),
        OnDyingEntry = DyingEntry(),
        OnDyingExit = DyingExit(),
        OnDeadEntry = DeadEntry()
    });

    public override void Update(GameTime gameTime)
    {
        if (!EnsureSpriteAttached()) return;

        if (_invincibilityTimer > 0)
            _invincibilityTimer = Math.Max(0, _invincibilityTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (TryHandleIncapacitatedUpdate(gameTime)) return;

        if (MovementDirection == Vector2.Zero)
        {
            _stateController.Fire(PlayerTrigger.MoveStop);
        }
        else
        {
            Vector2 movementDirectionNoDiagonal = Mover.PreventDiagonal(MovementDirection);
            _stateController.Fire(PlayerTrigger.MoveStart);
            Direction = Mover.UpdateFacingDirection(Sprite, movementDirectionNoDiagonal, Direction);
            if (_stateController.IsInState(PlayerState.Moving))
                Move(movementDirectionNoDiagonal, (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        AdvanceFrameAndRegisterHitboxes(gameTime);
    }

    public void Attack(MoveData move) { _pendingMove = move; _stateController.Fire(PlayerTrigger.AttackStart); }

    public void Move(Vector2 direction, float deltaTime) =>
        Position += direction * deltaTime * Speed;

    public void Reset(Vector2 position)
    {
        ResetActor(position);
        _stateController = CreateStateController();
    }
}