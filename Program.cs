// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using PowershellGpt.AzureAi;
using System.Text;
using Spectre.Console.Rendering;

string[] exitTerms = new string[] { "exit", "quit", "q", "done" };
const string MULTILINE_INDICATOR = "`";

// Get a system prompt from args.
// If prompt is defined in args, we will tell the response later
string? systemPrompFromArgs = args.FirstOrDefault();

// Get a system prompt from env.
// If prompt is defined in env, we won't tell the initial response. We'll go directly to business
string? systemPromptFromEnv = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT_PROMPT");

bool showAssistantResponse = systemPrompFromArgs == null;

// Get OpenAI endpoint values from env or from the user
string openapiendpoint = GetOpenApiSetting("AZURE_OPENAI_ENDPOINT_URL");
string openaimodel = GetOpenApiSetting("AZURE_OPENAI_MODEL_NAME");
string openaikey = GetOpenApiSetting("AZURE_OPENAI_API_KEY", true);

// Initialize client
var gpt = new AzureAiClient(openapiendpoint, openaimodel, openaikey, systemPrompFromArgs ?? systemPromptFromEnv);
Console.WriteLine("Ask ChatGPT:");

string? userPrompt;
StringBuilder builder;
// What does the user ask?
while(!string.IsNullOrEmpty(userPrompt = AnsiConsole.Ask<string>("[purple]>>[/]")))
{
    if (string.IsNullOrWhiteSpace(userPrompt) || exitTerms.Contains(userPrompt.ToLower()))
    {
        break;
    }
    else if (userPrompt == MULTILINE_INDICATOR)
    {
        userPrompt = ReadMultiline();
    }
    builder = new StringBuilder(" ");
    await AnsiConsole.Live(GetRenderable("")).StartAsync(async ctx => {
        await foreach (var message in gpt.Ask(userPrompt))
        {
            builder.Append(message);
            ctx.UpdateTarget(GetRenderable(builder.ToString()));
            ctx.Refresh();
        }
        ctx.UpdateTarget(GetRenderable(builder.ToString(), true));
        ctx.Refresh();
    });
}

AnsiConsole.Write(new Rule("[green]Chat conversation done[/]").LeftJustified()); 

IRenderable GetRenderable(string text, bool complete = false)
{
    var element = new Panel(new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]"));
    element.Expand();
    element.Border = BoxBorder.None;
    var table = new Table();
    table.AddColumn("");
    table.AddRow(element);
    table.Expand();
    table.Border = TableBorder.None;
    return table;
}

string GetOpenApiSetting(string envVariableName, bool secret = false)
{
    string? value = Environment.GetEnvironmentVariable(envVariableName);
    if (string.IsNullOrEmpty(value)) {
        var prompt = new TextPrompt<string>($"$env:{envVariableName} not found, enter manually:");
        if (secret) prompt.Secret('*');
        value = AnsiConsole.Prompt(prompt);
        //if (!secret) Environment.SetEnvironmentVariable(envVariableName, value, EnvironmentVariableTarget.User);
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