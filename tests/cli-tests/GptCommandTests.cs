using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;
using Moq;
using Spectre.Console;

public class GptCommandTests
{
    private readonly IRemainingArguments _remainingArgs = new Mock<IRemainingArguments>().Object;
    private CommandContext GetContext(object? data) => new CommandContext(_remainingArgs, "", data);

    [Fact]
    public void Question_Passed_To_Ai_Model()
    {
        var mockEnv = Common.Bootstrap();
        var options = new GptCommandOptions()
        {
            Text = "This is a question"
        };
        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask("This is a question"), Times.Once);
    }

    [Fact]
    public void Exit_Works()
    {
        var mockEnv = Common.Bootstrap();
        var options = new GptCommandOptions()
        {
        };

        mockEnv.IOProvider.SetupSequence(provider => provider.AskUser())
            .Returns("Give me a list of 10 fruit")
            .Returns("Give me 10 more")
            .Returns("exit");

        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public void Config_Only_Clear_Called()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            Text = "This is a question",
            Clear = true
        };
        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Never);
        mockEnv.ConfigProvider.Verify(conf => conf.ClearAll(), Times.Once);
    }

    [Fact]
    public void Config_Print_Called()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            Text = "This is a question",
            GetProfile = true
        };
        AnsiConsole.Record();
        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        var resultText = AnsiConsole.ExportText();
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Never);
        Assert.Contains("Setting name", resultText);
        Assert.Contains("Setting value", resultText);
    }
    
    [Fact]
    public void Chat_Mode_Works()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            Text = "This is a question",
            Chat = true
        };
         mockEnv.IOProvider.SetupSequence(provider => provider.AskUser())
            .Returns("Give me a list of 10 fruit")
            .Returns("Give me 10 more")
            .Returns("exit");

        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public void Template_Param_Is_Used()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            Text = "This is a question",
            Template = "@mytemplate"
        };

        mockEnv.TemplateProvider.Setup(provider => provider.GetUserMessage("This is a question", "@mytemplate")).ReturnsAsync("Template: This is a question");

        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask("Template: This is a question"), Times.Once);
    }
}

