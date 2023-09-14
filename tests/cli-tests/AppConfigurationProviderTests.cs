using System.IO.Abstractions;
using Moq;
using PowershellGpt.Config.Provider;
using System.Text;

public class AppConfigurationProviderTests
{

    
    [Fact]
    public void Initialize_With_No_File()
    {
        var mockFileSystem = new Mock<FileSystem>();
        
        mockFileSystem.Setup(fileSystem => fileSystem.File.Exists(ConfigFileLocation)).Returns(false);
        var configurationProvider = new AppConfigurationProvider(mockFileSystem.Object);
        Assert.NotNull(configurationProvider.AppConfig);
    }

    [Fact]
    public void Initialize_With_Existing_File()
    {
        var mockFileSystem = new Mock<FileSystem>();
        
        mockFileSystem.Setup(fileSystem => fileSystem.File.Exists(ConfigFileLocation)).Returns(true);

        var memorystream = new MemoryStream(Encoding.UTF8.GetBytes(exampleFile));
        var mockStream = new MockFileSystemStream(memorystream, ".");
        
        mockFileSystem.Setup(fileSystem => fileSystem.FileStream.New(ConfigFileLocation, FileMode.Open, FileAccess.Read)).Returns(mockStream);
        var configurationProvider = new AppConfigurationProvider(mockFileSystem.Object);
        Assert.Equal("https://examplendpoint.openai.azure.com/", configurationProvider.AppConfig.EndpointUrl);
    }

    [Fact]
    public void File_Is_Saved()
    {
        var mockFileSystem = new Mock<FileSystem>();
        
        mockFileSystem.Setup(fileSystem => fileSystem.File.Exists(ConfigFileLocation)).Returns(false);

        var stream = new MemoryStream();
        var mockStream = new MockFileSystemStream(stream, ".");
        
        mockFileSystem.Setup(fileSystem => fileSystem.FileStream.New(ConfigFileLocation, FileMode.Open, FileAccess.Read)).Returns(mockStream);
        
        var configurationProvider = new AppConfigurationProvider(mockFileSystem.Object);
        mockFileSystem.Setup(fileSystem => fileSystem.Directory.Exists(ConfigFileFolder)).Returns(true);
        mockFileSystem.Setup(fileSystem => fileSystem.FileStream.New(ConfigFileLocation, FileMode.Create, FileAccess.Write)).Returns(mockStream);
        configurationProvider.Save(new PowershellGpt.Config.AppConfigSection(){ EndpointUrl = "https://contoso-1234.org" });

        Assert.Contains("https://contoso-1234.org", new String(Encoding.UTF8.GetChars(stream.ToArray())));
    }

    
    private string ConfigFileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt");
    private string ConfigFileLocation => Path.Combine(ConfigFileFolder, "ps-gpt.config");

    private const string exampleFile = "{\"appConfig\":{\"EndpointType\":1,\"endpointUrl\":\"https://examplendpoint.openai.azure.com/\",\"model\":\"gpt4-environment\",\"defaultAppPrompt\":null,\"defaultSystemPrompt\":null,\"apikey\":\"\",\"modelConfig\":{\"temperature\":0.7,\"maxTokenCount\":2000,\"nucleusSamplingFactor\":0.95,\"frequencyPenalty\":0,\"presencePenalty\":0}}}";
}

class MockFileSystemStream : FileSystemStream
{
    public MockFileSystemStream(Stream stream, string path) : base(stream, path, false)
    {
    }
}