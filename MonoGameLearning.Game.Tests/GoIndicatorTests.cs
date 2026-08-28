using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Collisions.QuadTree;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Game.Entities.GoIndicator;

namespace MonoGameLearning.Game.Tests;

public class TestUiEntity(string name) : UiBase(name, Vector2.Zero, 64, 64)
{
    public override void Update(GameTime gameTime) { }
    public override void Render(RenderContext context) { }
}

[TestFixture]
public class GoIndicatorTests
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
    public void UiBase_IsUpdatable()
    {
        var entity = new TestUiEntity("test");
        Assert.That(entity, Is.InstanceOf<IUpdatable>());
    }

    [Test]
    public void UiBase_IsScreenRenderable()
    {
        var entity = new TestUiEntity("test");
        Assert.That(entity, Is.InstanceOf<IScreenRenderable>());
    }

    [Test]
    public void UiBase_IsNotWorldRenderable()
    {
        var entity = new TestUiEntity("test");
        Assert.That(entity, Is.Not.InstanceOf<IRenderable>());
    }

    [Test]
    public void UiBase_IsDebugDrawable()
    {
        var entity = new TestUiEntity("test");
        Assert.That(entity, Is.InstanceOf<IDebugDrawable>());
    }

    [Test]
    public void UiBase_EntityManager_RegistersInUpdatables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var entity = new TestUiEntity("test");

        mgr.Register(entity);

        Assert.That(mgr.Updatables, Does.Contain(entity));
    }

    [Test]
    public void UiBase_EntityManager_RegistersInScreenRenderables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var entity = new TestUiEntity("test");

        mgr.Register(entity);

        Assert.That(mgr.ScreenRenderables, Does.Contain(entity));
        Assert.That(mgr.Renderables, Does.Not.Contain(entity));
    }

    [Test]
    public void UiBase_EntityManager_RegistersInDebugDrawables()
    {
        var mgr = new EntityService(CreateTestWorld());
        var entity = new TestUiEntity("test");

        mgr.Register(entity);

        Assert.That(mgr.DebugDrawables, Does.Contain(entity));
    }

    [Test]
    public void UiBase_EntityManager_Clear_RemovesFromAllLists()
    {
        var mgr = new EntityService(CreateTestWorld());
        var entity = new TestUiEntity("test");
        mgr.Register(entity);

        mgr.Clear();

        Assert.That(mgr.ScreenRenderables, Does.Not.Contain(entity));
        Assert.That(mgr.Updatables, Does.Not.Contain(entity));
        Assert.That(mgr.Renderables, Does.Not.Contain(entity));
        Assert.That(mgr.DebugDrawables, Does.Not.Contain(entity));
    }

    [Test]
    public void UiBase_DefaultVisibilityIsTrue()
    {
        var entity = new TestUiEntity("test");
        Assert.That(entity.Visible, Is.True);
    }

    [Test]
    public void UiBase_CanToggleVisibility()
    {
        var entity = new TestUiEntity("test");
        entity.Visible = false;
        Assert.That(entity.Visible, Is.False);
        entity.Visible = true;
        Assert.That(entity.Visible, Is.True);
    }

    [Test]
    public void GoIndicator_Constants_AreDefined()
    {
        Assert.That(GoIndicatorEntity.SCALE, Is.EqualTo(0.3f));
        Assert.That(GoIndicatorEntity.MARGIN, Is.EqualTo(20));
        Assert.That(GoIndicatorEntity.FLASH_PERIOD, Is.EqualTo(0.8f));
    }
}