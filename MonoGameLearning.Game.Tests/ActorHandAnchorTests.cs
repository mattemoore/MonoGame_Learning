using MonoGameLearning.Core.Animation;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Game.AnimatedSprites;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class ActorHandAnchorTests
{
    // Keyed off each actor's own SpriteAnimationDefs so a frame-count edit is caught here
    // rather than silently wrapping a hand pose to the wrong frame.
    private static void AssertCoversEveryFrame(HandAnchorTable table, SpriteAnimationDef[] animations)
    {
        foreach (var animation in animations)
        {
            for (int frame = 0; frame < animation.FrameCount; frame++)
            {
                Assert.That(table.TryResolve(animation.Name, frame, out var hand), Is.True,
                    $"Missing hand anchor for {animation.Name}[{frame}]");
                Assert.That(float.IsFinite(hand.X) && float.IsFinite(hand.Y), Is.True,
                    $"Non-finite hand anchor for {animation.Name}[{frame}]");
            }
        }
    }

    [Test]
    public void PlayerSprite_HandTable_CoversEveryAnimationFrame() =>
        AssertCoversEveryFrame(PlayerSprite.HandAnchors, PlayerSprite.AnimationDefs);

    [Test]
    public void EnemySprite_HandTable_CoversItsAnimationFrames() =>
        AssertCoversEveryFrame(EnemySprite.HandAnchors, EnemySprite.AnimationDefs);

    [Test]
    public void PlayerAndEnemy_TablesAreDistinct()
    {
        Assert.That(PlayerSprite.HandAnchors, Is.Not.SameAs(EnemySprite.HandAnchors),
            "Each actor owns its own hand table so future sprites can differ");
    }

    [Test]
    public void PlayerAndEnemy_ResolveTheSameHandPoints_WhileSharingTheAtlas()
    {
        // Both actors render the placeholder `adventurer` atlas, so their shared animations
        // must resolve identical hand points. Without this guard, pasting a re-exported `hand`
        // block into only one actor silently leaves the other's weapon on stale pivots.
        foreach (var animation in EnemySprite.AnimationDefs)
        {
            for (int frame = 0; frame < animation.FrameCount; frame++)
            {
                Assert.That(PlayerSprite.HandAnchors.TryResolve(animation.Name, frame, out var player), Is.True,
                    $"Player missing {animation.Name}[{frame}]");
                Assert.That(EnemySprite.HandAnchors.TryResolve(animation.Name, frame, out var enemy), Is.True,
                    $"Enemy missing {animation.Name}[{frame}]");
                Assert.That(enemy, Is.EqualTo(player),
                    $"{animation.Name}[{frame}] drifted between player and enemy while they share the atlas");
            }
        }
    }
}
