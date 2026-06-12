using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

public class TestActorEntity(string name, Vector2 position, int width, int height)
    : ActorEntity(name, position, width, height)
{
}

[TestFixture]
public class ActorCollisionTests
{
    private static RectangleF TwoScreenBounds => new(0, 200, 1600, 200);
    private const int EntitySize = 50;

    private static TestActorEntity MakeEntity(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    private static CollisionEventArgs CollisionWith(Vector2 penetration) =>
        new() { Other = null!, PenetrationVector = penetration };

    // ==========================================
    // OnCollision — entity-to-entity push-apart
    // ==========================================

    [Test]
    public void OnCollision_TwoOverlappingEntities_ArePushedApart()
    {
        var left = MakeEntity(90, 100);
        var right = MakeEntity(110, 100);

        // Overlap: left.Right=115, right.Left=85 → 30px overlap.
        // Each should be pushed apart by half the overlap.
        left.OnCollision(new CollisionEventArgs { Other = right, PenetrationVector = new Vector2(15, 0) });
        right.OnCollision(new CollisionEventArgs { Other = left, PenetrationVector = new Vector2(-15, 0) });

        Assert.That(left.Position.X, Is.EqualTo(75));
        Assert.That(right.Position.X, Is.EqualTo(125));

        // No longer overlapping
        Assert.That(left.Frame.Right, Is.LessThanOrEqualTo(right.Frame.Left));
    }

    // ==========================================
    // ClampToBounds — staying inside level edges
    // ==========================================

    [Test]
    public void ClampToBounds_EntityInside_DoesNotMove()
    {
        var entity = MakeEntity(400, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        Assert.That(entity.Position, Is.EqualTo(new Vector2(400, 300)));
    }

    [Test]
    public void ClampToBounds_PastLeftEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(-10, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        // Min allowed: 0 + 25 = 25
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void ClampToBounds_PastRightEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(1610, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        // Max allowed: 1600 - 25 = 1575
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void ClampToBounds_PastTopEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(400, 190);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        // Min allowed: 200 + 25 = 225
        Assert.That(entity.Position.Y, Is.EqualTo(225));
    }

    [Test]
    public void ClampToBounds_PastBottomEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(400, 410);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        // Max allowed: 400 - 25 = 375
        Assert.That(entity.Position.Y, Is.EqualTo(375));
    }

    [Test]
    public void ClampToBounds_EmptyBounds_DoesNothing()
    {
        var entity = MakeEntity(-100, -100);
        entity.ClampToBounds();
        Assert.That(entity.Position, Is.EqualTo(new Vector2(-100, -100)));
    }

    [Test]
    public void ClampToBounds_EntityAlreadyAtLeftEdge_StaysPut()
    {
        var entity = MakeEntity(25, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void ClampToBounds_EntityAlreadyAtRightEdge_StaysPut()
    {
        var entity = MakeEntity(1575, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    // ==========================================
    // Multi-screen bounds (2 screens = 1600px wide)
    // ==========================================

    [Test]
    public void MultiScreen_CanMoveAcrossScreenBoundary()
    {
        var entity = MakeEntity(400, 300);
        entity.MovementBounds = TwoScreenBounds;

        // Move from screen 1 (center 400) past seam at 800 into screen 2
        entity.Position = new Vector2(900, 300);
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(900)); // well within 1600 bounds
    }

    [Test]
    public void MultiScreen_ClampsAtFarRightBoundary()
    {
        var entity = MakeEntity(1600, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void MultiScreen_ClampsAtFarLeftBoundary()
    {
        var entity = MakeEntity(-10, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    // ==========================================
    // Collision + Clamping — entity pushed by another near edge
    // ==========================================

    [Test]
    public void CollisionThenClamp_EntityPushedPastLeftEdge_ClampedToBounds()
    {
        var entity = MakeEntity(40, 300);
        entity.MovementBounds = TwoScreenBounds;

        // Collision pushes entity left (positive penetration vector = subtracts from position)
        entity.OnCollision(CollisionWith(new Vector2(25, 0)));

        // Now at 15, which is left of min allowed (25)
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void CollisionThenClamp_EntityPushedPastRightEdge_ClampedToBounds()
    {
        var entity = MakeEntity(1560, 300);
        entity.MovementBounds = TwoScreenBounds;

        // Collision pushes entity right (penetration from left-side overlap)
        entity.OnCollision(CollisionWith(new Vector2(-30, 0)));

        // Penetration vector is negative, so Position -= (-30) means Position += 30
        // Position becomes 1590 -- past max allowed (1575)
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void CollisionThenClamp_MultiplePushesTowardEdge_StaysInBounds()
    {
        var entity = MakeEntity(50, 300);
        entity.MovementBounds = TwoScreenBounds;

        // Multiple collision pushes toward left edge (positive penetration = subtracts from position)
        entity.OnCollision(CollisionWith(new Vector2(10, 0)));
        entity.OnCollision(CollisionWith(new Vector2(10, 0)));
        entity.OnCollision(CollisionWith(new Vector2(10, 0)));
        entity.ClampToBounds();

        // 50 - 30 = 20, clamped to 25
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void ClampThenCollision_CollisionCanStillMoveEntityAwayFromEdge()
    {
        var entity = MakeEntity(25, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.ClampToBounds();

        // Collision pushes entity right (negative penetration vector = adds to position)
        entity.OnCollision(CollisionWith(new Vector2(-10, 0)));
        Assert.That(entity.Position.X, Is.EqualTo(35)); // moved right, no longer at edge
    }

    // ==========================================
    // Entity-to-entity collision + bounds at level boundaries
    // ==========================================

    [Test]
    public void TwoEntitiesAtLeftEdge_PushedIntoEdge_BothStayInBounds()
    {
        var bounds = TwoScreenBounds;
        // Two entities touching near the left edge
        var left = MakeEntity(40, 300);
        var right = MakeEntity(70, 300);
        left.MovementBounds = bounds;
        right.MovementBounds = bounds;

        // Simulate collision: right pushes left further left (positive penetration)
        left.OnCollision(new CollisionEventArgs { Other = right, PenetrationVector = new Vector2(20, 0) });

        // Left is now at 20 — past min (25), clamp to 25
        left.ClampToBounds();
        Assert.That(left.Position.X, Is.EqualTo(25));

        // Right stays put (no collision push on it)
        Assert.That(right.Position.X, Is.EqualTo(70));
    }

    [Test]
    public void TwoEntitiesAtRightEdge_PushedIntoEdge_BothStayInBounds()
    {
        var bounds = TwoScreenBounds;
        var left = MakeEntity(1530, 300);
        var right = MakeEntity(1560, 300);
        left.MovementBounds = bounds;
        right.MovementBounds = bounds;

        // Simulate collision: left pushes right further right
        right.OnCollision(new CollisionEventArgs { Other = left, PenetrationVector = new Vector2(-25, 0) });

        // Right is now at 1585 — past max (1575), clamp to 1575
        right.ClampToBounds();
        Assert.That(right.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void ThreeEntitiesPushingTowardEdge_AllStayInBounds()
    {
        var bounds = TwoScreenBounds;
        var e1 = MakeEntity(35, 300);
        var e2 = MakeEntity(65, 300);
        var e3 = MakeEntity(95, 300);
        e1.MovementBounds = bounds;
        e2.MovementBounds = bounds;
        e3.MovementBounds = bounds;

        // Rightmost entity pushes middle, middle pushes leftmost (positive penetration = push left)
        e2.OnCollision(new CollisionEventArgs { Other = e3, PenetrationVector = new Vector2(15, 0) });
        e1.OnCollision(new CollisionEventArgs { Other = e2, PenetrationVector = new Vector2(15, 0) });

        e1.ClampToBounds();
        e2.ClampToBounds();
        e3.ClampToBounds();

        // e1: 35 - 15 = 20, clamped to 25
        // e2: 65 - 15 = 50 (still in bounds)
        // e3: 95 (unchanged)
        Assert.That(e1.Position.X, Is.EqualTo(25));
        Assert.That(e2.Position.X, Is.EqualTo(50));
        Assert.That(e3.Position.X, Is.EqualTo(95));
    }

    [Test]
    public void SingleEntityPushedByCollision_ThenClamped_DoesNotBounceBackFromEdge()
    {
        var entity = MakeEntity(30, 300);
        entity.MovementBounds = TwoScreenBounds;

        // Push left past edge (positive penetration = subtracts from position)
        entity.OnCollision(CollisionWith(new Vector2(10, 0)));
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(25));

        // Clamping again should not push it back outward
        entity.ClampToBounds();
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    // ==========================================
    // Frame consistency after operations
    // ==========================================

    [Test]
    public void AfterCollisionAndClamp_FrameIsConsistentWithPosition()
    {
        var entity = MakeEntity(30, 300);
        entity.MovementBounds = TwoScreenBounds;

        entity.OnCollision(CollisionWith(new Vector2(10, 0)));
        entity.ClampToBounds();

        float halfSize = EntitySize / 2f;
        var expectedFrame = new RectangleF(25 - halfSize, 300 - halfSize, EntitySize, EntitySize);
        Assert.That(entity.Frame, Is.EqualTo(expectedFrame));
    }
}