using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Graphics;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Movement;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Core.StateMachines;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.StateMachines;

namespace MonoGameLearning.Game.Entities.Player;

public class PlayerEntity : CombatActorBase, IHudPlayerData, IDamageResponse
{
    private StateMachineController<PlayerState, PlayerTrigger> _stateController;
    private float _invincibilityTimer;
    private MoveData _pendingMove;
    private ThrowableWeaponDef _pendingThrowable;

    public bool IsInvincible => _invincibilityTimer > 0;
    public ThrowableWeaponDef EquippedThrowable => EquippedWeapon as ThrowableWeaponDef;

    /// <summary>Raised mid-swing when a held throwable leaves the hand (def, origin, facing).</summary>
    public event Action<ThrowableWeaponDef, Vector2, FacingDirection> Thrown;

    string IHudPlayerData.Name => Name;
    int IHudPlayerData.Health => HealthComponent.Value;
    int IHudPlayerData.MaxHealth => HealthComponent.MaxHealth;

    private readonly MoveData _attack1Move = new()
    {
        AnimationKey = PlayerSprite.AnimationAttack1,
        Damage = 5,
        Strength = AttackStrength.Light,
        AttackSfx = SfxId.AttackSwing1,
        ImpactSfx = SfxId.HitLight,
        FrameHitboxes = new()
        {
            [1] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
            [2] = [new() { Offset = new Vector2(35, 0), Size = new Point(45, 40) }],
        }
    };

    public MoveData Attack1Move => (EquippedWeapon as MeleeWeaponDef)?.SwingMove ?? _attack1Move;

    /// <summary>The move most recently requested via <see cref="Attack"/> (for subclass state entries).</summary>
    protected MoveData PendingMove => _pendingMove;
    public readonly MoveData Attack2Move = new()
    {
        AnimationKey = PlayerSprite.AnimationAttack2,
        Damage = 8,
        Strength = AttackStrength.Medium,
        AttackSfx = SfxId.AttackSwing2,
        ImpactSfx = SfxId.HitLight,
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
        AttackSfx = SfxId.AttackSwing3,
        ImpactSfx = SfxId.HitHeavy,
        FrameHitboxes = new()
        {
            [2] = [new() { Offset = new Vector2(50, 0), Size = new Point(55, 40) }],
        }
    };

    protected override ActorPhase Phase => _stateController.State switch
    {
        PlayerState.Moving => ActorPhase.Moving,
        PlayerState.Attacking => ActorPhase.Attacking,
        PlayerState.Hurt => ActorPhase.Hurt,
        PlayerState.KnockedDown => ActorPhase.KnockedDown,
        PlayerState.Dying => ActorPhase.Dying,
        PlayerState.Dead => ActorPhase.Dead,
        _ => ActorPhase.Idle,
    };
    protected override void FirePhaseCompleted()
    {
        switch (Phase)
        {
            case ActorPhase.KnockedDown: _stateController.Fire(PlayerTrigger.KnockdownCompleted); break;
            case ActorPhase.Hurt: _stateController.Fire(PlayerTrigger.HurtCompleted); break;
            case ActorPhase.Dying: _stateController.Fire(PlayerTrigger.DeathCompleted); break;
            default: _stateController.Fire(PlayerTrigger.AttackCompleted); break;
        }
    }

    public PlayerEntity(string name, Vector2 position, float scale, AnimatedSprite sprite, AudioService audio)
        : base(name, position, 48, 60, sprite, scale, 100, new(PlayerSprite.AnimationIdle, PlayerSprite.AnimationRun, PlayerSprite.AnimationHurt, PlayerSprite.AnimationFall, PlayerSprite.AnimationDie, PlayerSprite.AnimationGetUp), audio)
    {
        Speed = 200f;
        Faction = Faction.Player;
        _stateController = CreateStateController();
    }

    public override bool CanTakeDamage() =>
        HealthComponent.IsAlive && _invincibilityTimer <= 0 && _stateController.State != PlayerState.KnockedDown;

    public override void OnDeath() => _stateController.Fire(PlayerTrigger.Die);

    public void OnKnockdown(DamageInfo info)
    {
        LastImpactSfx = info.ImpactSfx;
        _invincibilityTimer = 1.5f;
        _stateController.Fire(PlayerTrigger.TakeKnockdown);
    }

    public void OnHit(DamageInfo info)
    {
        LastImpactSfx = info.ImpactSfx;
        _invincibilityTimer = 1.0f;
        _stateController.Fire(PlayerTrigger.TakeDamage);
    }

    protected virtual StateMachineController<PlayerState, PlayerTrigger> CreateStateController() => PlayerStateMachine.Create(new PlayerStateMachineCallbacks
    {
        OnIdleEntry = () => SpriteRenderer.SetAnimation(Animations.Idle),
        OnMovingEntry = () => SpriteRenderer.SetAnimation(Animations.Run),
        OnAttackingEntry = () =>
        {
            CurrentMove = _pendingMove;
            FrameTracker.Reset();
            PlayAnimation(_pendingMove.AnimationKey);
            if (_pendingMove.AttackSfx.HasValue)
                Audio.PlaySfx(_pendingMove.AttackSfx.Value);
        },
        OnAttackingExit = () =>
        {
            // Drop any throw that was interrupted before its spawn frame could fire.
            _pendingThrowable = null;
            AttackingExitImpl();
        },
        OnHurtEntry = () =>
        {
            HurtEntryImpl();
            if (LastImpactSfx.HasValue)
                Audio.PlaySfx(LastImpactSfx.Value);
            Audio.PlaySfx(SfxId.PlayerHurt);
        },
        OnHurtExit = HurtExitImpl,
        OnKnockdownEntry = () =>
        {
            KnockdownEntryImpl();
            if (LastImpactSfx.HasValue)
                Audio.PlaySfx(LastImpactSfx.Value);
            Audio.PlaySfx(SfxId.Knockdown);
        },
        OnKnockdownExit = KnockdownExitImpl,
        OnDyingEntry = () =>
        {
            DyingEntryImpl();
            Audio.PlaySfx(SfxId.PlayerDeath);
        },
        OnDyingExit = DyingExitImpl,
        OnDeadEntry = DeadEntryImpl,
    });

    public override void Update(GameTime gameTime)
    {
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
            if (_stateController.IsInState(PlayerState.Moving))
            {
                Direction = Mover.UpdateFacingDirection(SpriteRenderer, movementDirectionNoDiagonal, Direction);
                Move(movementDirectionNoDiagonal, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
        }

        AdvanceFrameAndRegisterHitboxes(gameTime);
    }

    public void Attack(MoveData move) { _pendingMove = move; _stateController.Fire(PlayerTrigger.AttackStart); }

    /// <summary>
    /// Attack1 input: throws a held throwable (consuming it) or performs the normal punch.
    /// </summary>
    public void PrimaryAttack()
    {
        if (EquippedThrowable is { } throwable)
            Throw(throwable);
        else
            Attack(Attack1Move);
    }

    private void Throw(ThrowableWeaponDef throwable)
    {
        _pendingThrowable = throwable;
        Attack(throwable.ThrowMove);
    }

    protected override void OnFrameAdvanced(int frameIndex)
    {
        if (_pendingThrowable is not { } throwable) return;
        // A stale pending throw must never fire on a later punch/attack.
        if (CurrentMove != throwable.ThrowMove) return;
        if (frameIndex < throwable.SpawnFrame) return;

        _pendingThrowable = null;
        var origin = Position + WeaponDef.ApplyWeaponFacing(throwable.SpawnOffset, Direction);
        Thrown?.Invoke(throwable, origin, Direction);
        // Only consume the weapon actually thrown: a pickup during the windup may have
        // replaced it, and unequipping then would silently destroy the newly acquired weapon.
        if (ReferenceEquals(EquippedWeapon, throwable))
            UnequipWeapon();
    }

    public void Move(Vector2 direction, float deltaTime) =>
        Position += direction * deltaTime * Speed;

    public void Reset(Vector2 position)
    {
        ResetActor(position);
        _pendingThrowable = null;
        _stateController = CreateStateController();
    }

    public void Respawn()
    {
        _invincibilityTimer = 2.5f;
    }

    protected override Color GetDebugFrameColor() => IsInvincible ? Color.Yellow : Color.Blue;
}