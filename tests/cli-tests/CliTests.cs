using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;
using Moq;
using Spectre.Console;

public class CliTests
{

    private readonly IRemainingArguments _remainingArgs = new Mock<IRemainingArguments>().Object;
    private CommandContext context => new CommandContext(_remainingArgs, "", null);

    [Fact]
    public void Question_Passed_To_Ai_Model()
    {
        var mockEnv = Common.Bootstrap();
        var options = new GptCommand.Options()
        {
            Text = "This is a question"
        };

        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask("This is a question"), Times.Once);
    }

    [Fact]
    public void Exit_Works()
    {
        var mockEnv = Common.Bootstrap();
        var options = new GptCommand.Options()
        {
        };

        mockEnv.IOProvider.SetupSequence(provider => provider.AskUser())
            .Returns("Give me a list of 10 fruit")
            .Returns("Give me 10 more")
            .Returns("exit");

        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public void Config_Only_Clear_Called()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommand.Options()
        {
            Text = "This is a question",
            Clear = true
        };
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Never);
        mockEnv.ConfigProvider.Verify(conf => conf.ClearAll(), Times.Once);
    }

    [Fact]
    public void Config_Print_Called()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommand.Options()
        {
            Text = "This is a question",
            GetProfile = true
        };
        AnsiConsole.Record();
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

        var options = new GptCommand.Options()
        {
            Text = "This is a question",
            Chat = true
        };
         mockEnv.IOProvider.SetupSequence(provider => provider.AskUser())
            .Returns("Give me a list of 10 fruit")
            .Returns("Give me 10 more")
            .Returns("exit");

        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Exactly(3));
    }
}

