using MonoGameLearning.Core.Settings;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class ResolutionSettingsTests
{
    private static string _tempDir = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MGL-Tests-Resolution", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("MGL_SETTINGS_DIR", _tempDir);
        Directory.CreateDirectory(_tempDir);
    }

    [SetUp]
    public void ResetToDefault() => SettingsService.SaveResolution(new ResolutionSetting(1024, 768));

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        var path = SettingsService.GetSettingsPath();
        if (File.Exists(path))
            File.Delete(path);
        Environment.SetEnvironmentVariable("MGL_SETTINGS_DIR", null);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Test]
    public void Load_Default_Is1024x768()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.CurrentResolution.Width, Is.EqualTo(1024));
            Assert.That(SettingsService.CurrentResolution.Height, Is.EqualTo(768));
        });
    }

    [Test]
    public void SaveThenLoad_RoundTrips()
    {
        var original = new ResolutionSetting(800, 600);
        SettingsService.SaveResolution(original);

        var reloaded = SettingsService.LoadResolution();
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Width, Is.EqualTo(800));
            Assert.That(reloaded.Height, Is.EqualTo(600));
        });
    }

    [Test]
    public void Load_FileMissing_ReturnsDefault()
    {
        // delete persisted settings to simulate first launch (in the test's temp dir, never real app data)
        var path = SettingsService.GetSettingsPath();
        if (File.Exists(path))
            File.Delete(path);

        var loaded = SettingsService.LoadResolution();
        Assert.Multiple(() =>
        {
            Assert.That(loaded.Width, Is.EqualTo(1024));
            Assert.That(loaded.Height, Is.EqualTo(768));
        });
    }

    [Test]
    public void Save_Then_Load_UpdatesCurrent()
    {
        SettingsService.SaveResolution(new ResolutionSetting(800, 600));
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.CurrentResolution.Width, Is.EqualTo(800));
            Assert.That(SettingsService.CurrentResolution.Height, Is.EqualTo(600));
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
        SettingsService.SaveResolution(new ResolutionSetting(1920, 1080));
        SettingsService.LoadResolution();
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.CurrentResolution.Width, Is.EqualTo(1024));
            Assert.That(SettingsService.CurrentResolution.Height, Is.EqualTo(768));
        });
    }

    [Test]
    public void AvailableResolutions_AllAre4to3()
    {
        foreach (var r in SettingsService.AvailableResolutions)
            Assert.That(r.Width * 3, Is.EqualTo(r.Height * 4), $"{r.Width}x{r.Height} is not 4:3");
    }

    [Test]
    public void LoadResolution_Then_LoadAudio_PreservesPersistedAudio_AtStartupOrder()
    {
        // Simulate startup order: GameLoop static field init calls LoadResolution() (GameLoop.cs:36-37)
        // before Initialize() calls LoadAudio() (GameLoop.cs:66). An audio-only settings file must not
        // have its audio clobbered by LoadResolution's fallback SaveSettings().
        File.WriteAllText(SettingsService.GetSettingsPath(), """{"audio":{"SfxVolume":0.25,"MusicVolume":0.5}}""");

        SettingsService.LoadResolution();
        SettingsService.LoadAudio();

        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.AudioSettings.SfxVolume, Is.EqualTo(0.25f));
            Assert.That(SettingsService.AudioSettings.MusicVolume, Is.EqualTo(0.5f));
        });
    }
}