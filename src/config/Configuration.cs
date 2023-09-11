using System.Text.Json.Serialization;

namespace PowershellGpt.Config;

public class PsGptConfiguration
{
    [JsonPropertyName("appConfig")]
    public AppConfigSection AppConfig { get; set; } = new AppConfigSection();
}