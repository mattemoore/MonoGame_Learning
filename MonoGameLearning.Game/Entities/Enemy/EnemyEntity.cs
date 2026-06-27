using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Game.Sprites;

namespace MonoGameLearning.Game.Entities.Enemy;

public class EnemyEntity : CombatActorBase
{
    private readonly EnemyStateController _stateController;
    private readonly EnemyAI _ai;

    protected override bool IsIncapacitated => _stateController.State is EnemyState.Dead or EnemyState.Dying or EnemyState.Hurt or EnemyState.KnockedDown;
    protected override bool IsInKnockedDownState => _stateController.State == EnemyState.KnockedDown;
    protected override bool IsInHurtState => _stateController.State == EnemyState.Hurt;
    protected override bool IsInDyingState => _stateController.State == EnemyState.Dying;
    protected override void FireKnockdownCompleted() => _stateController.Fire(EnemyTrigger.KnockdownCompleted);
    protected override void FireHurtCompleted() => _stateController.Fire(EnemyTrigger.HurtCompleted);
    protected override void FireDeathCompleted() => _stateController.Fire(EnemyTrigger.DeathCompleted);
    protected override void FireAttackCompleted()
    {
        _ai.AttackCooldown = 1.5f;
        _stateController.Fire(EnemyTrigger.AttackCompleted);
    }

    public Entity Target { get; set; }
    public float AttackRange { get; set; } = 70f;
    public float MinChaseDistance { get; set; } = 60f;

    public readonly MoveData AttackMove = new()
    {
        AnimationKey = EnemySprite.AnimationAttack1,
        Damage = 5,
        Strength = AttackStrength.Light,
        FrameHitboxes = new()
        {
            [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
        }
    };

    public EnemyEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
        : base(name, position, 48, 60, sprite, scale, 30, new(EnemySprite.AnimationIdle, EnemySprite.AnimationRun, EnemySprite.AnimationHurt, EnemySprite.AnimationFall, EnemySprite.AnimationDie, EnemySprite.AnimationGetUp))
    {
        Speed = 120f;
        Sprite.Color = Color.Red;
        Faction = Faction.Enemy;
        _ai = new EnemyAI(AttackRange, MinChaseDistance);
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
        OnIdleEntry = () => Sprite.SetAnimation(Animations.Idle),
        OnChasingEntry = () => Sprite.SetAnimation(Animations.Run),
        OnAttackingEntry = () =>
        {
            CurrentMove = AttackMove;
            FrameTracker.Reset();
            PlayAnimation(AttackMove.AnimationKey);
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

        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool isIdleOrChasing = _stateController.State is EnemyState.Idle or EnemyState.Chasing;

        if (Target is not null)
        {
            var action = _ai.Update(Position, Target.Position, isIdleOrChasing, deltaSeconds);

            switch (action)
            {
                case AIAction.StartChase:
                    if (_stateController.State == EnemyState.Idle)
                        _stateController.Fire(EnemyTrigger.StartChase);
                    break;
                case AIAction.StopChase:
                    _stateController.Fire(EnemyTrigger.StopChase);
                    break;
                case AIAction.Attack:
                    _stateController.Fire(EnemyTrigger.AttackStart);
                    break;
            }

            if (_ai.FacingChanged)
                Direction = Mover.UpdateFacingDirection(Sprite, new Vector2(_ai.NewFacingX, 0), Direction);
        }
        else
        {
            _ai.AttackCooldown = Math.Max(0, _ai.AttackCooldown - deltaSeconds);
        }

        if (_stateController.State == EnemyState.Chasing && _ai.MovementDirection != Vector2.Zero)
            Position += _ai.MovementDirection * deltaSeconds * Speed;

        AdvanceFrameAndRegisterHitboxes(gameTime);
    }

    public void Reset(Vector2 position, Entity target)
    {
        ResetActor(position);
        Target = target;
        _ai.Reset();
        Sprite.Color = Color.Red;
    }
}