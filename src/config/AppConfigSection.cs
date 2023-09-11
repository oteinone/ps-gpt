using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace PowershellGpt.Config;

public partial class AppConfigSection : INamedConfigSection
{
    public GptEndpointType? EndpointType { get; set; }

    [JsonPropertyName("endpointUrl")]
    public string? EndpointUrl { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("defaultAppPrompt")]
    public string? DefaultAppPrompt { get; set; }

    [JsonPropertyName("defaultSystemPrompt")]
    public string? DefaultSystemPrompt { get; set; }
    
    [JsonIgnore]
    public string? ApiKey
    {
        get
        {
            return UnProtectKey(ApiKeyStorageValue);
        }
        set
        {
            ApiKeyStorageValue = ProtectKey(value);
        }
    }

    [JsonPropertyName("apikey")]
    public string? ApiKeyStorageValue { get; set; }

    [JsonIgnore]
    public string CensoredApiKey => new String(ApiKey?.Select(k => '*').ToArray() ?? new char[0]);

    [JsonIgnore]
    public string MultilineIndicator => "`";

    [JsonIgnore]
    public int ResponsePadding => 2;

    [JsonIgnore]
    public string[] ExitTerms => new string[] { "exit", "quit", "q", "done" };

    private static string? ProtectKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return key;
        }
        return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser));
    }

    private static string? UnProtectKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return key;
        }
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(key), null, DataProtectionScope.CurrentUser));
    }

    [JsonPropertyName("modelConfig")]
    public ModelConfigSection ModelConfig { get; set; } = new ModelConfigSection();

    public static string GetSectionName() => "GptConfig";
}
public enum GptEndpointType {
    AzureOpenAI,
    OpenAIApi
}