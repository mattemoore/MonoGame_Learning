using MonoGameLearning.Core.Entities.Helpers;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class HealthDisplayTests
{
    [Test]
    public void Format_ReturnsHealthSlashMax() =>
        Assert.That(HealthDisplay.Format(30, 30), Is.EqualTo("30/30"));

    [Test]
    public void Format_FullHealth() =>
        Assert.That(HealthDisplay.Format(100, 100), Is.EqualTo("100/100"));

    [Test]
    public void Format_ZeroHealth() =>
        Assert.That(HealthDisplay.Format(0, 100), Is.EqualTo("0/100"));

    [Test]
    public void Format_PartialHealth() =>
        Assert.That(HealthDisplay.Format(6, 18), Is.EqualTo("6/18"));
}