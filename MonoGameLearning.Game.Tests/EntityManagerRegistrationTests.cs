using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Game.Tests;

internal sealed class StubCombatActor(string name, Vector2 position, int width, int height)
    : CombatActorBase(name, position, width, height, null!, 1f, 100, new(null!, null!, null!, null!, null!, null!))
{
    public override void Update(GameTime gameTime) { }
    protected override bool IsIncapacitated => false;
    protected override bool IsInKnockedDownState => false;
    protected override bool IsInHurtState => false;
    protected override bool IsInDyingState => false;
    protected override void FireKnockdownCompleted() { }
    protected override void FireHurtCompleted() { }
    protected override void FireDeathCompleted() { }
}

internal sealed class GenericCollidableEntity(string name, Vector2 position, int width, int height)
    : Entity(name, position, width, height), ICollisionActor, IUpdatable, IRenderable
{
    public int Id => GetHashCode();
    public CollisionShape2D Shape => new(new BoundingBox2D(new Vector2(Frame.X, Frame.Y), new Vector2(Frame.Right, Frame.Bottom)));
    public void Update(GameTime gameTime) { }
    public void Render(RenderContext context) { }
}

[TestFixture]
public class EntityManagerRegistrationTests
{
    private static CollisionWorld2D CreateTestWorld()
    {
        var world = new CollisionWorld2D();
        var bb = new BoundingBox2D(new Vector2(0, 0), new Vector2(2000, 600));
        world.AddLayer("actors", new Layer(new QuadTreeSpace(bb)));
        world.AddLayer("props", new Layer(new QuadTreeSpace(bb)));
        world.EnableCollisionBetweenLayers("actors", "props");
        return world;
    }

    [Test]
    public void CombatActor_RegisteredInActorCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.ActorCollidables, Does.Contain(actor));
            Assert.That(mgr.PropCollidables, Is.Empty);
        });
    }

    [Test]
    public void CombatActor_NotInPropCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.That(mgr.PropCollidables, Is.Empty);
    }

    [Test]
    public void GenericCollidable_NotInActorOrPropLists()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var entity = new GenericCollidableEntity("g", Vector2.Zero, 50, 50);

        mgr.Register(entity);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.ActorCollidables, Is.Empty);
            Assert.That(mgr.PropCollidables, Is.Empty);
        });
    }

    [Test]
    public void MultipleCombatActors_AllInActorCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var a1 = new StubCombatActor("a1", Vector2.Zero, 50, 50);
        var a2 = new StubCombatActor("a2", Vector2.Zero, 50, 50);

        mgr.Register(a1);
        mgr.Register(a2);

        Assert.That(mgr.ActorCollidables, Has.Count.EqualTo(2));
    }

    [Test]
    public void ScreenRenderable_NotInWorldRenderables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var ui = new TestUiEntity("ui");

        mgr.Register(ui);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.ScreenRenderables, Does.Contain(ui));
            Assert.That(mgr.Renderables, Does.Not.Contain(ui));
        });
    }

    [Test]
    public void WorldRenderable_NotInScreenRenderables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);

        Assert.That(mgr.ScreenRenderables, Is.Empty);
    }

    [Test]
    public void UiBase_OnlyScreenRenderable_NotWorldRenderable()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var ui = new TestUiEntity("ui");

        mgr.Register(ui);

        Assert.Multiple(() =>
        {
            Assert.That(mgr.ScreenRenderables, Does.Contain(ui));
            Assert.That(mgr.Renderables, Does.Not.Contain(ui));
        });
    }

    [Test]
    public void Clear_RemovesFromActorCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);
        mgr.Register(actor);

        mgr.Clear();

        Assert.That(mgr.ActorCollidables, Does.Not.Contain(actor));
    }

    [Test]
    public void Clear_RemovesFromScreenRenderables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var ui = new TestUiEntity("ui");
        mgr.Register(ui);

        mgr.Clear();

        Assert.That(mgr.ScreenRenderables, Is.Empty);
    }

    [Test]
    public void Destroy_RemovesFromActorCollidables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);
        mgr.Register(actor);

        mgr.Destroy(actor);
        mgr.ProcessPending();

        Assert.That(mgr.ActorCollidables, Does.Not.Contain(actor));
    }

    [Test]
    public void Destroy_RemovesFromScreenRenderables()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var ui = new TestUiEntity("ui");
        mgr.Register(ui);

        mgr.Destroy(ui);
        mgr.ProcessPending();

        Assert.That(mgr.ScreenRenderables, Is.Empty);
    }

    [Test]
    public void Destroy_LeavesOtherActors()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var a1 = new StubCombatActor("a1", Vector2.Zero, 50, 50);
        var a2 = new StubCombatActor("a2", Vector2.Zero, 50, 50);
        mgr.Register(a1);
        mgr.Register(a2);

        mgr.Destroy(a1);
        mgr.ProcessPending();

        Assert.Multiple(() =>
        {
            Assert.That(mgr.ActorCollidables, Has.Count.EqualTo(1));
            Assert.That(mgr.ActorCollidables, Does.Contain(a2));
        });
    }

    [Test]
    public void Register_Duplicate_NoOp()
    {
        var mgr = new EntityManager(CreateTestWorld());
        var actor = new StubCombatActor("a", Vector2.Zero, 50, 50);

        mgr.Register(actor);
        mgr.Register(actor);

        Assert.That(mgr.ActorCollidables, Has.Count.EqualTo(1));
    }
}