using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MonoGameLearning.Core.Settings;

public record ResolutionSetting(int Width, int Height);

public static class ResolutionSettings
{
    public static ResolutionSetting Current { get; private set; } = new(1024, 768);
    public static IReadOnlyList<ResolutionSetting> AvailableResolutions { get; private set; } = GetCommon4to3Resolutions();

    public static ResolutionSetting Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            TrySave(Current);
            return Current;
        }

        try
        {
            var json = File.ReadAllText(path);
            var setting = JsonSerializer.Deserialize<ResolutionSetting>(json);
            if (setting is not null && AvailableResolutions.Any(r => r == setting))
            {
                Current = setting;
                return Current;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Debug.WriteLine($"[ResolutionSettings] Failed to load settings: {ex.Message}");
        }

        Current = new(1024, 768);
        TrySave(Current);
        return Current;
    }

    public static void Save(ResolutionSetting setting)
    {
        Current = setting;
        TrySave(setting);
    }

    private static void TrySave(ResolutionSetting setting)
    {
        try
        {
            var dir = Path.GetDirectoryName(GetSettingsPath());
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            var json = JsonSerializer.Serialize(setting);
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"[ResolutionSettings] Failed to save settings: {ex.Message}");
        }
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

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "MonoGameLearning", "settings.json");
    }
}