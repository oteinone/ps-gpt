using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace PowershellGpt.Config;

public partial class AppConfiguration
{
    public partial class GptConfigSection : ConfigurationSection, INamedConfigSection
    {
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

        public string CensoredApiKey => new String(ApiKey?.Select(k => '*').ToArray() ?? new char[0]);
        

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

        [ConfigurationProperty("defaultSystemPrompt",
        IsRequired = false,
        IsKey = true)]
        public string DefaultSystemPrompt
        {
            get
            {
                return (string) this["defaultSystemPrompt"];
            }
            set
            {
                this["defaultSystemPrompt"] = value;
            }
        }

        public string MultilineIndicator => "`";
        public int ResponsePadding => 2;
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

        public static string GetSectionName() => "GptConfig";
    }
}
public enum GptEndpointType {
    AzureOpenAI,
    OpenAIApi
}