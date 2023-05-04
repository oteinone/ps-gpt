// See https://aka.ms/new-console-template for more information
using PowershellGpt.AzureAi;
using System.Text;
using PowershellGpt.Config;
using PowershellGpt.Application;

bool configSaved = false;

// Initialize application and configuration

var appControl = Application.AppStart(args);
if (appControl.EndApplication) return;

var gptConfig = AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>();

// Initialize client
var azureAiClient = new AzureAiClient(gptConfig.EndpointType, gptConfig.EndpointUrl, gptConfig.Model,
    gptConfig.ApiKey!, appControl.CustomPromptValue);

// Show system response if custom assistant prompt was used to know whether it has been accepted.
if (appControl.ShowPromptResponse) 
{
    //TODO: If untested api key, do this every time
    Application.WriteHorizontalDivider("System prompt response");
    await Application.StreamChatAnswerToScreen(azureAiClient.GetSystemResponse());
    configSaved = Application.CheckConfigSaved(configSaved);
}

// Main program loop
Application.WriteHorizontalDivider("Ask ChatGPT");

string? userPrompt;
StringBuilder builder;
while(!string.IsNullOrEmpty(userPrompt = Application.Ask()))
{
    if (string.IsNullOrWhiteSpace(userPrompt) || gptConfig.ExitTerms.Contains(userPrompt.ToLower()))
    {
        break;
    }
    else if (userPrompt == gptConfig.MultilineIndicator)
    {
        userPrompt = Application.ReadMultiline();
    }
    builder = await Application.StreamChatAnswerToScreen(azureAiClient.Ask(userPrompt));
    configSaved = Application.CheckConfigSaved(configSaved);
}

Application.WriteHorizontalDivider("Chat conversation done");