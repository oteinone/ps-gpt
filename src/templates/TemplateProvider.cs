namespace PowershellGpt.Templates;
using System.IO.Abstractions;

public interface ITemplateProvider
{
    Task<string> GetUserMessage(string text, string? template_expression = null);
}

public class TemplateProvider : ITemplateProvider
{
    private IFileSystem _fileSystem;
    
    public TemplateProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<string> GetUserMessage(string text, string? template_expression = null)
    {
        if (template_expression == null) return text;
        var template = await GetTemplate(template_expression);

        return template.Contains("{0}") ? template.Replace("{0}", text)
            : $"{template}{Environment.NewLine}###{Environment.NewLine}{text}";
    }
    
    private async Task<string> GetTemplate(string template_expression)
    {
        if (template_expression.StartsWith('@'))
        {
            return await GetTemplateFromFile(GetTemplateFilePath(template_expression.Substring(1, template_expression.Length -1)));
        }
        else
        {
            return await GetTemplateFromFile(template_expression);
        }
    }

    private async Task<string> GetTemplateFromFile(string templatePath)
    {
        return await ReadFileAsync(templatePath);
    }

    private async Task<string> ReadFileAsync(string filePath)
    {
        return await _fileSystem.File.ReadAllTextAsync(filePath);
    }

    private static string TemplateFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt", "templates");
    private static string GetTemplateFilePath(string templatename) => Path.Combine(TemplateFolder, $"{templatename}.template");
}