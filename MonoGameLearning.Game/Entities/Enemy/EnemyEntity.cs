using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.AI;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Entities.Pickup;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.Levels;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.StateMachines;

namespace MonoGameLearning.Game.Entities.Enemy;

public class EnemyEntity : CombatActorBase, IPickupDropper
{
    private StateMachineController<EnemyState, EnemyTrigger> _stateController;
    private readonly EnemyAI _ai;
    private readonly LevelDirector _director;

    private float _spawnWalkTargetX;
    private Vector2 _spawnWalkDirection;

    protected override bool IsIncapacitated => _stateController.State is EnemyState.Dead or EnemyState.Dying or EnemyState.Hurt or EnemyState.KnockedDown;
    protected override bool IsInKnockedDownState => _stateController.State == EnemyState.KnockedDown;
    protected override bool IsInHurtState => _stateController.State == EnemyState.Hurt;
    protected override bool IsInDyingState => _stateController.State == EnemyState.Dying;
    protected override bool IsInAttackingState => _stateController.State == EnemyState.Attacking;
    protected override void FireKnockdownCompleted() => _stateController.Fire(EnemyTrigger.KnockdownCompleted);
    protected override void FireHurtCompleted() => _stateController.Fire(EnemyTrigger.HurtCompleted);
    protected override void FireDeathCompleted() => _stateController.Fire(EnemyTrigger.DeathCompleted);
    protected override void FireAttackCompleted()
    {
        _ai.AttackCooldown = 1.5f;
        _stateController.Fire(EnemyTrigger.AttackCompleted);
    }

    public Entity Target { get; set; }
    public IReadOnlyList<PickupSpawnDef> Drops { get; set; }
    public IReadOnlyList<PickupSpawnDef> CreateDrops() => Drops ?? [];
    public float AttackRange { get; set; } = 70f;
    public float MinChaseDistance { get; set; } = 60f;

    private readonly MoveData _attackMove = new()
    {
        AnimationKey = EnemySprite.AnimationAttack1,
        Damage = 5,
        Strength = AttackStrength.Light,
        AttackSfx = SfxId.EnemyAttackSwing,
        ImpactSfx = SfxId.HitLight,
        FrameHitboxes = new()
        {
            [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
        }
    };

    public MoveData AttackMove => EquippedWeapon?.SwingMove ?? _attackMove;

    public EnemyEntity(string name, Vector2 position, float scale, AnimatedSprite sprite, AudioService audio, LevelDirector director)
        : base(name, position, 48, 60, sprite, scale, 30, new(EnemySprite.AnimationIdle, EnemySprite.AnimationRun, EnemySprite.AnimationHurt, EnemySprite.AnimationFall, EnemySprite.AnimationDie, EnemySprite.AnimationGetUp), audio)
    {
        Speed = 120f;
        SpriteRenderer.SetColor(Color.Red);
        Faction = Faction.Enemy;
        _ai = new EnemyAI(AttackRange, MinChaseDistance);
        _stateController = CreateStateController();
        _director = director;
    }

    public override bool CanTakeDamage() =>
        HealthComponent.IsAlive && _stateController.State != EnemyState.KnockedDown;

    public override void OnDeath() => _stateController.Fire(EnemyTrigger.Die);

    public override void OnKnockdown(DamageInfo info)
    {
        LastImpactSfx = info.ImpactSfx;
        _stateController.Fire(EnemyTrigger.TakeKnockdown);
    }

    public override void OnHit(DamageInfo info)
    {
        LastImpactSfx = info.ImpactSfx;
        _stateController.Fire(EnemyTrigger.TakeDamage);
    }

    protected virtual StateMachineController<EnemyState, EnemyTrigger> CreateStateController() => EnemyStateMachine.Create(new()
    {
        OnIdleEntry = () => SpriteRenderer.SetAnimation(Animations.Idle),
        OnChasingEntry = () => SpriteRenderer.SetAnimation(Animations.Run),
        OnAttackingEntry = () =>
        {
            CurrentMove = AttackMove;
            FrameTracker.Reset();
            PlayAnimation(AttackMove.AnimationKey);
            if (AttackMove.AttackSfx.HasValue)
                Audio.PlaySfx(AttackMove.AttackSfx.Value);
        },
        OnAttackingExit = Callbacks.OnAttackingExit,
        OnHurtEntry = () =>
        {
            PlayAnimation(Animations.Hurt);
            if (LastImpactSfx.HasValue)
                Audio.PlaySfx(LastImpactSfx.Value);
            Audio.PlaySfx(SfxId.EnemyHurt);
        },
        OnHurtExit = Callbacks.OnHurtExit,
        OnKnockdownEntry = () =>
        {
            KnockdownPhase = KnockdownPhase.Falling;
            UnequipWeapon();
            PlayAnimation(Animations.Fall);
            if (LastImpactSfx.HasValue)
                Audio.PlaySfx(LastImpactSfx.Value);
            Audio.PlaySfx(SfxId.Knockdown);
        },
        OnKnockdownExit = Callbacks.OnKnockdownExit,
        OnDyingEntry = () =>
        {
            UnequipWeapon();
            PlayAnimation(Animations.Die);
            Audio.PlaySfx(SfxId.EnemyDeath);
        },
        OnDyingExit = Callbacks.OnDyingExit,
        OnDeadEntry = Callbacks.OnDeadEntry,
        OnEnteringEntry = () =>
        {
            SpriteRenderer.SetAnimation(Animations.Run);
        },
        OnEnteringExit = () =>
        {
            _spawnWalkDirection = Vector2.Zero;
            _spawnWalkTargetX = 0f;
        }
    });

    public void SetSpawnWalkData(Vector2 direction, float targetX)
    {
        _spawnWalkDirection = direction;
        _spawnWalkTargetX = targetX;
        _stateController?.Fire(EnemyTrigger.StartEntering);
    }

    public override void Update(GameTime gameTime)
    {
        if (TryHandleIncapacitatedUpdate(gameTime)) return;

        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_stateController.State == EnemyState.Entering)
        {
            Position += _spawnWalkDirection * deltaSeconds * Speed;
            bool passedTarget = _spawnWalkDirection.X > 0
                ? Position.X >= _spawnWalkTargetX
                : Position.X <= _spawnWalkTargetX;
            if (passedTarget)
                _stateController.Fire(EnemyTrigger.SpawnWalkCompleted);
            AdvanceFrameAndRegisterHitboxes(gameTime);
            return;
        }

        bool isIdleOrChasing = _stateController.State is EnemyState.Idle or EnemyState.Chasing;

        if (Target is not null)
        {
            ref readonly var world = ref _director.CurrentWorld;
            float halfW = Width * 0.5f;
            float halfH = Height * 0.5f;
            var action = _ai.Update(Position, halfW, halfH, world, isIdleOrChasing, deltaSeconds);

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
                Direction = Mover.UpdateFacingDirection(SpriteRenderer, new Vector2(_ai.NewFacingX, 0), Direction);
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
        _stateController = CreateStateController();
        _ai.Reset();
        Target = target;
        SpriteRenderer.SetColor(Color.Red);
        _spawnWalkTargetX = 0f;
        _spawnWalkDirection = Vector2.Zero;
        Drops = null;
    }

    public override void DrawDebug(DebugDrawContext context)
    {
        base.DrawDebug(context);

        var force = _ai.Force;
        var color = force switch
        {
            DominantForce.Avoid => Color.Red,
            DominantForce.Separate => Color.Orange,
            DominantForce.Seek => Color.Green,
            DominantForce.Bounds => Color.Blue,
            _ => Color.AntiqueWhite
        };

        context.SpriteBatch.DrawRectangle(Frame, color);

        context.SpriteBatch.DrawCircle(Position, 50f, 16, Color.Yellow * 0.3f, 1f);
        context.SpriteBatch.DrawCircle(Position, 90f, 16, Color.Cyan * 0.3f, 1f);

        var label = force switch
        {
            DominantForce.Avoid => "AVOID",
            DominantForce.Separate => "SEP",
            DominantForce.Seek => "SEEK",
            DominantForce.Bounds => "BOUNDS",
            _ => ""
        };
        if (label.Length > 0)
        {
            var textSize = context.Font.MeasureString(label);
            context.SpriteBatch.DrawString(context.Font, label,
                new Vector2(Position.X - textSize.X * 0.5f, Position.Y - Height * 0.5f - 20f), color);
        }
    }
}