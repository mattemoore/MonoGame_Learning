using MonoGameLearning.Core.Entities.Components;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class HealthTests
{
    [Test]
    public void ToDisplayString_ReturnsHealthSlashMax() =>
        Assert.That(new Health(30).ToDisplayString(), Is.EqualTo("30/30"));

    [Test]
    public void ToDisplayString_FullHealth() =>
        Assert.That(new Health(100).ToDisplayString(), Is.EqualTo("100/100"));

    [Test]
    public void ToDisplayString_ZeroHealth()
    {
        var h = new Health(100);
        h.Subtract(100);
        Assert.That(h.ToDisplayString(), Is.EqualTo("0/100"));
    }

    [Test]
    public void ToDisplayString_PartialHealth()
    {
        var h = new Health(18);
        h.Subtract(12);
        Assert.That(h.ToDisplayString(), Is.EqualTo("6/18"));
    }

    [Test]
    public void Add_BelowMax_IncreasesValue()
    {
        var h = new Health(100);
        h.Subtract(30);
        h.Add(15);
        Assert.That(h.Value, Is.EqualTo(85));
    }

    [Test]
    public void Add_AboveMax_ClampsToMax()
    {
        var h = new Health(100);
        h.Subtract(10);
        h.Add(20);
        Assert.That(h.Value, Is.EqualTo(100));
    }

    [Test]
    public void Add_Zero_NoChange()
    {
        var h = new Health(100);
        h.Subtract(30);
        h.Add(0);
        Assert.That(h.Value, Is.EqualTo(70));
    }
}