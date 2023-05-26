using System.Text.Json;

namespace PowershellGpt.Config;

public class AppConfiguration
{
    
    private static PsGptConfiguration Configuration { get; set; } = GetConfig();
    public static AppConfigSection AppConfig => Configuration.AppConfig;
    public static ModelConfigSection ModelConfig => Configuration.ModelConfig;

    private static string ConfigFileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt");
    private static string ConfigFileLocation => Path.Combine(ConfigFileFolder, "ps-gpt.config");

    private static PsGptConfiguration GetConfig()
    {
        if (File.Exists(ConfigFileLocation))
        {
            bool invalidFile = false;
            using (var fileStream = new FileStream(ConfigFileLocation, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < 3)
                {
                    invalidFile = true;
                }
                else
                {
                    var fileConfig = JsonSerializer.Deserialize<PsGptConfiguration>(fileStream);
                    if (fileConfig != null) return fileConfig;
                }    
            }
            if (invalidFile) ClearAll();
        }
        return new PsGptConfiguration();
    }

    public static void SaveAll()
    {
        if (!Directory.Exists(ConfigFileFolder))
        {
            Directory.CreateDirectory(ConfigFileFolder);
        }

        using (var saveStream = new FileStream(ConfigFileLocation, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize<PsGptConfiguration>(saveStream, Configuration);
        }
    }

    public static void ClearAll()
    {
        File.Delete(ConfigFileLocation);
    }
}