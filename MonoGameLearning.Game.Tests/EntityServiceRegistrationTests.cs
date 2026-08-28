using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Combat;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Actor;
using MonoGameLearning.Core.Entities.Prop;
using MonoGameLearning.Core.Rendering;

namespace MonoGameLearning.Game.Tests;

internal sealed class StubCombatActor : CombatActorBase
{
    public StubCombatActor(string name, Vector2 position, int width, int height, Faction faction = Faction.Player)
        : base(name, position, width, height, null!, 1f, 100, new(null!, null!, null!, null!, null!, null!), null!)
    {
        Faction = faction;
    }

    public void CallAdvanceFrameAndRegisterHitboxes(GameTime gt) => AdvanceFrameAndRegisterHitboxes(gt);
    public override void Update(GameTime gameTime) { }
    protected override ActorPhase Phase => ActorPhase.Idle;
    protected override void FirePhaseCompleted() { }
}

internal sealed class GenericCollidableEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IUpdatable, IRenderable
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public void Update(GameTime gameTime) { }
    public void Render(RenderContext context) { }
}

internal sealed class TestPropEntity(string name, Vector2 position)
    : PropBase(name, position, 40, 40, 100, CollisionAnchor.Center)
{
    public override void TakeDamage(DamageInfo info) { }
}

[TestFixture]
public class EntityServiceRegistrationTests
{
    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 600));
        world.AddLayer(CollisionLayers.Actors, new Layer(new QuadTreeSpace(bb)));
        world.AddLayer(CollisionLayers.Props, new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers(CollisionLayers.Actors, CollisionLayers.Props);
        return world;
    }

    [Test]
    public void CombatActor_RegisteredInActorCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Does.Contain(actor));
            Assert.That(mgr.GetCollidables(CollisionLayers.Props), Is.Empty);
        });
    }

    [Test]
    public void CombatActor_NotInPropCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.That(mgr.GetCollidables(CollisionLayers.Props), Is.Empty);
    }

    [Test]
    public void GenericCollidable_NotInActorOrPropLists()
    {
        var mgr = new EntityService(CreateTestWorld());
        var entity = new GenericCollidableEntity("g", Vector2.Zero, 50, 50);

        mgr.Register(entity);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Is.Empty);
            Assert.That(mgr.GetCollidables(CollisionLayers.Props), Is.Empty);
        });
    }

    [Test]
    public void MultipleCombatActors_AllInActorCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var a1 = new StubCombatActor("a1", Vector2.Zero, 50, 50);
        var a2 = new StubCombatActor("a2", Vector2.Zero, 50, 50);

        mgr.Register(a1);
        mgr.Register(a2);

        Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Has.Count.EqualTo(2));
    }

    [Test]
    public void Clear_RemovesFromActorCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);
        mgr.Register(actor);

        mgr.Clear();

        Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Does.Not.Contain(actor));
    }

    [Test]
    public void Destroy_RemovesFromActorCollidables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);
        mgr.Register(actor);

        mgr.Destroy(actor);
        mgr.ProcessPending();

        Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Does.Not.Contain(actor));
    }

    [Test]
    public void Destroy_LeavesOtherActors()
    {
        var mgr = new EntityService(CreateTestWorld());
        var a1 = new StubCombatActor("a1", Vector2.Zero, 50, 50);
        var a2 = new StubCombatActor("a2", Vector2.Zero, 50, 50);
        mgr.Register(a1);
        mgr.Register(a2);

        mgr.Destroy(a1);
        mgr.ProcessPending();

        Assert.Multiple(() =>
        {
            Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Has.Count.EqualTo(1));
            Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Does.Contain(a2));
        });
    }

    [Test]
    public void Register_Duplicate_NoOp()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);
        mgr.Register(actor);

        Assert.That(mgr.GetCollidables(CollisionLayers.Actors), Has.Count.EqualTo(1));
    }

    // --- HitboxService assignment tests ---

    [Test]
    public void Register_AssignsHitboxService_WhenPassedToConstructor()
    {
        var service = new HitboxService();
        var mgr = new EntityService(CreateTestWorld(), service);
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.That(actor.HitboxService, Is.Not.Null);
        Assert.That(actor.HitboxService, Is.SameAs(service));
    }

    [Test]
    public void Register_WhenNull_LeavesHitboxServiceNull()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.That(actor.HitboxService, Is.Null);
    }

    [Test]
    public void Register_AddsToHitboxProviders()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.That(mgr.HitboxProviders, Does.Contain(actor));
    }

    [Test]
    public void Register_MultipleHitboxProviders_AllAdded()
    {
        var service = new HitboxService();
        var mgr = new EntityService(CreateTestWorld(), service);
        var a1 = new StubCombatActor("a1", Vector2.Zero, 50, 50);
        var a2 = new StubCombatActor("a2", Vector2.Zero, 50, 50);

        mgr.Register(a1);
        mgr.Register(a2);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.HitboxProviders, Has.Count.EqualTo(2));
            Assert.That(a1.HitboxService, Is.Not.Null);
            Assert.That(a2.HitboxService, Is.Not.Null);
        });
    }

    [Test]
    public void Clear_RemovesFromHitboxProviders()
    {
        var mgr = new EntityService(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);
        mgr.Register(actor);
        Assert.That(mgr.HitboxProviders, Does.Contain(actor));

        mgr.Clear();

        Assert.That(mgr.HitboxProviders, Does.Not.Contain(actor));
    }

    [Test]
    public void NonHitboxProvider_NotAddedToHitboxProviders()
    {
        var mgr = new EntityService(CreateTestWorld());
        var entity = new GenericCollidableEntity("g", Vector2.Zero, 50, 50);

        mgr.Register(entity);

        Assert.That(mgr.HitboxProviders, Is.Empty);
    }

    // --- AdvanceFrameAndRegisterHitboxes safety ---

    [Test]
    public void AdvanceFrameAndRegisterHitboxes_NullHitboxService_DoesNotThrow()
    {
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        Assert.DoesNotThrow(() => actor.CallAdvanceFrameAndRegisterHitboxes(new GameTime()));
    }

    [Test]
    public void AdvanceFrameAndRegisterHitboxes_WithHitboxService_DoesNotThrow_OnNullSprite()
    {
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);
        actor.HitboxService = new HitboxService();

        Assert.DoesNotThrow(() => actor.CallAdvanceFrameAndRegisterHitboxes(new GameTime()));
    }

    // --- FindNearestAliveEnemy ---

    [Test]
    public void FindNearestAliveEnemy_NoEnemies_ReturnsNull()
    {
        var mgr = new EntityService(CreateTestWorld());
        Assert.That(mgr.FindNearestAliveEnemy(Vector2.Zero), Is.Null);
    }

    [Test]
    public void FindNearestAliveEnemy_OnlyPlayerUnits_ReturnsNull()
    {
        var mgr = new EntityService(CreateTestWorld());
        mgr.Register(new StubCombatActor("player", new Vector2(100, 0), 50, 50, Faction.Player));
        Assert.That(mgr.FindNearestAliveEnemy(Vector2.Zero), Is.Null);
    }

    [Test]
    public void FindNearestAliveEnemy_SingleEnemy_ReturnsThatEnemy()
    {
        var mgr = new EntityService(CreateTestWorld());
        var enemy = new StubCombatActor("enemy", new Vector2(200, 0), 50, 50, Faction.Enemy);
        mgr.Register(enemy);
        Assert.That(mgr.FindNearestAliveEnemy(Vector2.Zero), Is.SameAs(enemy));
    }

    [Test]
    public void FindNearestAliveEnemy_MultipleEnemies_ReturnsClosest()
    {
        var mgr = new EntityService(CreateTestWorld());
        var far = new StubCombatActor("far", new Vector2(500, 0), 50, 50, Faction.Enemy);
        var close = new StubCombatActor("close", new Vector2(100, 0), 50, 50, Faction.Enemy);
        mgr.Register(far);
        mgr.Register(close);
        Assert.That(mgr.FindNearestAliveEnemy(Vector2.Zero), Is.SameAs(close));
    }

    [Test]
    public void FindNearestAliveEnemy_DeadEnemy_Excluded()
    {
        var mgr = new EntityService(CreateTestWorld());
        var dead = new StubCombatActor("dead", new Vector2(50, 0), 50, 50, Faction.Enemy);
        // Dead entity still in the list — simulate by reducing health to 0
        // but StubCombatActor has no health manipulation. Instead, verify
        // that alive enemies are found even when dead ones exist.
        mgr.Register(dead);
        // Reduce health to 0 via the IDamageable interface
        ((IDamageResponse)dead).ReduceHealth(100);
        var alive = new StubCombatActor("alive", new Vector2(200, 0), 50, 50, Faction.Enemy);
        mgr.Register(alive);
        Assert.That(mgr.FindNearestAliveEnemy(Vector2.Zero), Is.SameAs(alive));
    }

    [Test]
    public void SortRenderablesByY_OrdersByVerticalPosition()
    {
        var mgr = new EntityService(CreateTestWorld());
        var low = new GenericCollidableEntity("low", new Vector2(50, 300), 10, 10);
        var high = new GenericCollidableEntity("high", new Vector2(50, 100), 10, 10);
        mgr.Register(low);
        mgr.Register(high);

        mgr.SortRenderablesByY();

        Assert.Multiple(() =>
        {
            Assert.That(mgr.Renderables[0], Is.SameAs(high));
            Assert.That(mgr.Renderables[1], Is.SameAs(low));
        });
    }

    [Test]
    public void Register_AddsPropToPropsList()
    {
        var mgr = new EntityService(CreateTestWorld());
        var prop = new TestPropEntity("prop", Vector2.Zero);

        mgr.Register(prop);

        Assert.That(mgr.Props, Does.Contain(prop));
    }

    [Test]
    public void Destroy_RemovesPropFromPropsList()
    {
        var mgr = new EntityService(CreateTestWorld());
        var prop = new TestPropEntity("prop", Vector2.Zero);
        mgr.Register(prop);

        mgr.Destroy(prop);
        mgr.ProcessPending();

        Assert.That(mgr.Props, Does.Not.Contain(prop));
    }

    [Test]
    public void Clear_RemovesPropFromPropsList()
    {
        var mgr = new EntityService(CreateTestWorld());
        var prop = new TestPropEntity("prop", Vector2.Zero);
        mgr.Register(prop);

        mgr.Clear();

        Assert.That(mgr.Props, Does.Not.Contain(prop));
    }
}