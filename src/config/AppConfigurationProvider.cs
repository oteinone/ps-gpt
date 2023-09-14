using System.IO.Abstractions;
using System.Text.Json;

namespace PowershellGpt.Config.Provider;

public interface IAppConfigurationProvider
{
    public AppConfigSection AppConfig { get; }
    public void Save(AppConfigSection appConfig);
    public void ClearAll();

}

public class AppConfigurationProvider : IAppConfigurationProvider
{
    private Lazy<PsGptConfiguration> _config { get; set; } 

    public virtual AppConfigSection AppConfig => JsonSerializer.Deserialize<AppConfigSection>(JsonSerializer.Serialize(_config.Value.AppConfig))
        ?? throw new Exception("Unexpected exception making a copy of appconfig");

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
                    return JsonSerializer.Deserialize<PsGptConfiguration>(fileStream) ?? new PsGptConfiguration();
                }    
            }
        }
        return new PsGptConfiguration();
    }

    public virtual void Save(AppConfigSection appConfig)
    {
        if (!_fileSystem.Directory.Exists(ConfigFileFolder))
        {
            _fileSystem.Directory.CreateDirectory(ConfigFileFolder);
        }
        
        _config.Value.AppConfig = appConfig;

        using (var saveStream = _fileSystem.FileStream.New(ConfigFileLocation, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize<PsGptConfiguration>(saveStream, _config.Value);
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