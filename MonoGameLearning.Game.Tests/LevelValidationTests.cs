using Microsoft.Xna.Framework;
using MonoGameLearning.Core.Entities;
using MonoGameLearning.Game.Levels;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class LevelValidationTests
{
    [Test]
    public void ValidateConnectivity_WithConnectedBackgrounds_ShouldPass()
    {
        var bg1 = new BackgroundEntity("bg1", null, new Vector2(0, 0), 100, 100);
        var bg2 = new BackgroundEntity("bg2", null, new Vector2(100, 0), 100, 100);

        var backgrounds = new List<BackgroundEntity> { bg1, bg2 };

        Assert.DoesNotThrow(() => Level.ValidateConnectivity(backgrounds));
    }

    [Test]
    public void ValidateConnectivity_WithDisconnectedBackgrounds_ShouldThrow()
    {
        var bg1 = new BackgroundEntity("bg1", null, new Vector2(0, 0), 100, 100);
        var bg2 = new BackgroundEntity("bg2", null, new Vector2(200, 0), 100, 100);

        var backgrounds = new List<BackgroundEntity> { bg1, bg2 };

        var ex = Assert.Throws<InvalidOperationException>(() => Level.ValidateConnectivity(backgrounds));
        Assert.That(ex.Message, Contains.Substring("are not connected"));
    }

    [Test]
    public void ValidateWaveDefs_WithValidIncreasingTriggers_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 200f, Enemies: []),
            new(TriggerX: 400f, FightAreaWidth: 200f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_WithNullList_ShouldThrow()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(null, 800f));
        Assert.That(ex.Message, Contains.Substring("must not be null or empty"));
    }

    [Test]
    public void ValidateWaveDefs_WithEmptyList_ShouldThrow()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs([], 800f));
        Assert.That(ex.Message, Contains.Substring("must not be null or empty"));
    }

    [Test]
    public void ValidateWaveDefs_WithEqualTriggers_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 500f, FightAreaWidth: 200f, Enemies: []),
            new(TriggerX: 500f, FightAreaWidth: 200f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("strictly increasing"));
    }

    [Test]
    public void ValidateWaveDefs_WithDecreasingTriggers_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 800f, FightAreaWidth: 200f, Enemies: []),
            new(TriggerX: 300f, FightAreaWidth: 200f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("strictly increasing"));
    }

    [Test]
    public void ValidateWaveDefs_WithZeroFightAreaWidth_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 0f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("FightAreaWidth"));
    }

    [Test]
    public void ValidateWaveDefs_WithNegativeFightAreaWidth_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: -50f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("FightAreaWidth"));
    }

    [Test]
    public void ValidateWaveDefs_WithSingleWave_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 200f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_WithManyWavesIncreasing_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 200f, Enemies: []),
            new(TriggerX: 400f, FightAreaWidth: 250f, Enemies: []),
            new(TriggerX: 900f, FightAreaWidth: 300f, Enemies: []),
            new(TriggerX: 1500f, FightAreaWidth: 200f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_WithGapInTriggers_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 200f, Enemies: []),
            new(TriggerX: 2000f, FightAreaWidth: 200f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_FightAreaWidthExceedsViewport_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 900f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("exceeds viewport width"));
    }

    [Test]
    public void ValidateWaveDefs_FightAreaWidthEqualsViewport_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 800f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_OverlappingFightAreas_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 300f, FightAreaWidth: 500f, Enemies: []),
            new(TriggerX: 400f, FightAreaWidth: 500f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("overlaps"));
    }

    [Test]
    public void ValidateWaveDefs_NonOverlappingFightAreas_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 300f, FightAreaWidth: 500f, Enemies: []),
            new(TriggerX: 800f, FightAreaWidth: 400f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_AdjacentFightAreas_ShouldPass()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 300f, FightAreaWidth: 500f, Enemies: []),
            new(TriggerX: 800f, FightAreaWidth: 500f, Enemies: [])
        };

        Assert.DoesNotThrow(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
    }

    [Test]
    public void ValidateWaveDefs_OverlapOnLeftSide_ShouldThrow()
    {
        var waveDefs = new List<WaveDef>
        {
            new(TriggerX: 500f, FightAreaWidth: 400f, Enemies: []),
            new(TriggerX: 600f, FightAreaWidth: 400f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() => WaveDef.ValidateWaveDefs(waveDefs, 800f));
        Assert.That(ex.Message, Contains.Substring("overlaps"));
    }

    [Test]
    public void WaveDefValidation_CalledInLevelConstructor_WithInvalidWaves_ShouldThrow()
    {
        var bg = new BackgroundEntity("bg", null, Vector2.Zero, 100, 100);
        var invalidWaves = new List<WaveDef>
        {
            new(TriggerX: 500f, FightAreaWidth: 200f, Enemies: []),
            new(TriggerX: 300f, FightAreaWidth: 200f, Enemies: [])
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = new TestLevel(invalidWaves, endTriggerX: 1000f);
        });
        Assert.That(ex.Message, Contains.Substring("strictly increasing"));
    }

    [Test]
    public void LevelConstructor_WithValidWaves_ShouldConstruct()
    {
        var waves = new List<WaveDef>
        {
            new(TriggerX: 100f, FightAreaWidth: 200f, Enemies: [])
        };

        var level = new TestLevel(waves, endTriggerX: 500f);
        Assert.That(level.WaveDefs, Is.EqualTo(waves));
        Assert.That(level.EndTriggerX, Is.EqualTo(500f));
    }
}
