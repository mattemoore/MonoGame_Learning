using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Animation;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class HandAnchorTableTests
{
    private static HandAnchorTable Table() => new(
        ("idle", [new Vector2(1, 2), new Vector2(3, 4)]),
        ("run", [new Vector2(5, 6)]));

    [Test]
    public void Resolve_KnownKeyAndFrame_ReturnsThatFrame()
    {
        Assert.That(Table().TryResolve("idle", 1, out var hand), Is.True);
        Assert.That(hand, Is.EqualTo(new Vector2(3, 4)));
    }

    [Test]
    public void Resolve_FramePastRun_WrapsModulo()
    {
        Assert.That(Table().TryResolve("idle", 3, out var hand), Is.True);
        Assert.That(hand, Is.EqualTo(new Vector2(3, 4)), "A looping animation passes frames past the run length");
    }

    [Test]
    public void Resolve_UnknownKey_ReturnsFalseAndZero()
    {
        Assert.That(Table().TryResolve("hurt", 0, out var hand), Is.False);
        Assert.That(hand, Is.EqualTo(Vector2.Zero));
    }

    [Test]
    public void Resolve_NullKey_ReturnsFalse() =>
        Assert.That(Table().TryResolve(null, 0, out _), Is.False);

    [Test]
    public void Resolve_EmptyFrameArray_IsSkipped()
    {
        var table = new HandAnchorTable(("idle", []), ("run", [new Vector2(9, 9)]));

        Assert.That(table.TryResolve("idle", 0, out _), Is.False, "An empty run must not match");
        Assert.That(table.TryResolve("run", 0, out var hand), Is.True);
        Assert.That(hand, Is.EqualTo(new Vector2(9, 9)));
    }
}
