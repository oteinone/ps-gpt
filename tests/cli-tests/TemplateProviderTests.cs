using Moq;
using PowershellGpt.Exceptions;
using PowershellGpt.Templates;
using System.IO.Abstractions;

public class TemplateProviderTests
{
    private (Mock<FileSystem>, TemplateProvider) Bootstrap() 
    {
        var fileEnv = new Mock<FileSystem>();
        fileEnv.Setup(env => env.Directory.CreateDirectory(It.IsAny<string>())).Verifiable();
        return (fileEnv, new TemplateProvider(fileEnv.Object));
    }

    [Fact]
    public async void TemplateProvider_Can_Get_TemplateFile()
    {
        var (fileEnv, provider) = Bootstrap();

        fileEnv.Setup(env => env.File.ReadAllTextAsync(GetTemplateFilePath("mytemplate"), default(CancellationToken))).ReturnsAsync(DefaultTemplate);
        fileEnv.Setup(env => env.File.Exists(GetTemplateFilePath("mytemplate"))).Returns(true);

        var result = await provider.GetUserMessage("This is template body", ":mytemplate");

        Assert.Equal($"This is template header{Environment.NewLine}{Environment.NewLine}This is template body", result);
    }

    [Fact]
    public async void TemplateProvider_Can_Get_OtherFile()
    {
        var (fileEnv, provider) = Bootstrap();

        fileEnv.Setup(env => env.File.ReadAllTextAsync("c:\\temp\\mytemplate.template", default(CancellationToken))).ReturnsAsync(DefaultTemplate);
        fileEnv.Setup(env => env.File.Exists("c:\\temp\\mytemplate.template")).Returns(true);

        var result = await provider.GetUserMessage("This is template body", "c:\\temp\\mytemplate.template");

        Assert.Equal($"This is template header{Environment.NewLine}{Environment.NewLine}This is template body", result);
    }

    [Fact]
    public async void Template_Get_Throws_If_No_File()
    {
        var (fileEnv, provider) = Bootstrap();

        fileEnv.Setup(env => env.File.Exists("c:\\temp\\mytemplate.template")).Returns(false);

        await Assert.ThrowsAsync<UserException>(async () => await provider.GetUserMessage("This is template body", "c:\\temp\\mytemplate.template"));
    }

    [Fact]
    public async Task TemplateProvider_Can_Add_Template()
    {
        var (fileEnv, provider) = Bootstrap();
        fileEnv.Setup(env => env.File.WriteAllTextAsync(GetTemplateFilePath("newmessage"), "File Content", default(CancellationToken))).Verifiable();
        fileEnv.Setup(env => env.File.Exists(GetTemplateFilePath("newmessage"))).Returns(true);

        await provider.SetTemplate(":newmessage", "File Content");
        fileEnv.Verify(env => env.File.WriteAllTextAsync(GetTemplateFilePath("newmessage"), "File Content", default(CancellationToken)), Times.Once);
    }

    [Fact]
    public async Task TemplateProvider_Can_Delete_Template()
    {
        var (fileEnv, provider) = Bootstrap();
        fileEnv.Setup(env => env.File.Delete(GetTemplateFilePath("newmessage"))).Verifiable();
        fileEnv.Setup(env => env.File.Exists(GetTemplateFilePath("newmessage"))).Returns(true);

        await provider.DeleteTemplate(":newmessage");
        fileEnv.Verify(env => env.File.Delete(GetTemplateFilePath("newmessage")), Times.Once);
    }

    [Fact]
    public async void Template_Delete_Throws_If_No_File()
    {
        var (fileEnv, provider) = Bootstrap();
        fileEnv.Setup(env => env.File.Exists(GetTemplateFilePath("newmessage"))).Returns(false);
        await Assert.ThrowsAsync<UserException>(async () => await provider.DeleteTemplate(":newmessage"));
    }

    private static string TemplateFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt", "templates");
    private static string GetTemplateFilePath(string templatename) => Path.Combine(TemplateFolder, $"{templatename}.template");

    private static string DefaultTemplate => "This is template header" + Environment.NewLine + Environment.NewLine + "{0}";
}

