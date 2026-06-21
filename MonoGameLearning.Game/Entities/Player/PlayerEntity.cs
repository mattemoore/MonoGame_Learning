using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Player;

public enum AttackType { Attack1, Attack2, Attack3 }

public class PlayerEntity : CombatActorBase
{
    private PlayerStateController _stateController;
    private float _invincibilityTimer;
    private AttackType _pendingAttackType;

    // --- Animation keys ---
    protected override string IdleAnimation => PlayerSprite.AnimationIdle;
    protected override string RunAnimation => PlayerSprite.AnimationRun;
    protected override string HurtAnimation => PlayerSprite.AnimationHurt;
    protected override string FallAnimation => PlayerSprite.AnimationFall;
    protected override string DieAnimation => PlayerSprite.AnimationDie;
    protected override string GetUpAnimation => PlayerSprite.AnimationGetUp;
    protected override bool IsIncapacitated => _stateController.State is PlayerState.Dead or PlayerState.Dying or PlayerState.Hurt or PlayerState.KnockedDown;

    // --- Animation completion ---
    protected override bool IsInKnockedDownState => _stateController.State == PlayerState.KnockedDown;
    protected override bool IsInHurtState => _stateController.State == PlayerState.Hurt;
    protected override bool IsInDyingState => _stateController.State == PlayerState.Dying;
    protected override void FireKnockdownCompleted() => _stateController.Fire(PlayerTrigger.KnockdownCompleted);
    protected override void FireHurtCompleted() => _stateController.Fire(PlayerTrigger.HurtCompleted);
    protected override void FireDeathCompleted() => _stateController.Fire(PlayerTrigger.DeathCompleted);
    protected override void FireAttackCompleted() => _stateController.Fire(PlayerTrigger.AttackCompleted);

    public PlayerEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, 48, 60, sprite, scale, 100)
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
        OnIdleEntry = () => Sprite.SetAnimation(IdleAnimation),
        OnMovingEntry = () => Sprite.SetAnimation(RunAnimation),
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
            FrameTracker.Reset();
            SubscribeToAnimationEvent();
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

    public void Attack1() { _pendingAttackType = AttackType.Attack1; _stateController.Fire(PlayerTrigger.AttackStart); }
    public void Attack2() { _pendingAttackType = AttackType.Attack2; _stateController.Fire(PlayerTrigger.AttackStart); }
    public void Attack3() { _pendingAttackType = AttackType.Attack3; _stateController.Fire(PlayerTrigger.AttackStart); }

    public void Move(Vector2 direction, float deltaTime) =>
        Position += direction * deltaTime * Speed;

    public void Reset(Vector2 position)
    {
        ResetActor(position);
        _stateController = CreateStateController();
    }
}