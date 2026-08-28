using System;
using Microsoft.Xna.Framework.Content;
using MonoGameLearning.Core.Rendering;
using NUnit.Framework;

namespace MonoGameLearning.Game.Tests;

internal sealed class NoopServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}

internal sealed class TrackingContentManager : ContentManager
{
    public int LoadCount;
    public TrackingContentManager() : base(new NoopServiceProvider()) { }
    public override T Load<T>(string assetName) { LoadCount++; return default!; }
}

internal sealed class FailOnceContentManager : ContentManager
{
    public int LoadCount;
    public FailOnceContentManager() : base(new NoopServiceProvider()) { }
    public override T Load<T>(string assetName)
    {
        LoadCount++;
        if (LoadCount == 1) throw new InvalidOperationException("asset missing");
        return default!;
    }
}

[TestFixture]
public class StaticTextureAssetTests
{
    [Test]
    public void Load_LoadsTextureOnce_AcrossRepeatedCalls()
    {
        var asset = new StaticTextureAsset("images/arrow");
        var content = new TrackingContentManager();

        asset.Load(content);
        asset.Load(content);

        Assert.That(content.LoadCount, Is.EqualTo(1));
    }

    [Test]
    public void Load_AfterFailedLoad_RetriesOnNextCall()
    {
        var asset = new StaticTextureAsset("images/arrow");
        var content = new FailOnceContentManager();

        Assert.Throws<InvalidOperationException>(() => asset.Load(content));
        Assert.DoesNotThrow(() => asset.Load(content));

        Assert.That(content.LoadCount, Is.EqualTo(2),
            "A failed load must not poison the loaded flag — the next call must retry.");
    }
}