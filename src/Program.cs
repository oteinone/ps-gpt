// See https://aka.ms/new-console-template for more information
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PowershellGpt.AzureAi;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;
using PowershellGpt.ConsoleApp;
using PowershellGpt.Templates;
using Spectre.Console.Cli;

// Set up DI
HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Services.AddSingleton<ITemplateProvider, TemplateProvider>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<IAppConfigurationProvider, AppConfigurationProvider>();
builder.Services.AddTransient<AppConfigSection>(sp => sp.GetRequiredService<IAppConfigurationProvider>().AppConfig);
builder.Services.AddSingleton<IIOProvider, ConsoleIOProvider>();
builder.Services.AddTransient<IAiClient, AzureAiClient>();
var host = builder.Build();

// Helper function
void DebugStackTrace(Exception e, int stackCount = 10)
{
    if (stackCount < 0) return;
    Console.Error.WriteLine($"Message: {e.Message}{Environment.NewLine}{e.StackTrace}");
    if (e.InnerException != null) DebugStackTrace(e.InnerException, stackCount - 1);
}

// Initialize application and configuration
var app = new CommandApp<GptCommand>();
app.WithData(host);
app.Configure(config => {
    config.SetApplicationName("ps-gpt");
    config.AddExample(new string[0]);
    config.AddExample(new [] {"\"Give me a list of 10 fruit\""});
    config.AddExample(new [] { "--set-profile", "model=gpt-3.5-turbo"});

    config.ValidateExamples();
    #if DEBUG
    {
        config.SetExceptionHandler(e => DebugStackTrace(e));
    }
    #endif
});

return app.Run(args);

