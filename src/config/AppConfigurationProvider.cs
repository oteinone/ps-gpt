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
    private PsGptConfiguration _config;
    public AppConfigSection AppConfig => _config.AppConfig;

    public IFileSystem _fileSystem;
    
    public AppConfigurationProvider(IFileSystem fileSystem)
    {
        _config = GetConfig();
        _fileSystem = fileSystem;
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
                    return JsonSerializer.Deserialize<PsGptConfiguration>(fileStream) ?? _config;
                }    
            }
        }
        return new PsGptConfiguration();
    }

    public void SaveAll()
    {
        if (!_fileSystem.Directory.Exists(ConfigFileFolder))
        {
            _fileSystem.Directory.CreateDirectory(ConfigFileFolder);
        }
        using (var saveStream = _fileSystem.FileStream.New(ConfigFileLocation, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize<PsGptConfiguration>(saveStream, _config);
        }
    }

    public void ClearAll()
    {
        _fileSystem.File.Delete(ConfigFileLocation);
        _config = new PsGptConfiguration();
    }
    
    private string ConfigFileFolder => _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt");
    private string ConfigFileLocation => _fileSystem.Path.Combine(ConfigFileFolder, "ps-gpt.config");
}