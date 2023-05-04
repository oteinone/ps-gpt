using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace PowershellGpt.Config;

public partial class AppConfiguration
{
    public partial class ModelConfigSection : ConfigurationSection, INamedConfigSection
    {
        public static string GetSectionName() => "ModelConfig";

        public ModelConfigSection()
        {
        }

        [ConfigurationProperty("temperature",
        IsRequired = false,
        DefaultValue = 0.7f,
        IsKey = false)]
        public float Temperature
        {
            get
            {
                return (float) this["temperature"];
            }
            set
            {
                this["temperature"] = value;
            }
        }

        [ConfigurationProperty("maxTokenCount",
        IsRequired = false,
        DefaultValue = 2000,
        IsKey = false)]

        public int MaxTokenCount
        {
            get
            {
                return (int) this["maxTokenCount"];
            }
            set
            {
                this["maxTokenCount"] = value;
            }
        }

        [ConfigurationProperty("nucleusSamplingFactor",
        IsRequired = false,
        DefaultValue = 0.95f,
        IsKey = false)]

        public float NucleusSamplingFactor
        {
            get
            {
                return (float) this["nucleusSamplingFactor"];
            }
            set
            {
                this["nucleusSamplingFactor"] = value;
            }
        }

        [ConfigurationProperty("frequencyPenalty",
        IsRequired = false,
        DefaultValue = 0f,
        IsKey = false)]

        public float FrequencyPenalty
        {
            get
            {
                return (float) this["frequencyPenalty"];
            }
            set
            {
                this["frequencyPenalty"] = value;
            }
        }

        [ConfigurationProperty("presencePenalty",
        IsRequired = false,
        DefaultValue = 0f,
        IsKey = false)]

        public float PresencePenalty
        {
            get
            {
                return (float) this["presencePenalty"];
            }
            set
            {
                this["presencePenalty"] = value;
            }
        }
    }
}