using System.Text.Json;

namespace PowershellGpt.Config;

public class AppConfiguration
{
    public static AppConfigSection AppConfig => _configuration.AppConfig;

    private static PsGptConfiguration _configuration = GetConfig();

    private static string ConfigFileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt");
    private static string ConfigFileLocation => Path.Combine(ConfigFileFolder, "ps-gpt.config");

    private static PsGptConfiguration GetConfig()
    {
        var config = new PsGptConfiguration();
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
                    config = JsonSerializer.Deserialize<PsGptConfiguration>(fileStream) ?? config;
                }    
            }
        }
        return config;
    }

    public static void SaveAll()
    {
        if (!Directory.Exists(ConfigFileFolder))
        {
            Directory.CreateDirectory(ConfigFileFolder);
        }
        using (var saveStream = new FileStream(ConfigFileLocation, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize<PsGptConfiguration>(saveStream, _configuration);
        }
    }

    public static void ClearAll()
    {
        File.Delete(ConfigFileLocation);
        _configuration = new PsGptConfiguration();
    }
}