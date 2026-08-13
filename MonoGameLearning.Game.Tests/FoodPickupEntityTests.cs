using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.Entities.Pickups;

namespace MonoGameLearning.Game.Tests;

internal sealed class HealTrackerEntity : IDamageable, IDamageResponse
{
    private int _health;
    private readonly int _maxHealth;

    public HealTrackerEntity(int maxHealth)
    {
        _maxHealth = maxHealth;
        _health = maxHealth;
    }

    public string Name => "HealTracker";
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public bool IsAlive => _health > 0;
    public Faction Faction => Faction.Player;
    public event EventHandler Died = delegate { };
    public int LastHealAmount { get; private set; }

    public void TakeDamage(Core.Combat.DamageInfo info) { }
    bool IDamageResponse.IsAlive => IsAlive;
    bool IDamageResponse.CanTakeDamage() => IsAlive;
    void IDamageResponse.ReduceHealth(int amount) => _health = Math.Max(0, _health - amount);
    void IDamageResponse.OnDeath() => Died?.Invoke(this, EventArgs.Empty);
    void IDamageResponse.OnKnockdown(Core.Combat.DamageInfo info) { }
    void IDamageResponse.OnHit(Core.Combat.DamageInfo info) { }
    void IDamageable.Heal(int amount) { if (!IsAlive) return; LastHealAmount = amount; _health = Math.Min(_maxHealth, _health + amount); }
}

[TestFixture]
public class FoodPickupEntityTests
{
    private static void ApplyHeal(IDamageable target) => target.Heal(FoodPickupEntity.HealAmount);

    [Test]
    public void OnPickup_HealsPlayerByHealAmount()
    {
        var target = new HealTrackerEntity(100);
        ((IDamageResponse)target).ReduceHealth(50);
        ApplyHeal(target);

        Assert.That(target.Health, Is.EqualTo(65));
        Assert.That(target.LastHealAmount, Is.EqualTo(FoodPickupEntity.HealAmount));
    }

    [Test]
    public void OnPickup_AtMaxHealth_NoChange()
    {
        var target = new HealTrackerEntity(100);
        ApplyHeal(target);

        Assert.That(target.Health, Is.EqualTo(100));
    }

    [Test]
    public void OnPickup_DoesNotExceedMaxHealth()
    {
        var target = new HealTrackerEntity(100);
        ((IDamageResponse)target).ReduceHealth(10);
        ApplyHeal(target);

        Assert.That(target.Health, Is.EqualTo(100));
    }

    [Test]
    public void OnPickup_DeadTarget_NoHeal()
    {
        var target = new HealTrackerEntity(100);
        ((IDamageResponse)target).ReduceHealth(100);
        Assert.That(target.IsAlive, Is.False);

        ApplyHeal(target);

        Assert.That(target.Health, Is.EqualTo(0));
    }
}