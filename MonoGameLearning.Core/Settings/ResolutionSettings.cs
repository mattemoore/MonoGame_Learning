using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonoGameLearning.Core.Settings;

public record ResolutionSetting(int Width, int Height);

public static class ResolutionSettings
{
    public static ResolutionSetting Current { get; private set; } = new(1024, 768);
    public static IReadOnlyList<ResolutionSetting> AvailableResolutions { get; private set; } = GetCommon4to3Resolutions();

    public static ResolutionSetting Load()
    {
        var data = SettingsService.LoadSettings();
        if (data?.Resolution is not null && AvailableResolutions.Any(r => r == data.Resolution))
        {
            Current = data.Resolution;
            return Current;
        }

        Current = new(1024, 768);
        SettingsService.SaveSettings();
        return Current;
    }

    public static void Save(ResolutionSetting setting)
    {
        Current = setting;
        SettingsService.SaveSettings();
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