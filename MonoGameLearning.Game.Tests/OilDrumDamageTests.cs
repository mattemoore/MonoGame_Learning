using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.Entities.Props;

namespace MonoGameLearning.Game.Tests;

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