// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using PowershellGpt.AzureAi;
using System.Text;
using Spectre.Console.Rendering;

string[] EXIT_TERMS = new string[] { "exit", "quit", "q", "done" };
const string MULTILINE_INDICATOR = "`";
const int RESPONSE_PADDING = 2;

#region Initialization

// Get a system prompt from args.
// If prompt is defined in args, we will tell the response later
string? systemPrompFromArgs = args.FirstOrDefault();

// Get a system prompt from env.
// If prompt is defined in env, we won't tell the initial response. We'll go directly to business
string? systemPromptFromEnv = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_PROMPT");

// Get OpenAI endpoint values from env or from the user
string openapiendpoint = GetOpenApiSetting("AZURE_OPENAI_ENDPOINT_URL");
string openaimodel = GetOpenApiSetting("AZURE_OPENAI_MODEL_NAME");
string openaikey = GetOpenApiSetting("AZURE_OPENAI_API_KEY", true);

// Initialize client
var azureAiClient = new AzureAiClient(openapiendpoint, openaimodel, openaikey, systemPrompFromArgs ?? systemPromptFromEnv);

// Show system response if custom assistant prompt was used to know whether it has been accepted.
if (systemPrompFromArgs != null) 
{
    WriteHorizontalDivider("System prompt response");
    await StreamChatAnswerToScreen(azureAiClient.GetSystemResponse());
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

static string GetOpenApiSetting(string envVariableName, bool secret = false)
{
    string? value = Environment.GetEnvironmentVariable(envVariableName);
    if (string.IsNullOrEmpty(value)) {
        var prompt = new TextPrompt<string>($"$env:{envVariableName} not found, enter manually:");
        if (secret) prompt.Secret('*');
        value = AnsiConsole.Prompt(prompt);
    } 
    return value;
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