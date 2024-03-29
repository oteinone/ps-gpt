using System.Text;
using PowershellGpt.Config;
using PowershellGpt.Config.Provider;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace PowershellGpt.ConsoleApp;

public interface IIOProvider
{
    bool IsConsoleInputRedirected { get; }
    void WriteHorizontalDivider(string message);
    string AskUser();
    string ReadMultiline(string multilineIndicator);
    AppConfigSection EnsureValidConfiguration(AppConfigSection appConfig);
    Task<string> ReadPipedInputAsync();
    Task StreamChatAnswerToStdOutAsync(IAsyncEnumerable<string> messageStream);
    Task<StringBuilder> StreamChatAnswerToScreenAsync(IAsyncEnumerable<string> messageStream);
    void PrintProfile(string[,] profile);
    void Print(string text);
}

public class ConsoleIOProvider: IIOProvider
{
    public virtual bool IsConsoleInputRedirected => Console.IsInputRedirected;

    private IAppConfigurationProvider _configProvider;
    public ConsoleIOProvider(IAppConfigurationProvider configurationProvider)
    {
        _configProvider = configurationProvider;
    }
    
    public void WriteHorizontalDivider(string message)
    {
        AnsiConsole.Write(new Rule($"[green]{message}[/]").LeftJustified());
    }

    public virtual string AskUser()
    {
        Console.Write(">> ");
        return Console.ReadLine() ?? string.Empty;
        //return AnsiConsole.Ask<string>("[purple]>>[/]");
    }

    public virtual string ReadMultiline(string multilineIndicator)
    {
        StringBuilder result = new StringBuilder();
        using (var sr = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
        {
            while (!sr.EndOfStream)
            {
                var input = sr.ReadLine();
                if (input == multilineIndicator)
                {
                    break;
                }
                result.AppendLine(input);
            }
            return result.ToString();
        }
    }
    
    public virtual async Task<string> ReadPipedInputAsync()
    {
        if (Console.IsInputRedirected)
        {
            var result = await Console.In.ReadToEndAsync();
            return result;
        }
        else return string.Empty;
    }

    public virtual async Task<StringBuilder> StreamChatAnswerToScreenAsync(IAsyncEnumerable<string> messageStream)
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

    public virtual async Task StreamChatAnswerToStdOutAsync(IAsyncEnumerable<string> messageStream)
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
    public AppConfigSection EnsureValidConfiguration(AppConfigSection appConfig)
    {
        if (appConfig.EndpointType == null || (appConfig.EndpointType == GptEndpointType.AzureOpenAI && string.IsNullOrEmpty(appConfig.EndpointUrl))
            || string.IsNullOrEmpty(appConfig.Model) || string.IsNullOrEmpty(appConfig.ApiKey))
        {
            if (Console.IsInputRedirected) throw new Exception(
                "Application configuration was incomplete, cannot accept input from pipeline. Run the application once without piped input to set application configuration");

            if (appConfig.EndpointType == null)
            {
                appConfig.EndpointType = SelectFromEnum<GptEndpointType>("Which type of endpoint are you using");
            }
            // Openai endpoint url is hardcoded into library so it is not necessary to ask user the api url
            if (appConfig.EndpointType == GptEndpointType.AzureOpenAI &&
                string.IsNullOrEmpty(appConfig.EndpointUrl))
            {
                appConfig.EndpointUrl = GetSettingString("API Endpoint");
            }
            if (string.IsNullOrEmpty(appConfig.Model))
            {
                appConfig.Model = GetSettingString(
                    appConfig.EndpointType == GptEndpointType.AzureOpenAI ? "Deployment name" : "Model name");
            }
            if (string.IsNullOrEmpty(appConfig.ApiKey))
            {
                appConfig.ApiKey = GetSettingString("API Key", true);
            }
        }
        return appConfig;
    }
    
    private IRenderable GetRenderable(string text, bool complete = false)
    {
        // Markup with ... at end if the reponse is not complete 
        var markup = new Markup($"[green]{Markup.Escape(text)}[orange1]{(!complete ? " ..." : "")}[/][/]");

        // Panel is for padding
        var panel = new Panel(markup);
        panel.Expand();
        panel.Border = BoxBorder.None;
        panel.PadLeft(_configProvider.AppConfig.ResponsePadding);

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

    public void Print(string text)
    {
        AnsiConsole.WriteLine(text);
    }
}