using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using PowershellGpt.Config;
using Spectre.Console.Cli;

namespace PowershellGpt.ConsoleApp;

public class GptCommand : AsyncCommand<GptCommandOptions>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] GptCommandOptions settings)
    {
        // Get DI host configuration from the context
        var host = (IHost) context.Data!;

        // See if user has requested config commands and return if they are succesfully completed
        var configResult = await HandleConfigCommands(host, settings);
        if (configResult >= 0) return configResult;


        var io = host.GetIOProvider();
        
        // Make sure GptConfigSection is populated
        io.EnsureValidConfiguration();
        host.GetAppConfigurationProvider().SaveAll();

        var appConfig = host.GetConfiguration();

        // Initialize client
        var azureAiClient = host.GetAiClient();

        var systemPrompt = settings.SystemPrompt ?? appConfig.DefaultSystemPrompt;
        if (systemPrompt != null)
        {
            azureAiClient.InitSystemPrompt(systemPrompt);
        }

        var templateProvider = host.GetTemplateProvider()   ;
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

    private static async Task<int> HandleConfigCommands(IHost host, GptCommandOptions settings)
    {
        if (settings.Clear)
        {
            host.GetAppConfigurationProvider().ClearAll();
            Console.WriteLine("Configuration cleared");
            return 0;
        }
        else if (settings.GetProfile)
        {
            PrintGptProfile(host);
            return 0;
        }
        else if (!string.IsNullOrEmpty(settings.SetProfile))
        {
            var io = host.GetIOProvider();
            string? pipedText = io.IsConsoleInputRedirected ? await io.ReadPipedInputAsync() : null;
            SetGptProfile(host, pipedText, settings);
            PrintGptProfile(host);
            return 0;
        }
        return -1;
    }

    private static void PrintGptProfile(IHost host)
    {
        var gptConfig = host.GetConfiguration();
        host.GetIOProvider().PrintProfile(new string[6,2]{
            { ConfigurationConst.EndpointType, gptConfig.EndpointType.ToString() ?? string.Empty },
            { ConfigurationConst.Model, gptConfig.Model ?? "" },
            { ConfigurationConst.EndpointUrl, gptConfig.EndpointType == GptEndpointType.OpenAIApi ? "<OpenAI Api>" : gptConfig.EndpointUrl ?? "" },
            { ConfigurationConst.ApiKey, gptConfig.CensoredApiKey},
            { ConfigurationConst.DefaultPrompt, gptConfig.DefaultAppPrompt ?? "" },
            { ConfigurationConst.DefaultSystemPrompt, gptConfig.DefaultSystemPrompt ?? "" }
        });
    }

    private static void SetGptProfile(IHost host, string? pipedText, GptCommandOptions settings)
    {
        var gptConfig = host.GetConfiguration();
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
        
        var appConfigurationProvider = host.GetAppConfigurationProvider();
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
}