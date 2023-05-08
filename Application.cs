using System.Text;
using PowershellGpt.Config;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PowershellGpt;

public static class Application
{

    // Makes sure that the current profile has all necessary parameters available to operate
    public static void EnsureValidConfiguration()
    {
        var config = GptConfiguration;

         if (config.EndpointType == null || (config.EndpointType == GptEndpointType.AzureOpenAI && string.IsNullOrEmpty(config.EndpointUrl))
            || string.IsNullOrEmpty(config.Model) || string.IsNullOrEmpty(config.ApiKey))
        {
            if (Console.IsInputRedirected) throw new ApplicationInitializationException(
                "Application configuration was incomplete, cannot accept input from pipeline. Run the application once without piped input to set application configuration");

            if (config.EndpointType == null)
            {
                config.EndpointType = SelectFromEnum<GptEndpointType>("Which type of endpoint are you using");
            }
            // Openai endpoint url is hardcoded into library so it is not necessary to ask user the api url
            if (config.EndpointType == GptEndpointType.AzureOpenAI &&
                string.IsNullOrEmpty(config.EndpointUrl))
            {
                config.EndpointUrl = GetSettingString("API Endpoint");
            }
            if (string.IsNullOrEmpty(config.Model))
            {
                config.Model = GetSettingString(
                    config.EndpointType == GptEndpointType.AzureOpenAI ? "Deployment name" : "Model name");
            }
            if (string.IsNullOrEmpty(config.ApiKey))
            {
                config.ApiKey = GetSettingString("API Key", true);
            }
        }
    }

    public static async Task<StringBuilder> StreamChatAnswerToScreenAsync(IAsyncEnumerable<string> messageStream)
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

    public static async Task StreamChatAnswerToStdOutAsync(IAsyncEnumerable<string> messageStream)
    {
        await foreach (var message in messageStream)
        {
            Console.Write(message);
        }
    }

    public static bool CheckConfigSaved(bool configSaved)
    {
        if (configSaved) return true;
        AppConfiguration.SaveAll();
        return true;
    }

    public static IRenderable GetRenderable(string text, bool complete = false)
    {
        // Markup with ... at end if the reponse is not complete 
        var markup = new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]");

        // Panel is for padding
        var panel = new Panel(markup);
        panel.Expand();
        panel.Border = BoxBorder.None;
        panel.PadLeft(GptConfiguration.ResponsePadding);

        // Panel does not really work alone with live view so we use a table as well
        var table = new Table();
        table.AddColumn("");
        table.AddRow(panel);
        table.Expand();
        table.Border = TableBorder.None;
        return table;
    }

    public static void WriteHorizontalDivider(string message)
    {
        AnsiConsole.Write(new Rule($"[green]{message}[/]").LeftJustified());
    }

    public static string? AskUser()
    {
        Console.Write(">> ");
        return Console.ReadLine();
        //return AnsiConsole.Ask<string>("[purple]>>[/]");
    }

    public static string ReadMultiline()
    {
        string result = "";
        using (var sr = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
        {
            while (!sr.EndOfStream)
            {
                var input = sr.ReadLine();
                if (input == GptConfiguration.MultilineIndicator)
                    return result;
                else
                    result += input + Environment.NewLine;
            }
            return result;
        }
    }

    public static async Task<string> ReadPipedInputAsync()
    {
        if (Console.IsInputRedirected)
        {
            var result = await Console.In.ReadToEndAsync();
            return result;
        }
        else return string.Empty;
    }

    public static void PrintProfile(string[,] profile)
    {
        var table = new Table();
        table.AddColumn("Setting name");
        table.AddColumn("Setting value");
        for (int i = 0; i < profile.GetLength(0); i++)
        {
            if (i % 2 == 0)
                table.AddRow(new Markup($"[seagreen1]{profile[i,0]}[/]"), new Markup($"[seagreen1]{profile[i,1]}[/]"));
            else
                table.AddRow(new Markup($"[steelblue1_1]{profile[i,0]}[/]"), new Markup($"[steelblue1_1]{profile[i,1]}[/]"));
        }
        AnsiConsole.Write(table);
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

    private static AppConfiguration.GptConfigSection GptConfiguration => 
        AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>();
}

public class ApplicationInitializationException : Exception
{
    public ApplicationInitializationException(string msg) 
        : base(msg) { }

    public ApplicationInitializationException(string msg, Exception inner) 
        : base(msg, inner) { }
}