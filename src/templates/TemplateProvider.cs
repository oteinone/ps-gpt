namespace PowershellGpt.Templates;
using System.IO.Abstractions;
using PowershellGpt.Exceptions;

public interface ITemplateProvider
{
    Task<string> GetTemplate(string template_expression);
    Task<string> GetUserMessage(string text, string? template_expression = null);
    Task SetTemplate(string templateName, string templateContent);
    Task DeleteTemplate(string templateName);
}

public class TemplateProvider : ITemplateProvider
{
    private IFileSystem _fileSystem;
    
    public TemplateProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public virtual async Task<string> GetTemplate(string template_expression)
    {
        if (template_expression.StartsWith(TEMPLATE_SIGNIFIER))
        {
            return await GetTemplateFromFile(GetTemplateFilePath(TemplateNameToFileName(template_expression)));
        }
        else
        {
            return await GetTemplateFromFile(template_expression);
        }
    }

    public virtual async Task<string> GetUserMessage(string text, string? template_expression = null)
    {
        if (template_expression == null) return text;
        var template = await GetTemplate(template_expression);

        return template.Contains("{0}") ? template.Replace("{0}", text)
            : $"{template}{Environment.NewLine}###{Environment.NewLine}{text}";
    }

    public virtual async Task SetTemplate(string templateName, string templateContent)
    {
        var templatePath = GetTemplateFilePath(TemplateNameToFileName(templateName));
        CheckDirectoryExists();
        await _fileSystem.File.WriteAllTextAsync(templatePath, templateContent);
    }

    public virtual async Task DeleteTemplate(string templateName)
    {
        var templatePath = GetTemplateFilePath(TemplateNameToFileName(templateName));
        CheckFileExists(templatePath);
        _fileSystem.File.Delete(templatePath);
        await Task.CompletedTask;
    }

    private string TemplateNameToFileName(string templateName)
    {
        return templateName.TrimStart(TEMPLATE_SIGNIFIER);
    }

    private async Task<string> GetTemplateFromFile(string templatePath)
    {
        return await ReadFileAsync(templatePath);
    }

    private async Task<string> ReadFileAsync(string filePath)
    {
        CheckFileExists(filePath);
        return await _fileSystem.File.ReadAllTextAsync(filePath);
    }

    private void CheckDirectoryExists()
    {
        _fileSystem.Directory.CreateDirectory(TemplateFolder);
    }

    private void CheckFileExists(string? filePath)
    {
        if (filePath == null || !_fileSystem.File.Exists(filePath))
        {
            throw new UserException($"Template file at '{filePath}' was not found");
        }
    }

    private const char TEMPLATE_SIGNIFIER = ':';

    private static string TemplateFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ps-gpt", "templates");
    private static string GetTemplateFilePath(string templatename) => Path.Combine(TemplateFolder, $"{templatename}.template");

}