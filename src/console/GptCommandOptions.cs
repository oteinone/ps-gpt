using System.ComponentModel;
using Spectre.Console.Cli;

namespace PowershellGpt.ConsoleApp;

public class GptCommandOptions : CommandSettings
{
    [CommandArgument(0, "[text]")]
    [Description("The text prompt sent to the specified model. Potentially preceded/surrounded by a prompt template if one has been defined.")]
    public string? Text { get; init; }

    [CommandOption("--clear")]
    [Description("Clears the current profile and resets the config to default values")]
    [DefaultValue(false)]
    public bool Clear { get; init; }

    [CommandOption("--get-profile|-g")]
    [Description("Shows the current profile settings saved in application configuration")]
    [DefaultValue(false)]
    public bool GetProfile { get; init; }

    [CommandOption("--set-profile|-s")]
    [Description("Sets a value in current profile. E.g. --set-profile model=gpt-3")]
    public string? SetProfile { get; init; }

    [CommandOption("--chat|-c")]
    [Description("Forces chat mode (continuous conversation) in cases where the input would otherwise be written to console/stdout")]
    public bool Chat { get; set; }

    [CommandOption("--system-prompt")]
    [Description("Commands the api with a 'system' type message before initializing the actual conversation")]
    public string? SystemPrompt { get; set; }

    [CommandOption("--template|-t")]
    [Description("A file path pointing to a template file. Template file is used as a wrapper for text content. '{text}' in templates is replaced with the text prompt")]
    public string? Template { get; set; }

    [CommandOption("--set-template")]
    [Description("Set a template in ps-gpt's template folder to the content specified in input text or pipeline.")]
    public string? SetTemplate { get; set; }

    [CommandOption("--get-template")]
    [Description("Print a template in ps-gpt's template folder")]
    public string? GetTemplate { get; set; }
}