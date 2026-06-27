using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Tests;

public class TestDamageableEntity(string name, Vector2 position, int width, int height) : Entity(name, position, width, height), IDamageable, ICollisionActor
{
    private readonly Health _health = new(6);
    private bool _isHitStunned;
    public IShapeF Bounds => Frame;
    public Faction Faction => Faction.Neutral;
    public int Health => _health.Value;
    public int MaxHealth => _health.MaxHealth;
    public bool IsAlive => _health.IsAlive;
    public event EventHandler Died = delegate { };
    public event Action<Entity> Destroyed = delegate { };

    public void TakeDamage(DamageInfo info)
    {
        if (!_health.IsAlive || _isHitStunned) return;

        int effective = info.Strength switch { AttackStrength.Heavy => 6, AttackStrength.Medium => 3, _ => 2 };
        _health.Subtract(effective);

        if (!_health.IsAlive)
            Destroyed?.Invoke(this);
        else
            _isHitStunned = true;
    }

    public void ClearHitStun() => _isHitStunned = false;

    public bool CanTakeDamage => _health.IsAlive && !_isHitStunned;
    public void OnCollision(CollisionEventArgs collisionInfo) { }

    bool IDamageable.CanTakeDamage() => _health.IsAlive && !_isHitStunned;
    void IDamageable.ReduceHealth(int amount) => _health.Subtract(amount);
    void IDamageable.OnDeath() => Destroyed?.Invoke(this);
    void IDamageable.OnKnockdown(DamageInfo info) { }
    void IDamageable.OnHit(DamageInfo info) { }
}

[TestFixture]
public class OilDrumEntityBehaviorTests
{
    private TestDamageableEntity _entity;

    [SetUp]
    public void Setup() => _entity = new("drum", Vector2.Zero, 50, 50);

    [Test]
    public void InitialState_CanBeDamaged()
    {
        Assert.That(_entity.CanTakeDamage, Is.True);
    }

    [Test]
    public void TakeDamage_SetsHitStun()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Light });
        Assert.That(_entity.CanTakeDamage, Is.False);
    }

    [Test]
    public void TakeDamage_DuringHitStun_IsRejected()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Light });
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Light });
        Assert.That(_entity.Health, Is.EqualTo(4));
    }

    [Test]
    public void ClearHitStun_AllowsFurtherDamage()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Light });
        _entity.ClearHitStun();
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Light });
        Assert.That(_entity.Health, Is.EqualTo(2));
    }

    [Test]
    public void DestroyedEvent_Fires_WhenHealthDepleted()
    {
        bool destroyed = false;
        _entity.Destroyed += _ => destroyed = true;
        _entity.TakeDamage(new DamageInfo { Amount = 50, Strength = AttackStrength.Heavy });
        Assert.That(destroyed, Is.True);
    }

    [Test]
    public void DestroyedEvent_DoesNotFire_WhenHealthRemains()
    {
        bool destroyed = false;
        _entity.Destroyed += _ => destroyed = true;
        _entity.TakeDamage(new DamageInfo { Amount = 2, Strength = AttackStrength.Light });
        Assert.That(destroyed, Is.False);
    }

    [Test]
    public void DeadEntity_RejectsFurtherDamage()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 50, Strength = AttackStrength.Heavy });
        int healthBefore = _entity.Health;
        _entity.TakeDamage(new DamageInfo { Amount = 50, Strength = AttackStrength.Heavy });
        Assert.That(_entity.Health, Is.EqualTo(healthBefore));
    }

    [Test]
    public void LightDamage_ReducesByTwo()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Light });
        Assert.That(_entity.Health, Is.EqualTo(4));
    }

    [Test]
    public void MediumDamage_ReducesByThree()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Medium });
        Assert.That(_entity.Health, Is.EqualTo(3));
    }

    [Test]
    public void HeavyDamage_Destroys()
    {
        _entity.TakeDamage(new DamageInfo { Amount = 0, Strength = AttackStrength.Heavy });
        Assert.That(_entity.Health, Is.EqualTo(0));
        Assert.That(_entity.IsAlive, Is.False);
    }
}