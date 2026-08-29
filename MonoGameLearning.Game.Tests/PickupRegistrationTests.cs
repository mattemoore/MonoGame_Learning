using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Tests;

internal sealed class StubPickupEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, ICollisionLayer, IPickup
{
    public int Id => GetHashCode();
    public string LayerName => CollisionLayers.Pickups;
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public void OnPickup(IDamageable target) { }
}

[TestFixture]
public class PickupRegistrationTests
{
    private static CollisionWorld2D CreateTestWorld() =>
        CollisionWorldFactory.Create(new RectangleF(0, 0, 2000, 600));

    [Test]
    public void PickupEntity_RegisteredInPickupCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);

        mgr.Register(pickup);

        Assert.That(mgr.PickupCollidables, Does.Contain(pickup));
    }

    [Test]
    public void PickupEntity_NotInActorOrPropCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);

        mgr.Register(pickup);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Is.Empty);
            Assert.That(mgr.GetCollidables(CollisionLayers.Props), Is.Empty);
        });
    }

    [Test]
    public void Clear_RemovesFromPickupCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);
        mgr.Register(pickup);

        mgr.Clear();

        Assert.That(mgr.PickupCollidables, Is.Empty);
    }

    [Test]
    public void Destroy_RemovesFromPickupCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);
        mgr.Register(pickup);

        mgr.Destroy(pickup);
        mgr.ProcessPending();

        Assert.That(mgr.PickupCollidables, Is.Empty);
    }

    [Test]
    public void PickupEntity_RegisteredInAll()
    {
        var mgr = new EntityService(CreateTestWorld());
        var pickup = new StubPickupEntity("p", Vector2.Zero, 32, 32);

        mgr.Register(pickup);

        Assert.That(mgr.All, Does.Contain(pickup));
    }
}