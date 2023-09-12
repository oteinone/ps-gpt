using System.Text;
using PowershellGpt.Config;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PowershellGpt.ConsoleApp;

public interface IIOProvider
{
    bool IsConsoleInputRedirected { get; }
    void WriteHorizontalDivider(string message);
    string AskUser();
    string ReadMultiline();
    void EnsureValidConfiguration();
    Task<string> ReadPipedInputAsync();
    Task StreamChatAnswerToStdOutAsync(IAsyncEnumerable<string> messageStream);
    Task<StringBuilder> StreamChatAnswerToScreenAsync(IAsyncEnumerable<string> messageStream);
    void PrintProfile(string[,] profile);
}

public class ConsoleIOProvider: IIOProvider
{
    public bool IsConsoleInputRedirected => Console.IsInputRedirected;
    
    public void WriteHorizontalDivider(string message)
    {
        AnsiConsole.Write(new Rule($"[green]{message}[/]").LeftJustified());
    }

    public string AskUser()
    {
        Console.Write(">> ");
        return Console.ReadLine() ?? string.Empty;
        //return AnsiConsole.Ask<string>("[purple]>>[/]");
    }

    public string ReadMultiline()
    {
        StringBuilder result = new StringBuilder();
        using (var sr = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
        {
            while (!sr.EndOfStream)
            {
                var input = sr.ReadLine();
                if (input == Services.Configuration.MultilineIndicator)
                {
                    break;
                }
                result.AppendLine(input);
            }
            return result.ToString();
        }
    }
    
    public async Task<string> ReadPipedInputAsync()
    {
        if (Console.IsInputRedirected)
        {
            var result = await Console.In.ReadToEndAsync();
            return result;
        }
        else return string.Empty;
    }

    public async Task<StringBuilder> StreamChatAnswerToScreenAsync(IAsyncEnumerable<string> messageStream)
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

    public async Task StreamChatAnswerToStdOutAsync(IAsyncEnumerable<string> messageStream)
    {
        await foreach (var message in messageStream)
        {
            Console.Write(message);
        }
        Console.WriteLine();
    }

    public void PrintProfile(string[,] profile)
    {
        var table = new Table();
        table.AddColumn("Setting name");
        table.AddColumn("Setting value");
        for (int i = 0; i < profile.GetLength(0); i++)
        {
            if (i % 2 == 0)
                table.AddRow(new Markup($"[seagreen1]{Markup.Escape(profile[i,0])}[/]"),
                    new Markup($"[seagreen1]{Markup.Escape(profile[i,1])}[/]"));
            else
                table.AddRow(new Markup($"[steelblue1_1]{Markup.Escape(profile[i,0])}[/]"),
                    new Markup($"[steelblue1_1]{Markup.Escape(profile[i,1])}[/]"));
        }
        AnsiConsole.Write(table);
    }

    // Makes sure that the current profile has all necessary parameters available to operate
    public void EnsureValidConfiguration()
    {
        var config = Services.Configuration;

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

            Services.AppConfigurationProvider.SaveAll();
        }
    }
    
    private IRenderable GetRenderable(string text, bool complete = false)
    {
        // Markup with ... at end if the reponse is not complete 
        var markup = new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]");

        // Panel is for padding
        var panel = new Panel(markup);
        panel.Expand();
        panel.Border = BoxBorder.None;
        panel.PadLeft(Services.Configuration.ResponsePadding);

        // Panel does not really work alone with live view so we use a table as well
        var table = new Table();
        table.AddColumn("");
        table.AddRow(panel);
        table.Expand();
        table.Border = TableBorder.None;
        return table;
    }

    private string GetSettingString(string settingName, bool secret = false)
    {
        var prompt = new TextPrompt<string>($"Input {settingName}:");
        if (secret) prompt.Secret('*');
        return AnsiConsole.Prompt(prompt);
    }

    private T SelectFromEnum<T>(string title)
        where T : struct, System.Enum
    {
        var prompt = new SelectionPrompt<T>()
        {
            Title = title
        };
        prompt.AddChoices(Enum.GetValues<T>());
        return AnsiConsole.Prompt(prompt);
    }
}