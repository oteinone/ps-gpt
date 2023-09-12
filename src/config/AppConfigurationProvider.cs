using System.IO.Abstractions;
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
    private Lazy<PsGptConfiguration> _config { get; set; } 
    private PsGptConfiguration Config => _config.Value;

    public virtual AppConfigSection AppConfig => Config.AppConfig;

    public IFileSystem _fileSystem;
    
    public AppConfigurationProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _config = new Lazy<PsGptConfiguration>(() => GetConfig());
    }

    private PsGptConfiguration GetConfig()
    {
        if (_fileSystem.File.Exists(ConfigFileLocation))
        {
            using (var fileStream = _fileSystem.FileStream.New(ConfigFileLocation, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length < 3)
                {
                    ClearAll();
                }
                else
                {
                    return JsonSerializer.Deserialize<PsGptConfiguration>(fileStream) ?? Config;
                }    
            }
        }
        return new PsGptConfiguration();
    }

    public virtual void SaveAll()
    {
        if (!_fileSystem.Directory.Exists(ConfigFileFolder))
        {
            _fileSystem.Directory.CreateDirectory(ConfigFileFolder);
        }
        using (var saveStream = _fileSystem.FileStream.New(ConfigFileLocation, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize<PsGptConfiguration>(saveStream, Config);
        }
    }

    public virtual void ClearAll()
    {
        _fileSystem.File.Delete(ConfigFileLocation);
        _config = new Lazy<PsGptConfiguration>(() => GetConfig());
    }
    
    private string ConfigFileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt");
    private string ConfigFileLocation => Path.Combine(ConfigFileFolder, "ps-gpt.config");
}