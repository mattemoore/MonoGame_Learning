using System;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Entities.Prop;
using NUnit.Framework;

namespace MonoGameLearning.Game.Tests;

internal sealed class DefaultDamageDouble : IDamageable, IDamageResponse
{
    private int _health = 100;
    public string Name => "DefaultDamageDouble";
    public int Health => _health;
    public int MaxHealth => 100;
    public bool IsAlive => _health > 0;
    public event EventHandler Died = delegate { };
    public int DeathCount { get; private set; }

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);
    public void Heal(int amount) => _health = Math.Min(100, _health + amount);

    bool IDamageResponse.IsAlive => IsAlive;
    void IDamageResponse.ReduceHealth(int amount) => _health = Math.Max(0, _health - amount);
    void IDamageResponse.OnDeath() { DeathCount++; Died?.Invoke(this, EventArgs.Empty); }
}

[TestFixture]
public class CombatInterfaceTests
{
    [Test]
    public void IDamageable_NoLongerRequiresFaction()
    {
        // DefaultDamageDouble implements IDamageable without a Faction member — compile-time proof.
        IDamageable target = new DefaultDamageDouble();
        Assert.That(target, Is.Not.Null);
    }

    [Test]
    public void KnockdownDamage_DefaultOnKnockdown_DoesNotThrow()
    {
        var target = new DefaultDamageDouble();

        Assert.DoesNotThrow(() => target.TakeDamage(new DamageInfo { Amount = 20, Knockdown = true }));
        Assert.That(target.Health, Is.EqualTo(80));
    }

    [Test]
    public void NormalDamage_DefaultOnHit_DoesNotThrow()
    {
        var target = new DefaultDamageDouble();

        Assert.DoesNotThrow(() => target.TakeDamage(new DamageInfo { Amount = 20, Knockdown = false }));
        Assert.That(target.Health, Is.EqualTo(80));
    }

    [Test]
    public void CanTakeDamage_Default_ReflectsIsAlive()
    {
        var target = new DefaultDamageDouble();
        Assert.That(((IDamageResponse)target).CanTakeDamage(), Is.True);

        ((IDamageResponse)target).ReduceHealth(100);
        Assert.That(((IDamageResponse)target).CanTakeDamage(), Is.False);
    }

    [Test]
    public void PropBase_NoLongerExposesFaction()
    {
        Assert.That(typeof(PropBase).GetProperty(nameof(Faction)), Is.Null);
    }

    [Test]
    public void CombatActorBase_StillExposesFaction()
    {
        Assert.That(typeof(CombatActorBase).GetProperty(nameof(Faction)), Is.Not.Null);
    }
}