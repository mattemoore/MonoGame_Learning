using MonoGameLearning.Core.Settings;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class ResolutionSettingsTests
{
    [SetUp]
    public void ResetToDefault() => ResolutionSettings.Save(new ResolutionSetting(1024, 768));

    [Test]
    public void Load_Default_Is1024x768()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ResolutionSettings.Current.Width, Is.EqualTo(1024));
            Assert.That(ResolutionSettings.Current.Height, Is.EqualTo(768));
        });
    }

    [Test]
    public void SaveThenLoad_RoundTrips()
    {
        var original = new ResolutionSetting(800, 600);
        ResolutionSettings.Save(original);

        var reloaded = ResolutionSettings.Load();
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Width, Is.EqualTo(800));
            Assert.That(reloaded.Height, Is.EqualTo(600));
        });
    }

    [Test]
    public void Load_FileMissing_ReturnsDefault()
    {
        // delete persisted settings to simulate first launch
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(appData, "MonoGameLearning", "settings.json");
        if (File.Exists(path))
            File.Delete(path);

        var loaded = ResolutionSettings.Load();
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Width, Is.EqualTo(1024));
            Assert.That(loaded.Height, Is.EqualTo(768));
        });
    }

    [Test]
    public void Save_Then_Load_UpdatesCurrent()
    {
        ResolutionSettings.Save(new ResolutionSetting(800, 600));
        Assert.Multiple(() =>
        {
            Assert.That(ResolutionSettings.Current.Width, Is.EqualTo(800));
            Assert.That(ResolutionSettings.Current.Height, Is.EqualTo(600));
        });
    }

    [Test]
    public void ResolutionSetting_Equality_ByValue()
    {
        var a = new ResolutionSetting(800, 600);
        var b = new ResolutionSetting(800, 600);
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Load_Non4to3Resolution_FallsBackToDefault()
    {
        ResolutionSettings.Save(new ResolutionSetting(1920, 1080));
        ResolutionSettings.Load();
        Assert.Multiple(() =>
        {
            Assert.That(ResolutionSettings.Current.Width, Is.EqualTo(1024));
            Assert.That(ResolutionSettings.Current.Height, Is.EqualTo(768));
        });
    }

    [Test]
    public void AvailableResolutions_AllAre4to3()
    {
        foreach (var r in ResolutionSettings.AvailableResolutions)
            Assert.That(r.Width * 3, Is.EqualTo(r.Height * 4), $"{r.Width}x{r.Height} is not 4:3");
    }
}