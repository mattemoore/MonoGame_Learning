using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Tests;

public class TestEnemyEntity : Entity, IDamageable, ICollisionActor
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public EnemyStateController StateController { get; }
    public Entity? Target { get; set; }
    public float AttackRange { get; set; } = 70f;
    public float MinChaseDistance { get; set; } = 60f;
    public float AttackCooldown { get; set; }
    public float AttackDelayTimer { get; set; }
    public int KnockdownPhase { get; set; }
    private const float AttackDelayDuration = 1.0f;
    private readonly Health _health;

    public Faction Faction { get; protected set; }
    public int Health => _health.Value;
    public int MaxHealth => _health.MaxHealth;
    public bool IsAlive => _health.IsAlive;
    public event EventHandler Died = null!;
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; } = 120f;
    public bool CanBeDamaged => _health.IsAlive && StateController.State != EnemyState.KnockedDown;

    public TestEnemyEntity(string name, Vector2 position, int width, int height)
        : base(name, position, width, height)
    {
        _health = new(30);
        StateController = new EnemyStateController(new()
        {
            OnIdleEntry = () => { },
            OnChasingEntry = () => { },
            OnAttackingEntry = () => { },
            OnAttackingExit = () => { },
            OnHurtEntry = () => { },
            OnHurtExit = () => { },
            OnKnockdownEntry = () => { },
            OnKnockdownExit = () => { },
            OnDyingEntry = () => { },
            OnDyingExit = () => { },
            OnDeadEntry = () => RaiseDied()
        });
    }

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);

    public bool CanTakeDamage => _health.IsAlive && StateController.State != EnemyState.KnockedDown;
    void IDamageable.ReduceHealth(int amount) => _health.Subtract(amount);

    bool IDamageable.CanTakeDamage() => _health.IsAlive && StateController.State != EnemyState.KnockedDown;
    void IDamageable.OnDeath() => StateController.Fire(EnemyTrigger.Die);
    void IDamageable.OnKnockdown(DamageInfo info) => StateController.Fire(EnemyTrigger.TakeKnockdown);
    void IDamageable.OnHit(DamageInfo info) => StateController.Fire(EnemyTrigger.TakeDamage);

    private void RaiseDied() => Died?.Invoke(this, EventArgs.Empty);

    public void UpdateAI(float deltaSeconds)
    {
        if (StateController.State is EnemyState.Dead or EnemyState.Dying or EnemyState.Hurt or EnemyState.KnockedDown)
        {
            MovementDirection = Vector2.Zero;
            return;
        }

        if (Target is not null)
        {
            float dx = Target.Position.X - Position.X;
            float distance = Math.Abs(dx);

            if (distance <= AttackRange && StateController.State is EnemyState.Idle or EnemyState.Chasing && AttackCooldown <= 0)
            {
                if (StateController.State == EnemyState.Chasing)
                {
                    StateController.Fire(EnemyTrigger.StopChase);
                    MovementDirection = Vector2.Zero;
                }

                if (AttackDelayTimer <= 0)
                    AttackDelayTimer = AttackDelayDuration;

                AttackDelayTimer -= deltaSeconds;
                if (AttackDelayTimer <= 0)
                {
                    AttackDelayTimer = 0;
                    StateController.Fire(EnemyTrigger.AttackStart);
                }
            }
            else if (distance > AttackRange && StateController.State is EnemyState.Idle or EnemyState.Chasing)
            {
                AttackDelayTimer = 0;
                StateController.Fire(EnemyTrigger.StartChase);
                float direction = dx > 0 ? 1f : -1f;
                MovementDirection = new Vector2(direction, 0);

                if (distance <= MinChaseDistance)
                    MovementDirection = Vector2.Zero;
            }
            else if (StateController.State == EnemyState.Chasing && distance <= AttackRange)
            {
                StateController.Fire(EnemyTrigger.StopChase);
                MovementDirection = Vector2.Zero;
            }
        }

        AttackCooldown = Math.Max(0, AttackCooldown - deltaSeconds);

        if (StateController.State == EnemyState.Chasing && MovementDirection != Vector2.Zero)
            Position += MovementDirection * deltaSeconds * Speed;
    }

    public void CompleteCurrentAnimation()
    {
        if (StateController.State == EnemyState.KnockedDown)
        {
            if (KnockdownPhase == 0)
                KnockdownPhase = 1;
            else
                StateController.Fire(EnemyTrigger.KnockdownCompleted);
            return;
        }

        StateController.Fire(StateController.State switch
        {
            EnemyState.Hurt => EnemyTrigger.HurtCompleted,
            EnemyState.Dying => EnemyTrigger.DeathCompleted,
            _ => TriggerAttackCooldown()
        });
    }

    private EnemyTrigger TriggerAttackCooldown()
    {
        AttackCooldown = 1.5f;
        return EnemyTrigger.AttackCompleted;
    }
}

[TestFixture]
public class EnemyEntityBehaviorTests
{
    private TestEnemyEntity _entity;
    private TestEnemyEntity _target;

    [SetUp]
    public void Setup()
    {
        _entity = new("enemy", new Vector2(0, 0), 40, 60);
        _target = new("target", new Vector2(200, 0), 40, 60);
        _entity.Target = _target;
    }

    [Test]
    public void Entity_StartsAlive_WithFullHealth()
    {
        Assert.That(_entity.IsAlive, Is.True);
        Assert.That(_entity.Health, Is.EqualTo(_entity.MaxHealth));
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 10 });
        Assert.That(_entity.Health, Is.EqualTo(20));
    }

    [Test]
    public void TakeDamage_ToZero_TriggersDeath()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 30 });
        Assert.That(_entity.Health, Is.EqualTo(0));
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Dying));
    }

    [Test]
    public void TakeDamage_Overkill_ClampsToZero()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 100 });
        Assert.That(_entity.Health, Is.EqualTo(0));
    }

    [Test]
    public void DeadEntity_IgnoresFurtherDamage()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 30 });
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Dying));

        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Dead));

        _entity.TakeDamage(new DamageInfo { Amount = 10 });
        Assert.That(_entity.Health, Is.EqualTo(0));
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Dead));
    }

    [Test]
    public void TakeDamage_Knockdown_TransitionsToKnockedDown()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 5, Knockdown = true });
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void TakeDamage_WithoutKnockdown_TransitionsToHurt()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 5 });
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Hurt));
    }

    [Test]
    public void WhileKnockedDown_TakeDamage_IsIgnored()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 5, Knockdown = true });
        int healthBefore = _entity.Health;
        _entity.TakeDamage(new DamageInfo { Amount = 5 });
        Assert.That(_entity.Health, Is.EqualTo(healthBefore));
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.KnockedDown));
    }

    [Test]
    public void ChaseAI_MovesTowardTarget()
    {
        _entity.UpdateAI(1.0f);
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Chasing));
        Assert.That(_entity.MovementDirection.X, Is.GreaterThan(0));
    }

    [Test]
    public void ChaseAI_MovesTowardTarget_FromLeft()
    {
        _entity = new("enemy", new Vector2(200, 0), 40, 60);
        _entity.Target = _target;
        _target.Position = new Vector2(100, 0);
        _entity.UpdateAI(1.0f);
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Chasing));
        Assert.That(_entity.MovementDirection.X, Is.LessThan(0));
    }

    [Test]
    public void CombatAI_TriggersAttack_WhenTargetInRange()
    {
        _target.Position = new Vector2(50, 0);
        _entity.UpdateAI(1.0f);
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Attacking));
    }

    [Test]
    public void EnemyStops_AtMinChaseDistance()
    {
        _target.Position = new Vector2(_entity.MinChaseDistance - 10, 0);
        _entity.UpdateAI(1.0f);
        Assert.That(_entity.MovementDirection, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void AttackCooldown_PreventsImmediateReAttack()
    {
        _target.Position = new Vector2(50, 0);
        _entity.UpdateAI(1.5f);
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Attacking));

        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Idle));
        Assert.That(_entity.AttackCooldown, Is.EqualTo(1.5f));

        _entity.UpdateAI(0f);
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void Knockdown_TwoPhase_WorksCorrectly()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 5, Knockdown = true });
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.KnockedDown));

        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.KnockdownPhase, Is.EqualTo(1));

        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void Death_CanBeCompleted_ToDead()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 30 });
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Dying));

        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Dead));
    }

    [Test]
    public void Hurt_CanBeCompleted_ToIdle()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 5 });
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Hurt));

        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.StateController.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void DiedEvent_Fires_OnDeath()
    {
        bool died = false;
        _entity.Died += (_, _) => died = true;

        _entity.TakeDamage(new DamageInfo { Amount = 30 });
        _entity.CompleteCurrentAnimation();
        Assert.That(died, Is.True);
    }

    [Test]
    public void CannotTakeDamage_WhenDead()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 30 });
        _entity.CompleteCurrentAnimation();
        Assert.That(_entity.CanBeDamaged, Is.False);
    }

    [Test]
    public void CannotTakeDamage_WhenKnockedDown()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 5, Knockdown = true });
        Assert.That(_entity.CanBeDamaged, Is.False);
    }
}
