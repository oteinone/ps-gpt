using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PowershellGpt.AzureAi;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;
using PowershellGpt.ConsoleApp;
using Spectre.Console;

namespace PowershellGpt;

public static class Services
{
    public static IAppConfigurationProvider AppConfigurationProvider => HostContainer.Services.GetRequiredService<IAppConfigurationProvider>();
    public static IIOProvider IOProvider = HostContainer.Services.GetRequiredService<IIOProvider>();
    public static AppConfigSection Configuration => AppConfigurationProvider.AppConfig;
    public static IAiClient AiClient => HostContainer.Services.GetRequiredService<IAiClient>();
}

public static class HostContainer
{
    #pragma warning disable CS8618
    public static IHost Host { get; set; }
    #pragma warning restore CS8618

    public static IServiceProvider Services => Host.Services;
}

public class ApplicationInitializationException : Exception
{
    public ApplicationInitializationException(string msg) 
        : base(msg) { }

    public ApplicationInitializationException(string msg, Exception inner) 
        : base(msg, inner) { }
}