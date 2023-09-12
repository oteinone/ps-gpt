using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PowershellGpt.AzureAi;
using PowershellGpt.Config;
using PowershellGpt.Templates;
using Spectre.Console.Cli;

namespace PowershellGpt.ConsoleApp;

public class GptCommand : AsyncCommand<GptCommand.Options>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Options settings)
    {
        // See if user has requested config commands and return if they are succesfully completed
        var configResult = await HandleConfigCommands(settings);
        if (configResult >= 0) return configResult;

        var io = Services.IOProvider;
        
        // Make sure GptConfigSection is populated
        io.EnsureValidConfiguration();
        Services.AppConfigurationProvider.SaveAll();

        var appConfig = Services.Configuration;

        // Initialize client
        var azureAiClient = Services.AiClient;

        var systemPrompt = settings.SystemPrompt ?? appConfig.DefaultSystemPrompt;
        if (systemPrompt != null)
        {
            azureAiClient.InitSystemPrompt(systemPrompt);
        }

        var templateProvider = Services.TemplateProvider;
        string? text = null;

        if (io.IsConsoleInputRedirected) //Handle text from stdin
        {
            text = await io.ReadPipedInputAsync();
            if (!settings.Chat || !ConsoleHelper.ReclaimConsole()) // if chat mode is not attempted or console cannot be claimed, pipe directly to stdout and terminate
            {
                await io.StreamChatAnswerToStdOutAsync(azureAiClient.Ask(await templateProvider.GetUserMessage(text, settings.Template)));
                return 0;
            }
        }
        else if (!string.IsNullOrWhiteSpace(settings.Text)) //Handle text from args
        {
            text = settings.Text;
            if (!settings.Chat) // If chat mode is not attempted, pipe to stdout and terminate
            {
                await io.StreamChatAnswerToStdOutAsync(azureAiClient.Ask(await templateProvider.GetUserMessage(text, settings.Template)));
                return 0;
            }
        }

        // Handle chat (default mode)

        // Show system response if custom assistant prompt was used to know whether it has been accepted.
        if (!string.IsNullOrEmpty(settings.SystemPrompt))
        {
            io.WriteHorizontalDivider("System prompt response");
            await io.StreamChatAnswerToScreenAsync(azureAiClient.GetSystemResponse());
        }

        io.WriteHorizontalDivider("Ask ChatGPT");
        
        // Main program loop
        // First loop, use template and input text when appropriate
        string userPrompt = await templateProvider.GetUserMessage(
            text ?? io.AskUser(),
            settings.Template
        );


        while(true)
        {
            if (string.IsNullOrWhiteSpace(userPrompt) || appConfig.ExitTerms.Contains(userPrompt.ToLower())
                || appConfig.ExitTerms.Contains(text?.ToLower()))
            {
                break;
            }
            
            if (text == appConfig.MultilineIndicator)
            {
                userPrompt = await templateProvider.GetUserMessage(io.ReadMultiline(), settings.Template);
            }
            await io.StreamChatAnswerToScreenAsync(azureAiClient.Ask(userPrompt));
            // get input for next loop
            userPrompt = text = io.AskUser();
        }

        io.WriteHorizontalDivider("Chat conversation done");

        return 0;
    }

    private static async Task<int> HandleConfigCommands(Options settings)
    {
        if (settings.Clear)
        {
            Services.AppConfigurationProvider.ClearAll();
            Console.WriteLine("Configuration cleared");
            return 0;
        }
        else if (settings.GetProfile)
        {
            PrintGptProfile();
            return 0;
        }
        else if (!string.IsNullOrEmpty(settings.SetProfile))
        {
            var io = Services.IOProvider;
            string? pipedText = io.IsConsoleInputRedirected ? await io.ReadPipedInputAsync() : null;
            SetGptProfile(pipedText, settings);
            PrintGptProfile();
            return 0;
        }
        return -1;
    }

    private static void PrintGptProfile()
    {
        var gptConfig = Services.Configuration;
        Services.IOProvider.PrintProfile(new string[6,2]{
            { ConfigurationConst.EndpointType, gptConfig.EndpointType.ToString() ?? string.Empty },
            { ConfigurationConst.Model, gptConfig.Model ?? "" },
            { ConfigurationConst.EndpointUrl, gptConfig.EndpointType == GptEndpointType.OpenAIApi ? "<OpenAI Api>" : gptConfig.EndpointUrl ?? "" },
            { ConfigurationConst.ApiKey, gptConfig.CensoredApiKey},
            { ConfigurationConst.DefaultPrompt, gptConfig.DefaultAppPrompt ?? "" },
            { ConfigurationConst.DefaultSystemPrompt, gptConfig.DefaultSystemPrompt ?? "" }
        });
    }

    private static void SetGptProfile(string? pipedText, Options settings)
    {
        var gptConfig = Services.Configuration;
        string setting;
        string? value;

        if (string.IsNullOrEmpty(settings.SetProfile))
        {
            throw new Exception("");
        }

        if (pipedText != null)
        {
            setting = settings.SetProfile.Trim('=');
            value = pipedText;
        }
        else 
        {
            if (settings.SetProfile.Contains('='))
            {
                var cmd = settings.SetProfile.Trim().Split('=');
                setting = cmd[0];
                value =  cmd[1];
            }
            else
            {
                setting = settings.SetProfile;
                value = string.Empty;
            }
        }
        
        var appConfigurationProvider = Services.AppConfigurationProvider;
        switch(setting.ToLower())
        {
            case ConfigurationConst.Model:
                gptConfig.Model = value;
                appConfigurationProvider.SaveAll();
                break;
            case ConfigurationConst.EndpointUrl:
                gptConfig.EndpointUrl = value;
                appConfigurationProvider.SaveAll();
                break;
            case ConfigurationConst.ApiKey:
                gptConfig.ApiKey = value;
                appConfigurationProvider.SaveAll();
                break;
            case ConfigurationConst.DefaultPrompt:
                gptConfig.DefaultAppPrompt = value;
                appConfigurationProvider.SaveAll();
                break;
            case ConfigurationConst.DefaultSystemPrompt:
                gptConfig.DefaultSystemPrompt = value;
                appConfigurationProvider.SaveAll();
                break;
            default:
                throw new Exception($"Did not recognize profile setting {setting}");
        }
    }

    public class Options : CommandSettings
    {
        [CommandArgument(0, "[text]")]
        [Description("The text prompt sent to the specified model. Potentially preceded/surrounded by a prompt template if one has been defined.")]
        public string? Text { get; init; }

        [CommandOption("--clear")]
        [Description("Clears the current profile and resets the config to default values")]
        [DefaultValue(false)]
        public bool Clear { get; init; }

        [CommandOption("--get-profile|-g")]
        [Description("Shows the current profile settings saved in application configuration")]
        [DefaultValue(false)]
        public bool GetProfile { get; init; }

        [CommandOption("--set-profile|-s")]
        [Description("Sets a value in current profile. E.g. --set-profile model=gpt-3")]
        public string? SetProfile { get; init; }

        [CommandOption("--chat|-c")]
        [Description("Forces chat mode (continuous conversation) in cases where the input would otherwise be written to console/stdout")]
        public bool Chat { get; set; }

        [CommandOption("--system-prompt")]
        [Description("Commands the api with a 'system' type message before initializing the actual conversation")]
        public string? SystemPrompt { get; set; }

        [CommandOption("--template|-t")]
        [Description("A file path pointing to a template file. Template file is used as a wrapper for text content. '{text}' in templates is replaced with the text prompt")]
        public string? Template { get; set; }
    }
}