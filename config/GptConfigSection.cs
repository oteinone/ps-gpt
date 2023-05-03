using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace PowershellGpt.Config;

public sealed class GptConfigSection : ConfigurationSection
{
    public const string SectionName = "GptConfig";

    public GptConfigSection()
    {
    }

    [ConfigurationProperty("endpointType",
     IsRequired = false,
     IsKey = false)]

    public GptEndpointType? EndpointType
    {
        get
        {
            return (GptEndpointType?) this["endpointType"];
        }
        set
        {
            this["endpointType"] = value;
        }
    }

    [ConfigurationProperty("defaultAppPrompt",
     IsRequired = false,
     IsKey = true)]
    public string DefaultAppPrompt
    {
        get
        {
            return (string) this["defaultAppPrompt"];
        }
        set
        {
            this["defaultAppPrompt"] = value;
        }
    }

    [ConfigurationProperty("endpointUrl",
     IsRequired = false,
     IsKey = true)]
    public string EndpointUrl
    {
        get
        {
            return (string) this["endpointUrl"];
        }
        set
        {
            this["endpointUrl"] = value;
        }
    }

    [ConfigurationProperty("model",
     IsRequired = false,
     IsKey = true)]
    public string Model
    {
        get
        {
            return (string) this["model"];
        }
        set
        {
            this["model"] = value;
        }
    }

    [ConfigurationProperty("apikey",
     
     IsRequired = false,
     IsKey = true)]
    public string? ApiKey
    {
        get
        {
            return UnProtectKey((string) this["apikey"]);
        }
        set
        {
            this["apikey"] = ProtectKey(value);
        }
    }

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
}



public enum GptEndpointType {
    AzureOpenAI,
    OpenAIApi
}