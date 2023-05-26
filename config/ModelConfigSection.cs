using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace PowershellGpt.Config;

public partial class ModelConfigSection : INamedConfigSection
{
    public static string GetSectionName() => "ModelConfig";

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.7f;

    [JsonPropertyName("maxTokenCount")]

    public int MaxTokenCount { get; set; } = 2000;

    [JsonPropertyName("nucleusSamplingFactor")]

    public float NucleusSamplingFactor { get; set; } = 0.95f;

    [JsonPropertyName("frequencyPenalty")]

    public float FrequencyPenalty { get; set; } = 0f;

    [JsonPropertyName("presencePenalty")]

    public float PresencePenalty { get; set; } = 0f;
}