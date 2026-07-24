using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Tests;

internal sealed class StubPickupEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IPickup
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public void OnPickup(IDamageable target) { }
}

[TestFixture]
public class PickupRegistrationTests
{
    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 600));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("pickups", new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers("actors", "props");
        world.EnableCollisionBetweenLayers("actors", "pickups");
        return world;
    }

    [Test]
    public void PickupEntity_RegisteredInPickupCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);

        mgr.Register(pickup);

        Assert.That(mgr.PickupCollidables, Does.Contain(pickup));
    }

    [Test]
    public void PickupEntity_NotInActorOrPropCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);

        mgr.Register(pickup);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.ActorCollidables, Is.Empty);
            Assert.That(mgr.PropCollidables, Is.Empty);
        });
    }

    [Test]
    public void Clear_RemovesFromPickupCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);
        mgr.Register(pickup);

        mgr.Clear();

        Assert.That(mgr.PickupCollidables, Is.Empty);
    }

    [Test]
    public void Destroy_RemovesFromPickupCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);
        mgr.Register(pickup);

        mgr.Destroy(pickup);
        mgr.ProcessPending();

        Assert.That(mgr.PickupCollidables, Is.Empty);
    }

    [Test]
    public void PickupEntity_RegisteredInAll()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);

        mgr.Register(pickup);

        Assert.That(mgr.All, Does.Contain(pickup));
    }
}