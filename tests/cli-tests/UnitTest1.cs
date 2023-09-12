namespace cli_tests;
using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;
using Moq;
using Spectre.Console;

public class CliTests
{

    private readonly IRemainingArguments _remainingArgs = new Mock<IRemainingArguments>().Object;

    [Fact]
    public void Test1()
    {
        Common.BootStrap();
        var command = new GptCommand();
        var context = new CommandContext(_remainingArgs, "", null);

        var options = new GptCommand.Options()
        {
            Text = "This is a question"
        };

        AnsiConsole.Record();
        var result = command.ExecuteAsync(context, options);
        var resultText = AnsiConsole.ExportText();
        Assert.Equal("Endresult", resultText);
        Assert.True(true);

    }
}