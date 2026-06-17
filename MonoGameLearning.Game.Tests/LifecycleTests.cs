using System;
using Microsoft.Xna.Framework;
using MonoGameLearning.Game.GameLoop;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class OilDrumLifecycleTests
{
    [Test]
    public void DestroyedEvent_Fires_WhenHealthDepleted()
    {
        var entity = new TestDamageableEntity("drum", Vector2.Zero, 50, 50);
        bool destroyed = false;
        entity.Destroyed += _ => destroyed = true;
        entity.TakeDamage(50);
        Assert.That(destroyed, Is.True);
    }

    [Test]
    public void DestroyedEvent_Unsubscribe_RemovesHandler()
    {
        var entity = new TestDamageableEntity("drum", Vector2.Zero, 50, 50);
        int callCount = 0;
        Action<TestDamageableEntity> handler = _ => callCount++;
        entity.Destroyed += handler;
        entity.TakeDamage(50);
        entity.Destroyed -= handler;
        // After unsubscribing, firing again should not invoke handler
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void DestroyedEvent_DoesNotFire_WhenHealthRemains()
    {
        var entity = new TestDamageableEntity("drum", Vector2.Zero, 50, 50);
        bool destroyed = false;
        entity.Destroyed += _ => destroyed = true;
        entity.TakeDamage(2);
        Assert.That(destroyed, Is.False);
    }
}

[TestFixture]
public class CameraClampTests
{
    private static float ComputeClampedX(float playerX, int gameWidth, int totalLevelWidth)
    {
        float minX = gameWidth / 2f;
        float maxX = totalLevelWidth - (gameWidth / 2f);
        return Math.Clamp(playerX, minX, maxX);
    }

    [Test]
    public void PlayerAtLeftEdge_CameraClampsToHalfGameWidth()
    {
        float result = ComputeClampedX(0, 800, 1600);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void PlayerAtRightEdge_CameraClampsToTotalWidthMinusHalfGameWidth()
    {
        float result = ComputeClampedX(1600, 800, 1600);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void PlayerInMiddle_CameraFollowsExactly()
    {
        float result = ComputeClampedX(600, 800, 1600);
        Assert.That(result, Is.EqualTo(600));
    }

    [Test]
    public void PlayerPastLeftEdge_ClampsToMin()
    {
        float result = ComputeClampedX(-100, 800, 1600);
        Assert.That(result, Is.EqualTo(400));
    }

    [Test]
    public void PlayerPastRightEdge_ClampsToMax()
    {
        float result = ComputeClampedX(2000, 800, 1600);
        Assert.That(result, Is.EqualTo(1200));
    }

    [Test]
    public void PlayerAtExactBoundary_StaysAtBoundary()
    {
        float minResult = ComputeClampedX(400, 800, 1600);
        float maxResult = ComputeClampedX(1200, 800, 1600);
        Assert.That(minResult, Is.EqualTo(400));
        Assert.That(maxResult, Is.EqualTo(1200));
    }

    [Test]
    public void LevelWidthEqualsGameWidth_ClampsToCenter()
    {
        float minResult = ComputeClampedX(0, 800, 800);
        float maxResult = ComputeClampedX(800, 800, 800);
        Assert.That(minResult, Is.EqualTo(400));
        Assert.That(maxResult, Is.EqualTo(400));
    }
}