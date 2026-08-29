using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Audio;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Tests;

internal sealed class PickupServiceTestPickup(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, ICollisionLayer, IPickup
{
    public int Id => GetHashCode();
    public string LayerName => CollisionLayers.Pickups;
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public bool WasPickedUp { get; private set; }

    public void OnPickup(IDamageable target) => WasPickedUp = true;
}

[TestFixture]
public class PickupServiceTests
{
    private const int EntitySize = 50;
    private static RectangleF Bounds => new(0, 0, 2000, 2000);

    private static EntityService CreateManager() =>
        new(CollisionWorldFactory.Create(Bounds));

    private static TestActorForPickup MakePlayer(float x, float y) =>
        new("player", new Vector2(x, y), EntitySize, EntitySize);

    private static PickupServiceTestPickup MakePickup(float x, float y) =>
        new("pickup", new Vector2(x, y), EntitySize, EntitySize);

    [Test]
    public void ResolveOverlaps_OverlappingPickup_AppliesPickupAndQueuesDestroy()
    {
        var mgr = CreateManager();
        var player = MakePlayer(100, 100);
        var pickup = MakePickup(100, 100);
        mgr.Register(player);
        mgr.Register(pickup);

        SfxId? playedSfx = null;
        PickupService.ResolveOverlaps(mgr, player, sfx => playedSfx = sfx);
        mgr.ProcessPending();

        Assert.Multiple(() =>
        {
            Assert.That(pickup.WasPickedUp, Is.True);
            Assert.That(playedSfx, Is.EqualTo(SfxId.PickupHeal));
            Assert.That(mgr.All, Does.Not.Contain(pickup));
        });
    }

    [Test]
    public void ResolveOverlaps_NonOverlappingPickup_IsNotPickedUp()
    {
        var mgr = CreateManager();
        var player = MakePlayer(100, 100);
        var pickup = MakePickup(500, 100);
        mgr.Register(player);
        mgr.Register(pickup);

        bool sfxPlayed = false;
        PickupService.ResolveOverlaps(mgr, player, _ => sfxPlayed = true);

        Assert.Multiple(() =>
        {
            Assert.That(pickup.WasPickedUp, Is.False);
            Assert.That(sfxPlayed, Is.False);
            Assert.That(mgr.All, Does.Contain(pickup));
        });
    }

    [Test]
    public void ResolveOverlaps_DeadPlayer_DoesNotPickUp()
    {
        var mgr = CreateManager();
        var player = MakePlayer(100, 100);
        var pickup = MakePickup(100, 100);
        mgr.Register(player);
        mgr.Register(pickup);

        ((IDamageResponse)player).ReduceHealth(100);

        bool sfxPlayed = false;
        PickupService.ResolveOverlaps(mgr, player, _ => sfxPlayed = true);

        Assert.Multiple(() =>
        {
            Assert.That(pickup.WasPickedUp, Is.False);
            Assert.That(sfxPlayed, Is.False);
        });
    }
}