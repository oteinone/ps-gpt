using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;
using PowershellGpt.ConsoleApp;

public class Common
{

    public static Mock<ConsoleIOProvider> BootStrap()
    {
        var section = new AppConfigSection()
        {
            EndpointType = DEFAULT_ENDPOINT_TYPE,
            EndpointUrl = DEFAULT_ENDPOINT_URL,
            Model = DEFAULT_MODEL,
            ApiKey = DEFAULT_API_KEY
        };
        return BootStrap(section);
    }

    public static Mock<ConsoleIOProvider> BootStrap(AppConfigSection testConfiguration)
    {
        var mockIOProvider = new Mock<ConsoleIOProvider>();
        // Set up DI
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddTransient<IAppConfigurationProvider>(sp => new DummyConfigurationProvider(testConfiguration));
        builder.Services.AddTransient<IIOProvider>(sp => mockIOProvider.Object );
        PowershellGpt.HostContainer.Host = builder.Build();

        return mockIOProvider;
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