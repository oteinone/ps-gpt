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
        // See if user has requested config commands
        var configResult = await HandleConfigCommands(settings);
        if (configResult >= 0) return configResult;
        
        // Make sure GptConfigSection is populated
        Application.EnsureValidConfiguration();

        // Initialize client
        var azureAiClient = new AzureAiClient(AppConfiguration.GptConfig.EndpointType, AppConfiguration.GptConfig.EndpointUrl, AppConfiguration.GptConfig.Model,
            AppConfiguration.GptConfig.ApiKey!, settings.SystemPrompt ?? AppConfiguration.GptConfig.DefaultSystemPrompt);

        string? text = null;
        string? template = string.IsNullOrWhiteSpace(AppConfiguration.GptConfig.DefaultAppPrompt) ? null : AppConfiguration.GptConfig.DefaultAppPrompt;
        
        if (Console.IsInputRedirected) //Handle requests from stdin
        {
            text = await Application.ReadPipedInputAsync();
            if (!settings.Chat || !ClaimConsole.Claim())
            {
                await Application.StreamChatAnswerToStdOutAsync(azureAiClient.Ask(GetUserMessage(text, template)));
                return 0;
            }
        }
        else if (!string.IsNullOrWhiteSpace(settings.Text)) //Handle requests from params
        {
            if (!settings.Chat)
            {
                await Application.StreamChatAnswerToStdOutAsync(azureAiClient.Ask(GetUserMessage(settings.Text, template)));
                return 0;
            }
            else
            {
                text = settings.Text;
            }
        }

        // Handle chat (default mode)

        // Show system response if custom assistant prompt was used to know whether it has been accepted.
        if (!string.IsNullOrEmpty(settings.SystemPrompt))
        {
            Application.WriteHorizontalDivider("System prompt response");
            await Application.StreamChatAnswerToScreenAsync(azureAiClient.GetSystemResponse());
        }

        Application.WriteHorizontalDivider("Ask ChatGPT");
        
        // Main program loop
        string? userPrompt = null;
        StringBuilder builder;
        while(true)
        {
            // If this is the first loop AND settings.Text has value, use that one.
            userPrompt = (userPrompt != null || string.IsNullOrWhiteSpace(text)) ? Application.AskUser() : GetUserMessage(text, template);
            if (string.IsNullOrWhiteSpace(userPrompt) || AppConfiguration.GptConfig.ExitTerms.Contains(userPrompt.ToLower()))
            {
                break;
            }
            else if (userPrompt == AppConfiguration.GptConfig.MultilineIndicator)
            {
                userPrompt = Application.ReadMultiline();
            }
            builder = await Application.StreamChatAnswerToScreenAsync(azureAiClient.Ask(userPrompt));
        }

        Application.WriteHorizontalDivider("Chat conversation done");

        return 0;
    }
    
    private static async Task<int> HandleConfigCommands(Options settings)
    {
       string? pipedText = null;

        if (Console.IsInputRedirected)
        {
            pipedText = await Application.ReadPipedInputAsync();
        }

        if (settings.Clear)
        {
            AppConfiguration.ClearAll();
            AppConfiguration.SaveAll();
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
            SetGptProfile(pipedText, settings);
            PrintGptProfile();
            return 0;
        }
        return -1;
    }
    
    private string GetUserMessage(string text, string? template)
    {
        return template == null ? text
            : template.Contains("{text}") ? template.Replace("{text}", text)
            : $"{template}{Environment.NewLine}###{Environment.NewLine}{text}";
    }

    private static void PrintGptProfile()
    {
        var gptConfig = AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>();
        Application.PrintProfile(new string[6,2]{
            { Const.EndpointType, gptConfig.EndpointType.ToString() ?? string.Empty },
            { Const.Model, gptConfig.Model },
            { Const.EndpointUrl, gptConfig.EndpointType == GptEndpointType.OpenAIApi ? "<OpenAI Api>" : gptConfig.EndpointUrl },
            { Const.ApiKey, gptConfig.CensoredApiKey},
            { Const.DefaultPrompt, gptConfig.DefaultAppPrompt },
            { Const.DefaultSystemPrompt, gptConfig.DefaultSystemPrompt }
        });
    }

    private static void SetGptProfile(string? pipedText, Options settings)
    {
        var gptConfig = AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>();
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
        
        switch(setting.ToLower())
        {
            case Const.Model:
                gptConfig.Model = value;
                AppConfiguration.SaveAll();
                break;
            case Const.EndpointUrl:
                gptConfig.EndpointUrl = value;
                AppConfiguration.SaveAll();
                break;
            case Const.ApiKey:
                gptConfig.ApiKey = value;
                AppConfiguration.SaveAll();
                break;
            case Const.DefaultPrompt:
                gptConfig.DefaultAppPrompt = value;
                AppConfiguration.SaveAll();
                break;
            case Const.DefaultSystemPrompt:
                gptConfig.DefaultSystemPrompt = value;
                AppConfiguration.SaveAll();
                break;
            default:
                throw new Exception($"Did not recognize profile setting {setting}");
        }
    }

    public class Options : CommandSettings
    {
        [CommandArgument(0, "[text]")]
        [Description("The text content of the conversation to send to the specified model. Potentially preceded/surrounded by a prompt template.")]
        public string? Text { get; init; }

        [CommandOption("--clear")]
        [Description("Clears the current profile and resets the config to default values")]
        [DefaultValue(false)]
        public bool Clear { get; init; }

        [CommandOption("--get-profile")]
        [Description("Shows the current profile settings saved in application configuration")]
        [DefaultValue(false)]
        public bool GetProfile { get; init; }

        [CommandOption("--set-profile")]
        [Description("Sets a value in current profile. E.g. --set-profile model=gpt-3")]
        public string? SetProfile { get; init; }

        [CommandOption("--chat|-c")]
        [Description("Forces chat mode (continuous conversation) in cases where the input would otherwise be written to console/stdout")]
        public bool Chat { get; set; }

        [CommandOption("--system-prompt")]
        [Description("Commands the api with a 'system' type message before initializing the actual conversation")]
        public string? SystemPrompt { get; set; }

    }
}