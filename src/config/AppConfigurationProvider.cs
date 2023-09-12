using System.Text.Json;

namespace PowershellGpt.Config.Provider;

public interface IAppConfigurationProvider
{
    public AppConfigSection AppConfig { get; }
    public void SaveAll();
    public void ClearAll();

}

public class AppConfigurationProvider : IAppConfigurationProvider
{

    private PsGptConfiguration _config;
    public AppConfigSection AppConfig => _config.AppConfig;
    
    public AppConfigurationProvider()
    {
        _config = GetConfig();
    }

    private PsGptConfiguration GetConfig()
    {
        if (File.Exists(ConfigFileLocation))
        {
            using (var fileStream = new FileStream(ConfigFileLocation, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < 3)
                {
                    ClearAll();
                }
                else
                {
                    return JsonSerializer.Deserialize<PsGptConfiguration>(fileStream) ?? _config;
                }    
            }
        }
        return new PsGptConfiguration();
    }

    public void SaveAll()
    {
        if (!Directory.Exists(ConfigFileFolder))
        {
            Directory.CreateDirectory(ConfigFileFolder);
        }
        using (var saveStream = new FileStream(ConfigFileLocation, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize<PsGptConfiguration>(saveStream, _config);
        }
    }

    public void ClearAll()
    {
        File.Delete(ConfigFileLocation);
        _config = new PsGptConfiguration();
    }
    
    private static string ConfigFileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt");
    private static string ConfigFileLocation => Path.Combine(ConfigFileFolder, "ps-gpt.config");
}