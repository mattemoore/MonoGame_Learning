using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class SpriteRendererTests
{
    // Animations advancing beyond their frame count would break hand anchors and weapon
    // swing frames (they wrap/clamp), so the pose clock must be animation-relative and
    // reset at every SetAnimation.

    [Test]
    public void PoseFrame_StartsAtZero_AndCountsFrameChanges()
    {
        var renderer = new SpriteRenderer(null, 1f);

        Assert.That(renderer.AnimationFrame, Is.Zero);

        renderer.ObserveFrameChange(atlasFrameBefore: 31, atlasFrameAfter: 32);
        Assert.That(renderer.AnimationFrame, Is.EqualTo(1));

        renderer.ObserveFrameChange(atlasFrameBefore: 32, atlasFrameAfter: 32);
        Assert.That(renderer.AnimationFrame, Is.EqualTo(1), "An unchanged atlas frame is not a new pose");

        renderer.ObserveFrameChange(atlasFrameBefore: 36, atlasFrameAfter: 31);
        Assert.That(renderer.AnimationFrame, Is.EqualTo(2), "A loop wrap counts as a new pose");
    }

    [Test]
    public void PoseFrame_ResetsOnSetAnimation()
    {
        var renderer = new SpriteRenderer(null, 1f);
        renderer.ObserveFrameChange(31, 32);
        renderer.ObserveFrameChange(32, 33);
        Assert.That(renderer.AnimationFrame, Is.EqualTo(2));

        renderer.SetAnimation("idle");

        Assert.That(renderer.AnimationFrame, Is.Zero);
        Assert.That(renderer.CurrentAnimationKey, Is.EqualTo("idle"));
    }
}
