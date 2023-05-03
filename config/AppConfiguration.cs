using System.Configuration;

namespace PowershellGpt.Config;

public static class AppConfiguration
{
    private static Lazy<Configuration> execonfig = new Lazy<Configuration>(() => ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
    private static Configuration Config => execonfig.Value;

    public static GptConfigSection GptConfig
    {
        get
        {
            if (Config.Sections[GptConfigSection.SectionName] == null)
            {
                Config.Sections.Add(GptConfigSection.SectionName, new GptConfigSection());
            }
            return (GptConfigSection) Config.Sections[GptConfigSection.SectionName];
        }
    }

    public static void Save()
    {
        Config.Save(ConfigurationSaveMode.Modified);
    }

    public static void Clear()
    {
        Config.Sections.Remove(GptConfigSection.SectionName);
    }
}

