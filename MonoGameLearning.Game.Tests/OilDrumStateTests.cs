using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;
using MonoGameLearning.Game.Entities.Props;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class OilDrumStateControllerTests
{
    private OilDrumStateController _controller;

    [SetUp]
    public void Setup() => _controller = new OilDrumStateController();

    [Test]
    public void InitialState_ShouldBeNormal() =>
        Assert.That(_controller.State, Is.EqualTo(OilDrumState.Normal));

    [Test]
    public void FromNormal_Hit_TransitionsToHitStun()
    {
        _controller.Fire(OilDrumTrigger.Hit);
        Assert.That(_controller.State, Is.EqualTo(OilDrumState.HitStun));
    }

    [Test]
    public void FromHitStun_HitStunCompleted_TransitionsToNormal()
    {
        _controller.Fire(OilDrumTrigger.Hit);
        _controller.Fire(OilDrumTrigger.HitStunCompleted);
        Assert.That(_controller.State, Is.EqualTo(OilDrumState.Normal));
    }

    [Test]
    public void FromHitStun_Hit_IsIgnored()
    {
        _controller.Fire(OilDrumTrigger.Hit);
        _controller.Fire(OilDrumTrigger.Hit);
        Assert.That(_controller.State, Is.EqualTo(OilDrumState.HitStun));
    }

    [Test]
    public void FromNormal_HitStunCompleted_IsIgnored()
    {
        _controller.Fire(OilDrumTrigger.HitStunCompleted);
        Assert.That(_controller.State, Is.EqualTo(OilDrumState.Normal));
    }

    [Test]
    public void CanFire_ReturnsTrue_ForValidTransition()
    {
        Assert.That(_controller.CanFire(OilDrumTrigger.Hit), Is.True);
    }

    [Test]
    public void CanFire_ReturnsTrue_ForIgnoredTrigger_WhenInHitStun()
    {
        _controller.Fire(OilDrumTrigger.Hit);
        Assert.That(_controller.CanFire(OilDrumTrigger.Hit), Is.True);
    }

    [Test]
    public void CanFire_ReturnsTrue_ForIgnoredTrigger()
    {
        Assert.That(_controller.CanFire(OilDrumTrigger.HitStunCompleted), Is.True);
    }

    [Test]
    public void IsInState_ReturnsTrue_WhenInState()
    {
        _controller.Fire(OilDrumTrigger.Hit);
        Assert.That(_controller.IsInState(OilDrumState.HitStun), Is.True);
    }

    [Test]
    public void IsInState_ReturnsFalse_WhenNotInState() =>
        Assert.That(_controller.IsInState(OilDrumState.HitStun), Is.False);

    [Test]
    public void NormalEntryCallback_NotInvoked_OnInitialization()
    {
        bool entryInvoked = false;
        _ = new OilDrumStateController(new() { OnNormalEntry = () => entryInvoked = true });
        Assert.That(entryInvoked, Is.False);
    }

    [Test]
    public void NormalEntryCallback_IsInvoked_OnReturnFromHitStun()
    {
        bool entryInvoked = false;
        var controller = new OilDrumStateController(new() { OnNormalEntry = () => entryInvoked = true });
        entryInvoked = false;
        controller.Fire(OilDrumTrigger.Hit);
        controller.Fire(OilDrumTrigger.HitStunCompleted);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void HitStunEntryCallback_IsInvoked_OnHit()
    {
        bool entryInvoked = false;
        var controller = new OilDrumStateController(new() { OnHitStunEntry = () => entryInvoked = true });
        controller.Fire(OilDrumTrigger.Hit);
        Assert.That(entryInvoked, Is.True);
    }

    [Test]
    public void HitStunEntryCallback_NotInvoked_OnIgnoredHit()
    {
        int callCount = 0;
        var controller = new OilDrumStateController(new() { OnHitStunEntry = () => callCount++ });
        controller.Fire(OilDrumTrigger.Hit);
        controller.Fire(OilDrumTrigger.Hit);
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Fire_DoesNothing_ForInvalidTransition()
    {
        _controller.Fire(OilDrumTrigger.Hit);
        _controller.Fire(OilDrumTrigger.Hit);
        Assert.That(_controller.State, Is.EqualTo(OilDrumState.HitStun));
    }
}

public class TestDamageableEntity : PropEntity
{
    private readonly OilDrumStateController _stateController;
    public int Health { get; private set; } = 6;
    public bool IsAlive => Health > 0;
    public event Action<TestDamageableEntity>? Destroyed;

    public TestDamageableEntity(string name, Vector2 position, int width, int height)
        : base(name, position, width, height)
    {
        _stateController = new();
    }

    public override void TakeDamage(int amount, bool knockdown = false)
    {
        if (!IsAlive || _stateController.State == OilDrumState.HitStun) return;

        Health -= amount switch
        {
            >= 12 => 6,
            >= 8 => 3,
            _ => 2
        };

        if (Health <= 0)
        {
            Destroyed?.Invoke(this);
            return;
        }

        _stateController.Fire(OilDrumTrigger.Hit);
    }

    public void UpdateHitStun(float deltaSeconds)
    {
        if (!IsAlive || !_stateController.IsInState(OilDrumState.HitStun)) return;
        _stateController.Fire(OilDrumTrigger.HitStunCompleted);
    }

    public bool CanTakeDamage => IsAlive && _stateController.State != OilDrumState.HitStun;
}

[TestFixture]
public class OilDrumEntityBehaviorTests
{
    private TestDamageableEntity _entity;

    [SetUp]
    public void Setup() => _entity = new("drum", Vector2.Zero, 50, 50);

    [Test]
    public void TakeDamage_FirstHit_ReducesHealth()
    {
        _entity.TakeDamage(5);
        Assert.That(_entity.Health, Is.EqualTo(4));
    }

    [Test]
    public void HitStunState_PreventsDoubleHit()
    {
        _entity.TakeDamage(5);
        int healthAfterFirst = _entity.Health;
        _entity.TakeDamage(5);
        Assert.That(_entity.Health, Is.EqualTo(healthAfterFirst));
    }

    [Test]
    public void HitStunCompleted_AllowsNextHit()
    {
        _entity.TakeDamage(5);
        _entity.UpdateHitStun(0);
        int healthAfterFirst = _entity.Health;
        _entity.TakeDamage(5);
        Assert.That(_entity.Health, Is.LessThan(healthAfterFirst));
    }

    [Test]
    public void Destroyed_WhenHealthDepleted()
    {
        _entity.TakeDamage(50);
        Assert.That(_entity.Health, Is.LessThanOrEqualTo(0));
        Assert.That(_entity.IsAlive, Is.False);
    }

    [Test]
    public void DeadEntity_IgnoresFurtherDamage()
    {
        _entity.TakeDamage(50);
        int healthAfterDeath = _entity.Health;
        _entity.TakeDamage(5);
        Assert.That(_entity.Health, Is.EqualTo(healthAfterDeath));
    }

    [Test]
    public void DamageScaling_LowDamage_ReducesBy2()
    {
        _entity.TakeDamage(3);
        Assert.That(_entity.Health, Is.EqualTo(4));
    }

    [Test]
    public void DamageScaling_HighDamage_ReducesBy6()
    {
        _entity.TakeDamage(12);
        Assert.That(_entity.Health, Is.EqualTo(0));
    }

    [Test]
    public void DamageScaling_MediumDamage_ReducesBy3()
    {
        _entity.TakeDamage(8);
        Assert.That(_entity.Health, Is.EqualTo(3));
    }

    [Test]
    public void AlreadyDead_CanTakeDamage_ReturnsFalse()
    {
        _entity.TakeDamage(50);
        Assert.That(_entity.IsAlive, Is.False);
        Assert.That(_entity.CanTakeDamage, Is.False);
    }
}