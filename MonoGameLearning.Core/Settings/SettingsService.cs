#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace MonoGameLearning.Core.Settings;

public static class SettingsService
{
    public static AudioSettings AudioSettings { get; private set; } = AudioSettings.Default;
    public static ResolutionSetting CurrentResolution { get; private set; } = new(1024, 768);
    public static IReadOnlyList<ResolutionSetting> AvailableResolutions { get; } = GetCommon4to3Resolutions();

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

    internal static SettingsData LoadSettings()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return new SettingsData();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Debug.WriteLine($"[SettingsService] Failed to load settings: {ex.Message}");
            return new SettingsData();
        }
    }

    internal static void SaveSettings()
    {
        WriteSettings(CurrentResolution, AudioSettings);
    }

    private static void WriteSettings(ResolutionSetting resolution, AudioSettings audio)
    {
        try
        {
            var dir = Path.GetDirectoryName(GetSettingsPath());
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            var wrapper = new SettingsData
            {
                Resolution = resolution,
                Audio = audio
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
        AudioSettings = data.Audio?.Clamped() ?? AudioSettings.Default;
    }

    public static void SaveAudio(AudioSettings settings)
    {
        AudioSettings = settings.Clamped();
        SaveSettings();
    }

    public static ResolutionSetting LoadResolution()
    {
        var data = LoadSettings();
        if (data.Resolution is { } res && AvailableResolutions.Any(r => r == res))
        {
            CurrentResolution = res;
            return res;
        }

        // No cross-writing: LoadResolution must not mutate AudioSettings (that state belongs to
        // LoadAudio/SaveAudio and is loaded independently during Initialize). The fallback write
        // below persists the default resolution while keeping the file's own audio section intact.
        CurrentResolution = new(1024, 768);
        WriteSettings(CurrentResolution, data.Audio?.Clamped() ?? AudioSettings.Default);
        return CurrentResolution;
    }

    public static void SaveResolution(ResolutionSetting setting)
    {
        CurrentResolution = setting;
        SaveSettings();
    }

    private static List<ResolutionSetting> GetCommon4to3Resolutions() =>
    [
        new(640, 480),
        new(800, 600),
        new(1024, 768),
        new(1280, 960),
        new(1400, 1050),
        new(1600, 1200),
    ];
}