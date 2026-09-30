using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Game.AnimatedSprites;
using MonoGameLearning.Game.Entities.Enemy;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class KnockdownTimingTests
{
    private static GameTime Elapsed(float seconds) => new(TimeSpan.Zero, TimeSpan.FromSeconds(seconds));

    [Test]
    public void KnockedDown_HoldsOnTheGroundBeforeGettingUp()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));

        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });
        Assert.That(enemy.SpriteRenderer.CurrentAnimationKey, Is.EqualTo(EnemySprite.AnimationFall),
            "Knockdown starts with the fall clip.");

        // Fall clip ends: the actor must stay down (hold), not get up immediately.
        enemy.CompleteAnimation();
        Assert.That(enemy.SpriteRenderer.CurrentAnimationKey, Is.EqualTo(EnemySprite.AnimationFall));

        // Partway through the hold, still down.
        enemy.TickIncapacitated(Elapsed(0.3f));
        Assert.That(enemy.SpriteRenderer.CurrentAnimationKey, Is.EqualTo(EnemySprite.AnimationFall),
            "The actor must lie on the ground for the hold duration.");

        // Past the hold, the getup clip starts.
        enemy.TickIncapacitated(Elapsed(0.6f));
        Assert.That(enemy.SpriteRenderer.CurrentAnimationKey, Is.EqualTo(EnemySprite.AnimationGetUp),
            "The getup clip starts only after the hold elapses.");
    }

    [Test]
    public void GetUpCompletion_LeavesKnockdown()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));
        enemy.TakeDamage(new DamageInfo { Amount = 1, Knockdown = true });

        enemy.CompleteAnimation();               // fall done → hold
        enemy.TickIncapacitated(Elapsed(0.8f));  // hold elapsed → getup
        Assert.That(enemy.SpriteRenderer.CurrentAnimationKey, Is.EqualTo(EnemySprite.AnimationGetUp));

        enemy.CompleteAnimation();               // getup done → leave knockdown
        Assert.That(enemy.StateController!.State, Is.EqualTo(EnemyState.Idle));
    }

    [Test]
    public void IncapacitatedTick_NotKnockedDown_IsNoOp()
    {
        var enemy = new TestEnemyEntity("enemy", new Vector2(100, 100));

        Assert.That(enemy.TickIncapacitated(Elapsed(1.0f)), Is.False);
    }
}
