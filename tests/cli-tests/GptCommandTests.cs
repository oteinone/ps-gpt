using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;
using Moq;
using Spectre.Console;
using PowershellGpt.Config;

public class GptCommandTests
{
    private readonly IRemainingArguments _remainingArgs = new Mock<IRemainingArguments>().Object;
    private CommandContext GetContext(object? data) => new CommandContext(_remainingArgs, "", data);

    //TODO: Pipeline input tests

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
    public void Multiline_Mode_Works()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            Chat = true
        };
         mockEnv.IOProvider.SetupSequence(provider => provider.AskUser())
            .Returns("`")
            .Returns("exit");

        mockEnv.IOProvider.Setup(io => io.ReadMultiline("`")).Returns("How are you?");

        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask("How are you?"), Times.Once);
        mockEnv.AiClient.Verify(aic => aic.Ask(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Template_Param_Is_Used()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            Text = "This is a question",
            Template = ":mytemplate"
        };

        mockEnv.TemplateProvider.Setup(provider => provider.GetUserMessage("This is a question", ":mytemplate")).ReturnsAsync("Template: This is a question");

        var context = GetContext(mockEnv.Host);
        var result = new GptCommand().ExecuteAsync(context, options);
        mockEnv.AiClient.Verify(aic => aic.Ask("Template: This is a question"), Times.Once);
    }

    [Theory]
    [InlineData("model", "Model", "gpt900")]
    [InlineData("endpoint_url", "EndpointUrl", "https://www.google.fi?gpt")]
    [InlineData("api_key", "ApiKey", "asdf900198hjasdkjhk")]
    [InlineData("default_prompt_template", "DefaultAppPrompt", "Default app prompt:")]
    [InlineData("default_system_prompt", "DefaultSystemPrompt", "Default system prompt:")]
    public void Settings_Work(string paramName, string propertyName, string testValue)
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            SetProfile = $"{paramName}={testValue}"
        };

        var context = GetContext(mockEnv.Host);

        var result = new GptCommand().ExecuteAsync(context, options);
        
        mockEnv.ConfigProvider.Verify(configProvider => configProvider.Save(It.IsAny<AppConfigSection>()), Times.Once);

        var appConfigType = typeof(AppConfigSection);
        Assert.Equal(testValue, appConfigType?.GetProperty(propertyName)?.GetValue(mockEnv.TestConfiguration));
    }

    [Fact]
    public async Task Set_Template_Works()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            SetTemplate = ":mytemplate",
            Text = "My template is simply this: {0}"
        };

        var context = GetContext(mockEnv.Host);
        var result = await new GptCommand().ExecuteAsync(context, options);
        mockEnv.TemplateProvider.Verify(tp => tp.SetTemplate(":mytemplate", "My template is simply this: {0}"), Times.Once);
    }

    [Fact]
    public async Task Delete_Template_Works()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            SetTemplate = ":mytemplate"
        };

        var context = GetContext(mockEnv.Host);
        var result = await new GptCommand().ExecuteAsync(context, options);
        mockEnv.TemplateProvider.Verify(tp => tp.DeleteTemplate(":mytemplate"), Times.Once);
    }

    [Fact]
    public async Task Print_Template_Works()
    {
        var mockEnv = Common.Bootstrap();

        var options = new GptCommandOptions()
        {
            GetTemplate = ":mytemplate"
        };

        var context = GetContext(mockEnv.Host);
        mockEnv.TemplateProvider.Setup(provider => provider.GetTemplate(":mytemplate")).ReturnsAsync("Template: This is a question");

        AnsiConsole.Record();
        var result = await new GptCommand().ExecuteAsync(context, options);
        Assert.Contains("Template: This is a question", AnsiConsole.ExportText());
    }
}

