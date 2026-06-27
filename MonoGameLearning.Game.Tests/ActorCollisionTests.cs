using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Helpers;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Tests;

public class TestActorEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IMoveableEntity
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; }
    public RectangleF MovementBounds { get; set; }
}

public class CollisionPushEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IMoveableEntity
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public Vector2 MovementDirection { get; set; }
    public float Speed { get; set; }
    public RectangleF MovementBounds { get; set; }
}

[TestFixture]
public class ActorCollisionTests
{
    private static RectangleF TwoScreenBounds => new(0, 200, 1600, 200);
    private const int EntitySize = 50;

    private static TestActorEntity MakeEntity(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    private static CollisionPushEntity MakePushEntity(float x, float y) =>
        new("pusher", new Vector2(x, y), EntitySize, EntitySize);

    [Test]
    public void ClampToBounds_EntityInside_DoesNotMove()
    {
        var entity = MakeEntity(400, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position, Is.EqualTo(new Vector2(400, 300)));
    }

    [Test]
    public void ClampToBounds_PastLeftEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(-10, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void ClampToBounds_PastRightEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(1610, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void ClampToBounds_PastTopEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(400, 190);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.Y, Is.EqualTo(225));
    }

    [Test]
    public void ClampToBounds_PastBottomEdge_ClampsToCenterInset()
    {
        var entity = MakeEntity(400, 410);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.Y, Is.EqualTo(375));
    }

    [Test]
    public void ClampToBounds_EmptyBounds_DoesNothing()
    {
        var entity = MakeEntity(-100, -100);
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position, Is.EqualTo(new Vector2(-100, -100)));
    }

    [Test]
    public void ClampToBounds_EntityAlreadyAtLeftEdge_StaysPut()
    {
        var entity = MakeEntity(25, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void ClampToBounds_EntityAlreadyAtRightEdge_StaysPut()
    {
        var entity = MakeEntity(1575, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void MultiScreen_CanMoveAcrossScreenBoundary()
    {
        var entity = MakeEntity(400, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.Position = new Vector2(900, 300);
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(900));
    }

    [Test]
    public void MultiScreen_ClampsAtFarRightBoundary()
    {
        var entity = MakeEntity(1600, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void MultiScreen_ClampsAtFarLeftBoundary()
    {
        var entity = MakeEntity(-10, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void CollisionThenClamp_EntityPushedPastLeftEdge_ClampedToBounds()
    {
        var entity = MakeEntity(40, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.Position -= new Vector2(25, 0);
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void CollisionThenClamp_EntityPushedPastRightEdge_ClampedToBounds()
    {
        var entity = MakeEntity(1560, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.Position += new Vector2(30, 0);
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void CollisionThenClamp_MultiplePushesTowardEdge_StaysInBounds()
    {
        var entity = MakeEntity(50, 300);
        entity.MovementBounds = TwoScreenBounds;
        entity.Position -= new Vector2(10, 0);
        entity.Position -= new Vector2(10, 0);
        entity.Position -= new Vector2(10, 0);
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void ClampThenCollision_CollisionCanStillMoveEntityAwayFromEdge()
    {
        var entity = MakeEntity(25, 300);
        entity.MovementBounds = TwoScreenBounds;
        Mover.ClampToBounds(entity, entity.MovementBounds);
        entity.Position -= new Vector2(-10, 0);
        Assert.That(entity.Position.X, Is.EqualTo(35));
    }

    [Test]
    public void TwoEntitiesAtLeftEdge_PushedIntoEdge_BothStayInBounds()
    {
        var bounds = TwoScreenBounds;
        var left = MakePushEntity(40, 300);
        var right = MakePushEntity(70, 300);
        left.MovementBounds = bounds;
        right.MovementBounds = bounds;

        left.Position -= new Vector2(20, 0);
        Mover.ClampToBounds(left, left.MovementBounds);
        Assert.That(left.Position.X, Is.EqualTo(25));
        Assert.That(right.Position.X, Is.EqualTo(70));
    }

    [Test]
    public void TwoEntitiesAtRightEdge_PushedIntoEdge_BothStayInBounds()
    {
        var bounds = TwoScreenBounds;
        var left = MakePushEntity(1530, 300);
        var right = MakePushEntity(1560, 300);
        left.MovementBounds = bounds;
        right.MovementBounds = bounds;

        right.Position -= new Vector2(-25, 0);
        Mover.ClampToBounds(right, right.MovementBounds);
        Assert.That(right.Position.X, Is.EqualTo(1575));
    }

    [Test]
    public void ThreeEntitiesPushingTowardEdge_AllStayInBounds()
    {
        var bounds = TwoScreenBounds;
        var e1 = MakePushEntity(35, 300);
        var e2 = MakePushEntity(65, 300);
        var e3 = MakePushEntity(95, 300);
        e1.MovementBounds = bounds;
        e2.MovementBounds = bounds;
        e3.MovementBounds = bounds;

        e2.Position -= new Vector2(15, 0);
        e1.Position -= new Vector2(15, 0);

        Mover.ClampToBounds(e1, e1.MovementBounds);
        Mover.ClampToBounds(e2, e2.MovementBounds);
        Mover.ClampToBounds(e3, e3.MovementBounds);

        Assert.That(e1.Position.X, Is.EqualTo(25));
        Assert.That(e2.Position.X, Is.EqualTo(50));
        Assert.That(e3.Position.X, Is.EqualTo(95));
    }

    [Test]
    public void SingleEntityPushedByCollision_ThenClamped_DoesNotBounceBackFromEdge()
    {
        var entity = MakeEntity(30, 300);
        entity.MovementBounds = TwoScreenBounds;

        entity.Position -= new Vector2(10, 0);
        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));

        Mover.ClampToBounds(entity, entity.MovementBounds);
        Assert.That(entity.Position.X, Is.EqualTo(25));
    }

    [Test]
    public void AfterCollisionAndClamp_FrameIsConsistentWithPosition()
    {
        var entity = MakeEntity(30, 300);
        entity.MovementBounds = TwoScreenBounds;

        entity.Position -= new Vector2(10, 0);
        Mover.ClampToBounds(entity, entity.MovementBounds);

        float halfSize = EntitySize / 2f;
        var expectedFrame = new RectangleF(25 - halfSize, 300 - halfSize, EntitySize, EntitySize);
        Assert.That(entity.Frame, Is.EqualTo(expectedFrame));
    }
}
