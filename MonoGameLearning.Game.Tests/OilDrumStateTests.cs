using MonoGameLearning.Core.Combat;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class OilDrumBehaviorTests
{
    [Test]
    public void CanTakeDamage_WhenAliveAndNotStunned_ReturnsTrue()
    {
        var behavior = new OilDrumBehavior();
        Assert.That(behavior.CanTakeDamage(true), Is.True);
    }

    [Test]
    public void CanTakeDamage_WhenDead_ReturnsFalse()
    {
        var behavior = new OilDrumBehavior();
        Assert.That(behavior.CanTakeDamage(false), Is.False);
    }

    [Test]
    public void CanTakeDamage_WhenStunned_ReturnsFalse()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();
        Assert.That(behavior.CanTakeDamage(true), Is.False);
    }

    [Test]
    public void ApplyStun_SetsIsHitStunned()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();
        Assert.That(behavior.IsHitStunned, Is.True);
    }

    [Test]
    public void Update_DuringStun_DoesNotEndBeforeDuration()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();

        bool ended = behavior.Update(0.1f);
        Assert.That(behavior.IsHitStunned, Is.True);
        Assert.That(ended, Is.False);
    }

    [Test]
    public void Update_AfterDuration_EndsStun()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();

        bool ended = behavior.Update(0.3f);
        Assert.That(behavior.IsHitStunned, Is.False);
        Assert.That(ended, Is.True);
    }

    [Test]
    public void Update_NotStunned_ReturnsFalse()
    {
        var behavior = new OilDrumBehavior();
        Assert.That(behavior.Update(1.0f), Is.False);
    }

    [Test]
    public void Reset_ClearsHitStun()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();
        behavior.Reset();
        Assert.That(behavior.IsHitStunned, Is.False);
        Assert.That(behavior.CanTakeDamage(true), Is.True);
    }

    [Test]
    public void Update_ExactBoundary_EndsStun()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();

        bool ended = behavior.Update(0.3f);
        Assert.That(ended, Is.True);
        Assert.That(behavior.IsHitStunned, Is.False);
    }

    [Test]
    public void Update_PartialDurationThenRemaining_EndsStun()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();
        behavior.Update(0.2f);
        bool ended = behavior.Update(0.2f);
        Assert.That(ended, Is.True);
        Assert.That(behavior.IsHitStunned, Is.False);
    }

    [Test]
    public void Update_ZeroDelta_DoesNotEndStun()
    {
        var behavior = new OilDrumBehavior();
        behavior.ApplyStun();
        bool ended = behavior.Update(0f);
        Assert.That(ended, Is.False);
        Assert.That(behavior.IsHitStunned, Is.True);
    }
}

[TestFixture]
public class OilDrumDamageTests
{
    [Test]
    public void GetEffectiveDamage_Light_ReturnsTwo()
    {
        Assert.That(OilDrumDamage.GetEffectiveDamage(AttackStrength.Light), Is.EqualTo(2));
    }

    [Test]
    public void GetEffectiveDamage_Medium_ReturnsThree()
    {
        Assert.That(OilDrumDamage.GetEffectiveDamage(AttackStrength.Medium), Is.EqualTo(3));
    }

    [Test]
    public void GetEffectiveDamage_Heavy_ReturnsSix()
    {
        Assert.That(OilDrumDamage.GetEffectiveDamage(AttackStrength.Heavy), Is.EqualTo(6));
    }
}