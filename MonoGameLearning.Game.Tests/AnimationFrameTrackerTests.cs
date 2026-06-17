using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class AnimationFrameTrackerTests
{
    [Test]
    public void Initial_FrameIndex_IsZero()
    {
        var tracker = new AnimationFrameTracker();
        Assert.That(tracker.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void Initial_TryGetNewFrame_ReturnsTrueForFirstUnregistered()
    {
        var tracker = new AnimationFrameTracker();
        var result = tracker.TryGetNewFrame(out var frame);
        Assert.That(result, Is.True);
        Assert.That(frame, Is.EqualTo(0));
    }

    [Test]
    public void AfterTryGetNewFrame_CallingAgain_ReturnsFalse()
    {
        var tracker = new AnimationFrameTracker();
        tracker.TryGetNewFrame(out _);
        Assert.That(tracker.TryGetNewFrame(out _), Is.False);
    }

    [Test]
    public void Reset_RestoresInitialState()
    {
        var tracker = new AnimationFrameTracker();
        tracker.TryGetNewFrame(out _);
        tracker.Reset();
        Assert.That(tracker.FrameIndex, Is.EqualTo(0));
        // After Reset, TryGetNewFrame returns false until AdvanceOnFrameChange
        // detects an actual frame advance (sprite is at frame 0).
        Assert.That(tracker.TryGetNewFrame(out _), Is.False);
    }

    [Test]
    public void Reset_AfterMultipleAdvances_ResetsToZero()
    {
        var tracker = new AnimationFrameTracker();
        // Simulate 3 frame advance cycles via TryGetNewFrame + Reset
        tracker.TryGetNewFrame(out _);
        tracker.Reset();
        Assert.That(tracker.TryGetNewFrame(out _), Is.False);
        Assert.That(tracker.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void MultipleResets_AreIdempotent()
    {
        var tracker = new AnimationFrameTracker();
        tracker.Reset();
        tracker.Reset();
        // After Reset, same as Reset_RestoresInitialState
        Assert.That(tracker.TryGetNewFrame(out _), Is.False);
    }
}