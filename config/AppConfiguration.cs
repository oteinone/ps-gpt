using System.Configuration;

namespace PowershellGpt.Config;

public partial class AppConfiguration
{
    private static Lazy<Configuration> execonfig = new Lazy<Configuration>(() => ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None));
    private static Configuration Config => execonfig.Value;

    private static Lazy<AppConfiguration.GptConfigSection> _gptConfig = new Lazy<AppConfiguration.GptConfigSection>(
        () => AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>());

    public static AppConfiguration.GptConfigSection GptConfig => _gptConfig.Value;


    public static T GetOrCreateConfigSection<T>()
        where T: ConfigurationSection, INamedConfigSection, new()
    {
        if (Config.Sections[T.GetSectionName()] == null)
        {
            Config.Sections.Add(T.GetSectionName(), new T());
        }
        return (T) Config.Sections[T.GetSectionName()];
    }

    public static void SaveAll()
    {
        Config.Save(ConfigurationSaveMode.Modified);
    }

    public static void ClearAll()
    {
        Config.Sections.Remove(AppConfiguration.GptConfigSection.GetSectionName());
        Config.Sections.Remove(ModelConfigSection.GetSectionName());
    }

    public static void Clear<T>()
        where T: ConfigurationSection, INamedConfigSection, new()
    {
        Config.Sections.Remove(T.GetSectionName());
    }

    public partial class GptConfigSection
    {
        // Implementation in GptConfigSection.cs
    }

    public partial class ModelConfigSection
    {
        // Implementation in ModelConfigSection
    }

}