using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PowershellGpt.AzureAi;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;
using PowershellGpt.ConsoleApp;
using PowershellGpt.Templates;

namespace PowershellGpt;

public static class HostExtensions
{
    public static IAppConfigurationProvider GetAppConfigurationProvider(this IHost host)
        => host.Services.GetRequiredService<IAppConfigurationProvider>();
    public static IIOProvider GetIOProvider(this IHost host)
        => host.Services.GetRequiredService<IIOProvider>();

    public static AppConfigSection GetConfiguration(this IHost host)
        => host.Services.GetRequiredService<AppConfigSection>();
    public static IAiClient GetAiClient(this IHost host)
        => host.Services.GetRequiredService<IAiClient>();
    public static ITemplateProvider GetTemplateProvider(this IHost host) 
        => host.Services.GetRequiredService<ITemplateProvider>();
}