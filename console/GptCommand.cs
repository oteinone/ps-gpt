using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PowershellGpt.AzureAi;
using PowershellGpt.Config;
using Spectre.Console.Cli;

namespace PowershellGpt.ConsoleApp;

public class GptCommand : AsyncCommand<GptCommand.Options>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Options settings)
    {
        bool configSaved = false;

        // See if user has requested config commands
        var configResult = HandleConfigCommands(settings);
        if (configResult >= 0) return configResult;
        
        // Make sure GptConfigSection is populated
        Application.EnsureValidConfiguration();

        // Initialize client
        var azureAiClient = new AzureAiClient(AppConfiguration.GptConfig.EndpointType, AppConfiguration.GptConfig.EndpointUrl, AppConfiguration.GptConfig.Model,
            AppConfiguration.GptConfig.ApiKey!, settings.Prompt ?? AppConfiguration.GptConfig.DefaultAppPrompt);

        //Handle requests from stdin
        if (Console.IsInputRedirected)
        {
            await Application.StreamChatAnswerToStdOutAsync(azureAiClient.Ask(await Application.ReadPipedInputAsync()));
            return 0;
        }

        // Handle chat (default mode)

        // Show system response if custom assistant prompt was used to know whether it has been accepted.
        if (!string.IsNullOrEmpty(settings.Prompt))
        {
            Application.WriteHorizontalDivider("System prompt response");
            await Application.StreamChatAnswerToScreenAsync(azureAiClient.GetSystemResponse());
            configSaved = Application.CheckConfigSaved(configSaved);
        }

        Application.WriteHorizontalDivider("Ask ChatGPT");
        
        // Main program loop
        string? userPrompt = null;
        StringBuilder builder;
        while(true)
        {
            // if this is the first loop and pipedinput is not null, we use it. Otherwise we ask.
            userPrompt = Application.AskUser();
            if (string.IsNullOrWhiteSpace(userPrompt) || AppConfiguration.GptConfig.ExitTerms.Contains(userPrompt.ToLower()))
            {
                break;
            }
            else if (userPrompt == AppConfiguration.GptConfig.MultilineIndicator)
            {
                userPrompt = Application.ReadMultiline();
            }
            builder = await Application.StreamChatAnswerToScreenAsync(azureAiClient.Ask(userPrompt));
            configSaved = Application.CheckConfigSaved(configSaved);
        }

        Application.WriteHorizontalDivider("Chat conversation done");

        return 0;
    }
    
    private static int HandleConfigCommands(Options settings)
    {
        if (settings.Clear)
        {
            AppConfiguration.ClearAll();
            AppConfiguration.SaveAll();
            Console.WriteLine("Configuration cleared");
            return 0;
        }
        else if (settings.Profile)
        {
            var gptConfig = AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>();

            Application.PrintProfile(new string[5,2]{
                { Const.EndpointType, gptConfig.EndpointType.ToString() ?? string.Empty },
                { Const.Model, gptConfig.Model },
                { Const.EndpointUrl, gptConfig.EndpointType == GptEndpointType.OpenAIApi ? "<OpenAI Api>" : gptConfig.EndpointUrl },
                { Const.ApiKey, gptConfig.CensoredApiKey},
                { Const.DefaultPrompt, gptConfig.DefaultAppPrompt }
            });
            return 0;
        }
        else if (!string.IsNullOrEmpty(settings.PersistenPrompt))
        {
            AppConfiguration.GptConfig.DefaultAppPrompt = settings.PersistenPrompt;
            AppConfiguration.SaveAll();
            Console.WriteLine(string.IsNullOrEmpty(settings.PersistenPrompt) ? "Default prompt cleared" : $"Default prompt set to \"{settings.PersistenPrompt}\"");
            return 0;
        }
        return -1;
    }

    public class Options : CommandSettings
    {

        [CommandArgument(0, "[prompt]")]
        public string? Prompt { get; init; }

        [CommandOption("--clear")]
        [Description("Clears the setup and resets to default values")]
        [DefaultValue(false)]
        public bool Clear { get; init; }
        
        [CommandOption("--prompt")]
        [Description("Set a persistent prompt")]
        public string? PersistenPrompt { get; init; }
    
        [CommandOption("--profile|-p")]
        [DefaultValue(false)]
        public bool Profile { get; init; }

    }
}