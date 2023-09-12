using PowershellGpt.ConsoleApp;
using Spectre.Console.Cli;
using Moq;
using Spectre.Console;
using PowershellGpt.Templates;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

public class TemplateProviderTests
{
    private (Mock<FileSystem>, TemplateProvider) Bootstrap() 
    {
        var fileEnv = new Mock<FileSystem>();
        return (fileEnv, new TemplateProvider(fileEnv.Object));
    }

    [Fact]
    public async void TemplateProvider_Can_Get_TemplateFile()
    {
        var (fileEnv, provider) = Bootstrap();

        fileEnv.Setup(env => env.File.ReadAllTextAsync(GetTemplateFilePath("mytemplate"), default(CancellationToken))).ReturnsAsync(DefaultTemplate);

        var result = await provider.GetUserMessage("This is template body", "@mytemplate");

        Assert.Equal($"This is template header{Environment.NewLine}{Environment.NewLine}This is template body", result);
    }

    [Fact]
    public async void TemplateProvider_Can_Get_AnyFile()
    {
        var (fileEnv, provider) = Bootstrap();

        fileEnv.Setup(env => env.File.ReadAllTextAsync("c:\\temp\\mytemplate.template", default(CancellationToken))).ReturnsAsync(DefaultTemplate);

        var result = await provider.GetUserMessage("This is template body", "c:\\temp\\mytemplate.template");

        Assert.Equal($"This is template header{Environment.NewLine}{Environment.NewLine}This is template body", result);
    }


    private static string TemplateFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt", "templates");
    private static string GetTemplateFilePath(string templatename) => Path.Combine(TemplateFolder, $"{templatename}.template");

    private static string DefaultTemplate => "This is template header" + Environment.NewLine + Environment.NewLine + "{0}";
}

