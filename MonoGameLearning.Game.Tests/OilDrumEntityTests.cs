using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.Entities.Props;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class OilDrumEntityTests
{
    private static TestableOilDrumEntity CreateDrum() => new("drum");

    private static DamageInfo Strike(AttackStrength strength, int amount = 0) =>
        new() { Amount = amount, Strength = strength };

    private static void AdvancePastHitStun(TestableOilDrumEntity drum) =>
        drum.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.35f)));

    [Test]
    public void TakeDamage_Heavy_DestroysInOneHit()
    {
        var drum = CreateDrum();

        drum.TakeDamage(Strike(AttackStrength.Heavy));

        Assert.That(drum.IsAliveExposed, Is.False);
        Assert.That(drum.CurrentHealth, Is.Zero);
    }

    [Test]
    public void TakeDamage_Medium_TakesTwoHits()
    {
        var drum = CreateDrum();

        drum.TakeDamage(Strike(AttackStrength.Medium));
        Assert.That(drum.IsAliveExposed, Is.True);
        AdvancePastHitStun(drum);

        drum.TakeDamage(Strike(AttackStrength.Medium));
        Assert.That(drum.IsAliveExposed, Is.False);
    }

    [Test]
    public void TakeDamage_Light_TakesThreeHits()
    {
        var drum = CreateDrum();

        drum.TakeDamage(Strike(AttackStrength.Light));
        AdvancePastHitStun(drum);
        drum.TakeDamage(Strike(AttackStrength.Light));
        Assert.That(drum.IsAliveExposed, Is.True);
        AdvancePastHitStun(drum);

        drum.TakeDamage(Strike(AttackStrength.Light));
        Assert.That(drum.IsAliveExposed, Is.False);
    }

    [Test]
    public void TakeDamage_IgnoresAmount_StrengthMappingDrivesDurability()
    {
        // Durability is tiered by Strength, not Amount: a zero-amount heavy hit
        // still one-shots the 6-HP drum.
        var drum = CreateDrum();
        drum.TakeDamage(Strike(AttackStrength.Heavy, amount: 0));
        Assert.That(drum.IsAliveExposed, Is.False);
    }

    [Test]
    public void SurvivingHit_StunsAndBlocksNextHit()
    {
        var drum = CreateDrum();
        drum.TakeDamage(Strike(AttackStrength.Light));
        Assert.That(drum.CurrentHealth, Is.EqualTo(4));

        drum.TakeDamage(Strike(AttackStrength.Light));
        Assert.That(drum.CurrentHealth, Is.EqualTo(4), "stunned drum must ignore incoming hits");
    }

    [Test]
    public void HitStun_ExpiresAfterDuration_AllowsDamageAgain()
    {
        var drum = CreateDrum();
        drum.TakeDamage(Strike(AttackStrength.Light));
        Assert.That(drum.CurrentHealth, Is.EqualTo(4));

        drum.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.35f)));
        drum.TakeDamage(Strike(AttackStrength.Light));

        Assert.That(drum.CurrentHealth, Is.EqualTo(2));
    }

    [Test]
    public void TakeDamage_DeadDrum_IgnoresHits()
    {
        var drum = CreateDrum();
        drum.TakeDamage(Strike(AttackStrength.Heavy));

        drum.TakeDamage(Strike(AttackStrength.Light));

        Assert.That(drum.CurrentHealth, Is.Zero);
    }
}