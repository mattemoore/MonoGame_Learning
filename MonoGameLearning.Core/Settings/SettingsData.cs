#nullable enable
using System.Text.Json.Serialization;

namespace MonoGameLearning.Core.Settings;

public record SettingsData
{
    [JsonPropertyName("resolution")]
    public ResolutionSetting? Resolution { get; init; }

    [JsonPropertyName("audio")]
    public AudioSettings? Audio { get; init; }
}