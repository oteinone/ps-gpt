using System.Text;
using PowershellGpt.Config;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PowershellGpt.Application;

public static class Application
{


    
    // Method handles command arguments, updates application configuration
    // returns information required to run the program line application main loop.
    public static async Task<AppControlFlow> AppStart(string[] args)
    {
        var config = GptConfiguration;
        string? commandArg = args.FirstOrDefault();

        // handle command line arguments
        if (commandArg?.ToLower().Trim() == "--clear")
        {
            AppConfiguration.ClearAll();
            AppConfiguration.SaveAll();
            Console.WriteLine("Configuration cleared");
            return new AppControlFlow() { EndApplication = true };
        }
        else if (commandArg?.ToLower().Trim() == "--prompt")
        {
            var newPrompt = args.Length > 1 ? args[1] : string.Empty;
            config.DefaultAppPrompt = newPrompt;
            AppConfiguration.SaveAll();
            Console.WriteLine(string.IsNullOrEmpty(newPrompt) ? "Default prompt cleared" : $"Default prompt set to \"{newPrompt}\"");
            return new AppControlFlow() { EndApplication = true };
        }

         if (config.EndpointType == null || (config.EndpointType == GptEndpointType.AzureOpenAI && string.IsNullOrEmpty(config.EndpointUrl))
            || string.IsNullOrEmpty(config.Model) || string.IsNullOrEmpty(config.ApiKey))
        {
            if (Console.IsInputRedirected) throw new ApplicationInitializationException(
                "Application configuration was incomplete, cannot accept input from pipeline. Run the application directly to set application configuration");

            // Get OpenAI endpoint values from env or from the user
            if (config.EndpointType == null)
            {
                config.EndpointType = SelectFromEnum<GptEndpointType>("Which type of endpoint are you using");
            }
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

        return new AppControlFlow() 
        {
            EndApplication = false,
            ShowPromptResponse = !string.IsNullOrEmpty(commandArg),
            CustomPromptValue = commandArg ?? config.DefaultAppPrompt,
            PipedInput = await ReadPipedInput()
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

    public static async Task StreamChatAnswerToStdOut(IAsyncEnumerable<string> messageStream)
    {
            await foreach (var message in messageStream)
            {
                await Console.Out.WriteAsync(message);
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
        var element = new Panel(new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]"));
        element.Expand();
        element.Border = BoxBorder.None;
        element.PadLeft(GptConfiguration.ResponsePadding);

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

    internal static string? AskUser()
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

    private static async Task<string> ReadPipedInput()
    {
        if (Console.IsInputRedirected)
        {
            var result = await Console.In.ReadToEndAsync();
            return result;
        }
        else return string.Empty;
    }

    private static AppConfiguration.GptConfigSection GptConfiguration => 
        AppConfiguration.GetOrCreateConfigSection<AppConfiguration.GptConfigSection>();
}

public class AppControlFlow
{
    public bool EndApplication { get; set; }
    public bool ShowPromptResponse { get; set; }
    public string? CustomPromptValue { get; set; }

    public string? PipedInput { get; set; }
}

public class ApplicationInitializationException : Exception
{
    public ApplicationInitializationException(string msg) 
        : base(msg) { }

    public ApplicationInitializationException(string msg, Exception inner) 
        : base(msg, inner) { }
}