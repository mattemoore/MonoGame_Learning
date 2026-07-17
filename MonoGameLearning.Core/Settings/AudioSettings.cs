using System;

namespace MonoGameLearning.Core.Settings;

public record AudioSettings(float SfxVolume, float MusicVolume)
{
    public static readonly AudioSettings Default = new(1.0f, 1.0f);

    public AudioSettings Clamped() => new(
        Math.Clamp(SfxVolume, 0f, 1f),
        Math.Clamp(MusicVolume, 0f, 1f));
}