using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Core.Rendering;
using MonoGameLearning.Core.UI;
using MonoGameLearning.Core.Settings;
using MonoGameLearning.Game.Entities.GoIndicator;

namespace MonoGameLearning.Game.Tests;

public class TestUiEntity : UiBase
{
    public override void Update(GameTime gameTime) { }
    public override void Render(RenderContext context) { }
    public override void DrawDebug(DebugDrawContext context) { }
}

[TestFixture]
public class GoIndicatorTests
{
    private static CollisionWorld2D CreateTestWorld() =>
        CollisionWorldFactory.Create(new RectangleF(0, 0, 2000, 600));

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

    [Test]
    public void GoIndicator_Anchor_StaysOnScreenAtEverySupportedResolution()
    {
        const float textureWidth = 512f;
        const int virtualWidth = 800;
        const int virtualHeight = 600;
        float scaledWidth = textureWidth * GoIndicatorEntity.SCALE;

        foreach (var res in SettingsService.AvailableResolutions)
        {
            float scale = res.Width / (float)virtualWidth;
            var anchor = GoIndicatorEntity.ComputeAnchorPosition(virtualWidth, virtualHeight, textureWidth, GoIndicatorEntity.SCALE, GoIndicatorEntity.MARGIN);

            float screenLeft = (anchor.X - scaledWidth / 2f) * scale;
            float screenRight = (anchor.X + scaledWidth / 2f) * scale;

            Assert.That(screenLeft, Is.GreaterThanOrEqualTo(0f), $"indicator left edge off-screen at {res.Width}x{res.Height}");
            Assert.That(screenRight, Is.LessThanOrEqualTo(res.Width), $"indicator right edge off-screen at {res.Width}x{res.Height}");
        }
    }

    [Test]
    public void GoIndicator_AnchorInPixelSpace_WouldOvershootScreen()
    {
        const float textureWidth = 512f;
        const int virtualWidth = 800;

        var anchor = GoIndicatorEntity.ComputeAnchorPosition(1024, 768, textureWidth, GoIndicatorEntity.SCALE, GoIndicatorEntity.MARGIN);
        float scale = 1024f / virtualWidth;
        float screenRight = (anchor.X + textureWidth * GoIndicatorEntity.SCALE / 2f) * scale;

        Assert.That(screenRight, Is.GreaterThan(1024f),
            "Anchoring to letterboxed pixel size double-scales the GO indicator off the right edge");
    }
}