using System.Text;
using PowershellGpt.Config;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PowershellGpt.Application;

public static class Application
{
    // Method handles command arguments, updates application configuration
    // returns information required to run the program line application main loop.
    public static AppControlFlow AppStart(string[] args)
    {
        var config = AppConfiguration.GptConfig;
        string? commandArg = args.FirstOrDefault();

        // handle command line arguments
        if (commandArg?.ToLower().Trim() == "--clear")
        {
            AppConfiguration.Clear();
            AppConfiguration.Save();
            Console.WriteLine("Configuration cleared");
            return new AppControlFlow() { EndApplication = true };
        }
        else if (commandArg?.ToLower().Trim() == "--prompt")
        {
            var newPrompt = args.Length > 1 ? args[1] : string.Empty;
            config.DefaultAppPrompt = newPrompt;
            AppConfiguration.Save();
            Console.WriteLine(string.IsNullOrEmpty(newPrompt) ? "Default prompt cleared" : $"Default prompt set to \"{newPrompt}\"");
            return new AppControlFlow() { EndApplication = true };
        }

        // Get a system prompt from config.
        // If prompt is defined in config, we won't tell the initial response. We'll go directly to business
        string? systemPromptFromEnv = config.DefaultAppPrompt;

        // Get OpenAI endpoint values from env or from the user
        if (AppConfiguration.GptConfig.EndpointType == null)
        {
            AppConfiguration.GptConfig.EndpointType = SelectFromEnum<GptEndpointType>("Which type of endpoint are you using");
        }
        if (AppConfiguration.GptConfig.EndpointType == GptEndpointType.AzureOpenAI &&
            string.IsNullOrEmpty(AppConfiguration.GptConfig.EndpointUrl))
        {
            AppConfiguration.GptConfig.EndpointUrl = GetSettingString("API Endpoint");
        }
        if (string.IsNullOrEmpty(AppConfiguration.GptConfig.Model))
        {
            AppConfiguration.GptConfig.Model = GetSettingString(
                AppConfiguration.GptConfig.EndpointType == GptEndpointType.AzureOpenAI ? "Deployment name" : "Model name");
        }
        if (string.IsNullOrEmpty(AppConfiguration.GptConfig.ApiKey))
        {
            AppConfiguration.GptConfig.ApiKey = GetSettingString("API Key", true);
        }

        return new AppControlFlow() 
        {
            EndApplication = false,
            ShowPromptResponse = !string.IsNullOrEmpty(commandArg),
            CustomPromptValue = commandArg ?? systemPromptFromEnv
        };
    }

    public static async Task<StringBuilder> StreamChatAnswerToScreen(IAsyncEnumerable<string> messageStream)
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

    public static bool CheckConfigSaved(bool configSaved)
    {
        if (configSaved) return true;
        AppConfiguration.Save();
        return true;
    }

    public static IRenderable GetRenderable(string text, bool complete = false)
    {
        var element = new Panel(new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]"));
        element.Expand();
        element.Border = BoxBorder.None;
        element.PadLeft(AppConfiguration.GptConfig.ResponsePadding);

        var table = new Table();
        table.AddColumn("");
        table.AddRow(element);
        table.Expand();
        table.Border = TableBorder.None;
        return table;
    }

    public static void WriteHorizontalDivider(string message)
    {
        AnsiConsole.Write(new Rule($"[green]{message}[/]").LeftJustified());
    }

    private static string GetSettingString(string settingName, bool secret = false)
    {
        var prompt = new TextPrompt<string>($"Input {settingName}:");
        if (secret) prompt.Secret('*');
        return AnsiConsole.Prompt(prompt);
    }

    private static T SelectFromEnum<T>(string title)
        where T : struct, System.Enum
    {
        var prompt = new SelectionPrompt<T>()
        {
            Title = title
        };
        prompt.AddChoices(Enum.GetValues<T>());
        return AnsiConsole.Prompt(prompt);
    }

    internal static string? Ask()
    {
        return AnsiConsole.Ask<string>("[purple]>>[/]");
    }

    public static string ReadMultiline()
    {
        string result = "";
        using (var sr = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
        {
            while (!sr.EndOfStream)
            {
                var input = sr.ReadLine();
                if (input == AppConfiguration.GptConfig.MultilineIndicator)
                    return result;
                else
                    result += input + Environment.NewLine;
            }
            return result;
        }
    }
}

public class AppControlFlow
{
    public bool EndApplication { get; set; }
    public bool ShowPromptResponse { get; set; }
    public string? CustomPromptValue { get; set; }
}