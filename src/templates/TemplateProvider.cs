namespace PowershellGpt.Templates;

public class TemplateProvider
{
    public async Task<string> GetUserMessage(string text, string? template_expression = null)
    {
        if (template_expression == null) return text;
        var template = await GetTemplate(template_expression);

        return template.Contains("{text}") ? template.Replace("{text}", text)
            : $"{template}{Environment.NewLine}###{Environment.NewLine}{text}";
    }

    
    private async Task<string> GetTemplate(string template_expression)
    {
        if (!template_expression.StartsWith('@'))
        {
            return await GetTemplateFromFile(template_expression.Substring(1, template_expression.Length -1));
        }
        else
        {
            return template_expression;
        }
    }

    private async Task<string> GetTemplateFromFile(string templatePath)
    {
        return await ReadFileAsync(templatePath);
    }

    private static async Task<string> ReadFileAsync(string filePath)
    {
        using (StreamReader reader = new StreamReader(filePath))
        {
            string content = await reader.ReadToEndAsync();
            return content;
        }
    }
}