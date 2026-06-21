using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Enemy;

public class EnemyEntity : CombatActorBase
{
    private readonly EnemyStateController _stateController;
    private float _attackCooldown;
    private float _attackDelayTimer;
    private float _lastFacingX;
    private float _directionUpdateTimer;
    private const float AttackDelayDuration = 1.0f;
    private const float DirectionUpdateInterval = 0.35f;

    // --- Animation keys ---
    protected override string IdleAnimation => EnemySprite.AnimationIdle;
    protected override string RunAnimation => EnemySprite.AnimationRun;
    protected override string HurtAnimation => EnemySprite.AnimationHurt;
    protected override string FallAnimation => EnemySprite.AnimationFall;
    protected override string DieAnimation => EnemySprite.AnimationDie;
    protected override string GetUpAnimation => EnemySprite.AnimationGetUp;
    protected override bool IsIncapacitated => _stateController.State is EnemyState.Dead or EnemyState.Dying or EnemyState.Hurt or EnemyState.KnockedDown;

    // --- Animation completion ---
    protected override bool IsInKnockedDownState => _stateController.State == EnemyState.KnockedDown;
    protected override bool IsInHurtState => _stateController.State == EnemyState.Hurt;
    protected override bool IsInDyingState => _stateController.State == EnemyState.Dying;
    protected override void FireKnockdownCompleted() => _stateController.Fire(EnemyTrigger.KnockdownCompleted);
    protected override void FireHurtCompleted() => _stateController.Fire(EnemyTrigger.HurtCompleted);
    protected override void FireDeathCompleted() => _stateController.Fire(EnemyTrigger.DeathCompleted);
    protected override void FireAttackCompleted()
    {
        _attackCooldown = 1.5f;
        _stateController.Fire(EnemyTrigger.AttackCompleted);
    }

    public Entity Target { get; set; }
    public float AttackRange { get; set; } = 70f;
    public float MinChaseDistance { get; set; } = 60f;

    public EnemyEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, 48, 60, sprite, scale, 30)
    {
        Speed = 120f;
        Sprite.Color = Color.Red;
        Faction = Faction.Enemy;
        _stateController = CreateStateController();
    }

    protected override bool CanTakeDamage() =>
        HealthComponent.IsAlive && _stateController.State != EnemyState.KnockedDown;

    protected override void OnDeath() => _stateController.Fire(EnemyTrigger.Die);

    protected override void OnKnockdown(DamageInfo info) =>
        _stateController.Fire(EnemyTrigger.TakeKnockdown);

    protected override void OnHit(DamageInfo info) =>
        _stateController.Fire(EnemyTrigger.TakeDamage);

    private EnemyStateController CreateStateController() => new(new()
    {
        OnIdleEntry = () => Sprite.SetAnimation(IdleAnimation),
        OnChasingEntry = () => Sprite.SetAnimation(RunAnimation),
        OnAttackingEntry = () =>
        {
            Sprite.SetAnimation(EnemySprite.AnimationAttack1);
            CurrentMove = EnemyMoves.All[EnemySprite.AnimationAttack1];
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

        if (TryHandleIncapacitatedUpdate(gameTime)) return;

        if (Target is not null)
        {
            Vector2 toTarget = Target.Position - Position;
            float distance = toTarget.Length();

            if (distance <= AttackRange && _stateController.State is EnemyState.Idle or EnemyState.Chasing && _attackCooldown <= 0)
            {
                if (_stateController.State == EnemyState.Chasing)
                {
                    _stateController.Fire(EnemyTrigger.StopChase);
                    MovementDirection = Vector2.Zero;
                }

                if (_attackDelayTimer <= 0)
                    _attackDelayTimer = AttackDelayDuration;

                _attackDelayTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_attackDelayTimer <= 0)
                {
                    _attackDelayTimer = 0;
                    _stateController.Fire(EnemyTrigger.AttackStart);
                }
            }
            else if (distance > AttackRange && _stateController.State is EnemyState.Idle or EnemyState.Chasing)
            {
                _attackDelayTimer = 0;
                _stateController.Fire(EnemyTrigger.StartChase);
                _directionUpdateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_directionUpdateTimer <= 0)
                {
                    _directionUpdateTimer = DirectionUpdateInterval;
                    toTarget /= distance;
                    MovementDirection = Mover.PreventDiagonal(toTarget);
                    if (Math.Sign(MovementDirection.X) != Math.Sign(_lastFacingX))
                    {
                        _lastFacingX = MovementDirection.X;
                        Direction = Mover.UpdateFacingDirection(Sprite, MovementDirection, Direction);
                    }
                }

                if (distance <= MinChaseDistance)
                    MovementDirection = Vector2.Zero;
            }
            else if (_stateController.State == EnemyState.Chasing && distance <= AttackRange)
            {
                _stateController.Fire(EnemyTrigger.StopChase);
                MovementDirection = Vector2.Zero;
            }
        }

        _attackCooldown = Math.Max(0, _attackCooldown - (float)gameTime.ElapsedGameTime.TotalSeconds);

        if (_stateController.State == EnemyState.Chasing && MovementDirection != Vector2.Zero)
            Position += MovementDirection * (float)gameTime.ElapsedGameTime.TotalSeconds * Speed;

        AdvanceFrameAndRegisterHitboxes(gameTime);
    }

    public void Reset(Vector2 position, Entity target)
    {
        ResetActor(position);
        Target = target;
        _attackCooldown = 0;
        _attackDelayTimer = 0;
        _directionUpdateTimer = 0;
        _lastFacingX = 0;
        Sprite.Color = Color.Red;
    }
}