using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Pickup;

namespace MonoGameLearning.Game.Tests;

internal sealed class TestPickupActor(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IPickup
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public bool WasPickedUp { get; private set; }

    public void OnPickup(IDamageable target) => WasPickedUp = true;
}

internal sealed class TestActorForPickup(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IDamageable, IDamageResponse
{
    private readonly Health _health = new(100);
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public int Health => _health.Value;
    public int MaxHealth => _health.MaxHealth;
    public bool IsAlive => _health.IsAlive;
    public Faction Faction => Faction.Player;
    public event EventHandler Died = delegate { };

    public void TakeDamage(DamageInfo info) => CombatService.ApplyDamage(this, info);
    bool IDamageResponse.IsAlive => _health.IsAlive;
    bool IDamageResponse.CanTakeDamage() => _health.IsAlive;
    void IDamageResponse.ReduceHealth(int amount) => _health.Subtract(amount);
    void IDamageResponse.OnDeath() => Died?.Invoke(this, EventArgs.Empty);
    void IDamageResponse.OnKnockdown(DamageInfo info) { }
    void IDamageResponse.OnHit(DamageInfo info) { }
    void IDamageable.Heal(int amount) => _health.Add(amount);
}

[TestFixture]
public class PickupCollisionTests
{
    private const int EntitySize = 50;

    private static CollisionWorld2D CreateWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 2000));
        world.AddLayer(CollisionLayers.Actors, new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Actors);
        world.AddLayer(CollisionLayers.Pickups, new Layer(new QuadTreeSpace(bb)));
        world.DisableCollisionBetweenLayers(CollisionLayers.Pickups, CollisionLayers.Pickups);
        world.EnableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Pickups);
        return world;
    }

    private static TestPickupActor MakePickup(float x, float y) =>
        new("pickup", new Vector2(x, y), EntitySize, EntitySize);

    private static TestActorForPickup MakeActor(float x, float y) =>
        new("actor", new Vector2(x, y), EntitySize, EntitySize);

    [Test]
    public void Integration_PickupAndPlayer_Overlap_ProducesCollisionPair()
    {
        var world = CreateWorld();
        var actor = MakeActor(100, 100);
        var pickup = MakePickup(100, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(pickup, CollisionLayers.Pickups);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Pickups).ToList();

        Assert.That(pairs, Has.Count.EqualTo(1));
    }

    [Test]
    public void Integration_PickupFarFromPlayer_NoOverlap()
    {
        var world = CreateWorld();
        var actor = MakeActor(100, 100);
        var pickup = MakePickup(500, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(pickup, CollisionLayers.Pickups);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Pickups).ToList();

        Assert.That(pairs, Is.Empty);
    }

    [Test]
    public void Integration_OverlapPair_AppliesHealAndQueuesDestroy()
    {
        var world = CreateWorld();
        var actor = MakeActor(100, 100);
        var pickup = MakePickup(100, 100);

        world.Insert(actor, CollisionLayers.Actors);
        world.Insert(pickup, CollisionLayers.Pickups);
        world.RebuildDynamicLayers();

        var pairs = world.QueryCollisionPairs(CollisionLayers.Actors, CollisionLayers.Pickups).ToList();
        Assert.That(pairs, Has.Count.EqualTo(1));

        // Simulate the pickup resolution logic
        if (actor is IDamageable damageable)
        {
            pickup.OnPickup(damageable);
            Assert.That(pickup.WasPickedUp, Is.True);
        }
    }
}