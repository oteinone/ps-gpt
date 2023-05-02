// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using PowershellGpt.AzureAi;
using System.Text;
using Spectre.Console.Rendering;
using PowershellGpt.Config;

string[] EXIT_TERMS = new string[] { "exit", "quit", "q", "done" };
const string MULTILINE_INDICATOR = "`";
const int RESPONSE_PADDING = 2;

#region Initialization

var config = AppConfiguration.GptConfig;
bool configSaved = false;

// Get a system prompt from args.
// If prompt is defined in args, we will tell the response later
string? systemPrompFromArgs = args.FirstOrDefault();

// Get a system prompt from config.
// If prompt is defined in config, we won't tell the initial response. We'll go directly to business
string? systemPromptFromEnv = config.DefaultAppPrompt;

// Get OpenAI endpoint values from env or from the user
if (string.IsNullOrEmpty(AppConfiguration.GptConfig.EndpointUrl))
{
    AppConfiguration.GptConfig.EndpointUrl = GetSettingString("API Endpoint");
}
if (string.IsNullOrEmpty(AppConfiguration.GptConfig.Model))
{
    AppConfiguration.GptConfig.Model = GetSettingString("Gpt model/deployment name");
}
if (string.IsNullOrEmpty(AppConfiguration.GptConfig.ApiKey))
{
    AppConfiguration.GptConfig.ApiKey = GetSettingString("Api key", true);
}
// Initialize client
var azureAiClient = new AzureAiClient(AppConfiguration.GptConfig.EndpointUrl, AppConfiguration.GptConfig.Model, AppConfiguration.GptConfig.ApiKey,
    systemPrompFromArgs ?? systemPromptFromEnv);//, () => AppConfiguration.Save());

// Show system response if custom assistant prompt was used to know whether it has been accepted.
if (!string.IsNullOrEmpty(systemPrompFromArgs)) 
{
    //TODO: If untested api key, do this every time
    WriteHorizontalDivider("System prompt response");
    await StreamChatAnswerToScreen(azureAiClient.GetSystemResponse());
    configSaved = CheckConfigSaved(configSaved);
}
#endregion

#region Main program loop

WriteHorizontalDivider("Ask ChatGPT");

string? userPrompt;
StringBuilder builder;
while(!string.IsNullOrEmpty(userPrompt = AnsiConsole.Ask<string>("[purple]>>[/]")))
{
    if (string.IsNullOrWhiteSpace(userPrompt) || EXIT_TERMS.Contains(userPrompt.ToLower()))
    {
        break;
    }
    else if (userPrompt == MULTILINE_INDICATOR)
    {
        userPrompt = ReadMultiline();
    }
    builder = await StreamChatAnswerToScreen(azureAiClient.Ask(userPrompt));
    configSaved = CheckConfigSaved(configSaved);
}

WriteHorizontalDivider("Chat conversation done");

#endregion

#region Helper functions

static async Task<StringBuilder> StreamChatAnswerToScreen(IAsyncEnumerable<string> messageStream)
{
    var response = new StringBuilder(" ");
    await AnsiConsole.Live(GetRenderable("")).StartAsync(async ctx => {
        await foreach (var message in messageStream)
        {
            response.Append(message);
            ctx.UpdateTarget(GetRenderable(response.ToString()));
            ctx.Refresh();
        }
        ctx.UpdateTarget(GetRenderable(response.ToString(), true));
        ctx.Refresh();
    });
    Console.WriteLine();
    return response;
}

static bool CheckConfigSaved(bool configSaved)
{
    if (configSaved) return true;
    AppConfiguration.Save();
    return true;
}

static IRenderable GetRenderable(string text, bool complete = false)
{
    var element = new Panel(new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]"));
    element.Expand();
    element.Border = BoxBorder.None;
    element.PadLeft(RESPONSE_PADDING);

    var table = new Table();
    table.AddColumn("");
    table.AddRow(element);
    table.Expand();
    table.Border = TableBorder.None;
    return table;
}

static string GetSettingString(string settingName, bool secret = false)
{
    var prompt = new TextPrompt<string>($"Input {settingName}:");
    if (secret) prompt.Secret('*');
    return AnsiConsole.Prompt(prompt);
}

static string ReadMultiline()
{
    string result = "";
    using (var sr = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
    {
        while (!sr.EndOfStream)
        {
            var input = sr.ReadLine();
            if (input == MULTILINE_INDICATOR)
                return result;
            else
                result += input + Environment.NewLine;
        }
        return result;
    }
}

void WriteHorizontalDivider(string message)
{
    AnsiConsole.Write(new Rule($"[green]{message}[/]").LeftJustified());
}

#endregion