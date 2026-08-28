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

public class TestUiEntity : UiBase
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
        var entity = new TestUiEntity();
        Assert.That(entity, Is.InstanceOf<IUpdatable>());
    }

    [Test]
    public void UiBase_IsScreenRenderable()
    {
        var entity = new TestUiEntity();
        Assert.That(entity, Is.InstanceOf<IScreenRenderable>());
    }

    [Test]
    public void UiBase_IsNotWorldRenderable()
    {
        var entity = new TestUiEntity();
        Assert.That(entity, Is.Not.InstanceOf<IRenderable>());
    }

    [Test]
    public void UiBase_IsDebugDrawable()
    {
        var entity = new TestUiEntity();
        Assert.That(entity, Is.InstanceOf<IDebugDrawable>());
    }

    [Test]
    public void UiBase_IsNotEntity()
    {
        var entity = new TestUiEntity();
        Assert.That(entity, Is.Not.InstanceOf<Entity>());
    }

    [Test]
    public void UiBase_DefaultVisibilityIsTrue()
    {
        var entity = new TestUiEntity();
        Assert.That(entity.Visible, Is.True);
    }

    [Test]
    public void UiBase_CanToggleVisibility()
    {
        var entity = new TestUiEntity();
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