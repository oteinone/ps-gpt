using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using PowershellGpt.AzureAi;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;
using PowershellGpt.ConsoleApp;
using PowershellGpt.Templates;

public class Common
{
    public static MockEnv Bootstrap()
    {
        var section = new AppConfigSection()
        {
            EndpointType = DEFAULT_ENDPOINT_TYPE,
            EndpointUrl = DEFAULT_ENDPOINT_URL,
            Model = DEFAULT_MODEL,
            ApiKey = DEFAULT_API_KEY
        };
        return Bootstrap(section);
    }

    public static MockEnv Bootstrap(AppConfigSection testConfiguration)
    {
        var mockFileSystem = new Mock<FileSystem>();
        var mockConfigProvider = new Mock<AppConfigurationProvider>(mockFileSystem.Object);
        var mockIOProvider = new Mock<ConsoleIOProvider>(testConfiguration);
        var mockAIClient = new Mock<AzureAiClient>(testConfiguration);
        var mockTemplateProvider = new Mock<TemplateProvider>(mockFileSystem.Object);

        // Set up DI
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ITemplateProvider>(sp => mockTemplateProvider.Object);
        builder.Services.AddSingleton<IFileSystem>(sp => mockFileSystem.Object);
        builder.Services.AddTransient<IAppConfigurationProvider>(sp => mockConfigProvider.Object);
        builder.Services.AddTransient<AppConfigSection>(sp => sp.GetRequiredService<IAppConfigurationProvider>().AppConfig);
        builder.Services.AddSingleton<IIOProvider>(sp => mockIOProvider.Object);
        builder.Services.AddSingleton<IAiClient>(sp => mockAIClient.Object);

        //PowershellGpt.HostContainer.Host = builder.Build();

        mockAIClient.Setup(client => client.Ask(It.IsAny<string>())).Returns((string s) => new string[] { s }.ToAsyncEnumerable());
        mockIOProvider.Setup(io => io.IsConsoleInputRedirected).Returns(false);
        mockConfigProvider.Setup(configProvider => configProvider.AppConfig).Returns(testConfiguration);
        mockConfigProvider.Setup(configProvider => configProvider.SaveAll()).Verifiable();
        mockTemplateProvider.Setup(provider => provider.GetUserMessage(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync((string text, string? template) => text);
        
        return new MockEnv(
            builder.Build(),
            mockIOProvider,
            mockAIClient,
            mockConfigProvider,
            mockTemplateProvider,
            mockFileSystem
        );
    }

    public class MockEnv
    {
        public MockEnv(IHost host, Mock<ConsoleIOProvider> iOProvider, Mock<AzureAiClient> aiClient, Mock<AppConfigurationProvider> configProvider, Mock<TemplateProvider> templateProvider, Mock<FileSystem> fileSystem)
        {
            Host = host;
            IOProvider = iOProvider;
            AiClient = aiClient;
            ConfigProvider = configProvider;
            TemplateProvider = templateProvider;
            FileSystem = fileSystem;
        }

        public IHost Host { get; init; }
        public Mock<ConsoleIOProvider> IOProvider { get; init; }
        public Mock<AzureAiClient> AiClient { get; init; }
        public Mock<AppConfigurationProvider> ConfigProvider { get; init; }
        public Mock<TemplateProvider> TemplateProvider { get; init; }
        public Mock<FileSystem> FileSystem { get; init; }
    }

    const GptEndpointType DEFAULT_ENDPOINT_TYPE = GptEndpointType.AzureOpenAI;
    const string DEFAULT_ENDPOINT_URL = "https://contoso.com/azure-api/openai";
    const string DEFAULT_MODEL = "test-model";
    const string DEFAULT_API_KEY = "123456ASDFGHE";

}

public class DummyConfigurationProvider : IAppConfigurationProvider
{
    public DummyConfigurationProvider()
    {
        _appConfig = new AppConfigSection();
    }

    public DummyConfigurationProvider(AppConfigSection configuration)
    {
        _appConfig = configuration;
    }

    private AppConfigSection _appConfig;

    public AppConfigSection AppConfig => _appConfig;

    public void ClearAll()
    {
        return;
    }

    public void SaveAll()
    {
        return;
    }
}