#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Settings;

public static class SettingsService
{
    public static AudioSettings AudioSettings { get; private set; } = AudioSettings.Default;

public static string GetSettingsPath()
{
    var overrideDir = Environment.GetEnvironmentVariable("MGL_SETTINGS_DIR");
    if (overrideDir is not null)
        return Path.Combine(overrideDir, "settings.json");
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    return Path.Combine(appData, "MonoGameLearning", "settings.json");
}

    public static void Apply(GraphicsDeviceManager graphics, ResolutionSetting setting)
    {
        graphics.PreferredBackBufferWidth = setting.Width;
        graphics.PreferredBackBufferHeight = setting.Height;
        graphics.IsFullScreen = false;
        graphics.ApplyChanges();
    }

    internal static SettingsData? LoadSettings()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);

            // New wrapper format
            var wrapper = JsonSerializer.Deserialize<SettingsData>(json);
            if (wrapper?.Resolution is not null)
                return wrapper;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
        }

        return null;
    }

    internal static void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(GetSettingsPath());
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            var wrapper = new SettingsData
            {
                Resolution = ResolutionSettings.Current,
                Audio = AudioSettings
            };
            var json = JsonSerializer.Serialize(wrapper);
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
        }
    }

    public static void LoadAudio()
    {
        var data = LoadSettings();
        AudioSettings = data?.Audio?.Clamped() ?? AudioSettings.Default;
    }

    public static void SaveAudio(AudioSettings settings)
    {
        AudioSettings = settings.Clamped();
        SaveSettings();
    }
}