using MonoGameLearning.Core.Settings;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class AudioSettingsTests
{
    private static string _tempDir = null!;

    private static string GetTestPath() => SettingsService.GetSettingsPath();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MGL-Tests-Audio", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("MGL_SETTINGS_DIR", _tempDir);
        Directory.CreateDirectory(_tempDir);
    }

    [SetUp]
    public void Setup()
    {
        CleanupFile();
        SettingsService.LoadAudio();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupFile();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        CleanupFile();
        Environment.SetEnvironmentVariable("MGL_SETTINGS_DIR", null);
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static void CleanupFile()
    {
        var path = GetTestPath();
        if (File.Exists(path))
            File.Delete(path);
    }

    [Test]
    public void Default_IsOneOne()
    {
        var def = AudioSettings.Default;
        Assert.Multiple(() =>
        {
            Assert.That(def.SfxVolume, Is.EqualTo(1.0f));
            Assert.That(def.MusicVolume, Is.EqualTo(1.0f));
        });
    }

    [Test]
    public void Clamped_ZeroToOne()
    {
        var clamped = new AudioSettings(-0.5f, 1.5f).Clamped();
        Assert.Multiple(() =>
        {
            Assert.That(clamped.SfxVolume, Is.EqualTo(0f));
            Assert.That(clamped.MusicVolume, Is.EqualTo(1f));
        });
    }

    [Test]
    public void RoundTrip_SaveThenLoad()
    {
        var saved = new AudioSettings(0.5f, 0.75f);
        SettingsService.SaveAudio(saved);
        SettingsService.LoadAudio();
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.AudioSettings.SfxVolume, Is.EqualTo(0.5f));
            Assert.That(SettingsService.AudioSettings.MusicVolume, Is.EqualTo(0.75f));
        });
    }

    [Test]
    public void Load_FileMissing_ReturnsDefault()
    {
        var path = GetTestPath();
        if (File.Exists(path))
            File.Delete(path);
        SettingsService.LoadAudio();
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.AudioSettings.SfxVolume, Is.EqualTo(1.0f));
            Assert.That(SettingsService.AudioSettings.MusicVolume, Is.EqualTo(1.0f));
        });
    }

    [Test]
    public void Save_ClampsValues()
    {
        SettingsService.SaveAudio(new AudioSettings(-0.1f, 1.5f));
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.AudioSettings.SfxVolume, Is.EqualTo(0f));
            Assert.That(SettingsService.AudioSettings.MusicVolume, Is.EqualTo(1f));
        });
    }

    [Test]
    public void CorruptJson_FallsBackToDefaults()
    {
        File.WriteAllText(GetTestPath(), "not valid json");
        SettingsService.LoadAudio();
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.AudioSettings.SfxVolume, Is.EqualTo(1.0f));
            Assert.That(SettingsService.AudioSettings.MusicVolume, Is.EqualTo(1.0f));
        });
    }

    [Test]
    public void LoadAudio_AudioOnlySettings_PreservesPersistedAudio()
    {
        // A settings file with only the audio section (no resolution) must not drop the audio.
        File.WriteAllText(GetTestPath(), """{"audio":{"SfxVolume":0.25,"MusicVolume":0.5}}""");
        SettingsService.LoadAudio();
        Assert.Multiple(() =>
        {
            Assert.That(SettingsService.AudioSettings.SfxVolume, Is.EqualTo(0.25f));
            Assert.That(SettingsService.AudioSettings.MusicVolume, Is.EqualTo(0.5f));
        });
    }
}